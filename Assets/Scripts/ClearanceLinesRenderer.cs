using PdfSharpCore.Pdf.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class ClearanceLinesRenderer : MonoBehaviour
{
    #region Non-method Members
    private class MeshVertsData
    {
        public MeshVertsData(MeshFilter meshFilter)
        {
            MeshFilter = meshFilter;
        }

        public MeshFilter MeshFilter { get; }
        public Quaternion Rotation { get; set; }
        public Vector3 Position { get; set; }
        public Vector3[] Vertices { get; set; }
        public int Rotations { get; set; } = 1;
        public Vector3 LossyScale { get; set; }
        private int? _hierarchyNestedLevel;
        public int HierarchyNestedLevel
        { 
            get 
            { 
                _hierarchyNestedLevel ??= GetHierarchyNestedLevel();
                return (int)_hierarchyNestedLevel;
            }
        }

        private int GetHierarchyNestedLevel()
        {
            int level = 0;
            Transform parent = MeshFilter.transform.parent;
            while (parent != null)
            {
                parent = parent.parent;
                level++;
            }
            return level;
        }
    }

    public enum RendererType
    {
        ArmAssembly,
        Door
    }

    private static readonly float _sizeScalar = 0.0035f;
    private static readonly float _sizeScalarOrtho = 0.005f;
    private static readonly float _sizeScalarOrthoMax = 0.05f;

    private LineRenderer _lineRenderer;

    [field: SerializeField, Tooltip("Should be \"true\" on heads that can have attachements (i.e. boom head that can have added shelves)")] 
    private bool IncludeChildrenInMeasurement { get; set; }

    [field: SerializeField, Tooltip("Adds a buffer amount to clearance lines to account for lossy scale inaccuracies")]
    private float BufferSize { get; set; }

    [field: SerializeField, Tooltip("Only takes XZ data")]
    private Transform DoorHinge { get; set; }

    [field: SerializeField, Tooltip("Only takes XZ data")]
    private Transform DoorStrike { get; set; }

    [field: SerializeField]
    private float DoorSwingAngle { get; set; } = 90f;

    [field: SerializeField]
    private RendererType Type { get; set; }

    private Selectable _highestSelectable;

    /// <summary> Can be null, be sure to check</summary>
    private Selectable _selectable;
    private List<Selectable> _trackedParentSelectables = new();
    private List<MeshVertsData> _meshVertsDatas;
    private bool _rotateMeshWhenFindingFarthestVert;
    private bool _needsUpdate = true;
    private bool _taskRunning = false;
    private bool _cancelTask = false;
    bool _medianYEstablished = false;
    float _highestYValue = float.MinValue;
    float _lowestYValue = float.MaxValue;
    float _medianY = 0f;
    private object _lockObject = new();
    #endregion

    #region Monobehaviour
    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        Destroy(_lineRenderer.gameObject);
    }

    private void Start()
    {
        Subscribe();

        if (_trackedParentSelectables[0].TryGetArmAssemblyRoot(out GameObject armAssemblyRoot))
        {
            _highestSelectable = armAssemblyRoot.GetComponent<Selectable>();
        }
        else throw new Exception("Could not get clearance lines - no higher z-rotation in the arm assembly found");

        _rotateMeshWhenFindingFarthestVert = _selectable != null && _selectable.IsGizmoSettingAllowed(GizmoType.Rotate, Axis.Z);

        CheckStatus();
    }

    private void Update()
    {
        if (!UI_ToggleClearanceLines.IsActive) return;

        if (_needsUpdate && !_taskRunning)
        {
            UpdateLineRenderer();
        }

        if (FreeLookCam.IsActive)
        {
            float distanceToCamera = Vector3.Distance(gameObject.transform.position, Camera.main.transform.position);
            _lineRenderer.startWidth = _sizeScalar * distanceToCamera;
            _lineRenderer.endWidth = _sizeScalar * distanceToCamera;
        }
        else
        {
            float size = Mathf.Min(_sizeScalarOrthoMax, _sizeScalarOrtho * Camera.main.orthographicSize);
            _lineRenderer.startWidth = size;
            _lineRenderer.endWidth = size;
        }
    }
    #endregion

    #region EventHandling
    private void Subscribe()
    {
        var parent = transform.parent;

        while (parent != null)
        {
            if (parent.TryGetComponent(out Selectable selectable))
            {
                _trackedParentSelectables.Add(selectable);
            }

            parent = parent.parent;
        }

        _trackedParentSelectables.ForEach(selectable =>
        {
            selectable.ScaleUpdated.AddListener(SetNeedsUpdate);
        });

        UI_ToggleClearanceLines.ClearanceLinesToggled.AddListener(CheckStatus);
    }

    private void Unsubscribe()
    {
        _trackedParentSelectables.ForEach(selectable =>
        {
            if (selectable != null && !selectable.IsDestroyed)
            {
                selectable.ScaleUpdated.RemoveListener(SetNeedsUpdate);
            }
        });

        UI_ToggleClearanceLines.ClearanceLinesToggled.RemoveListener(CheckStatus);
    }
    #endregion

    #region Logic
    private void SetNeedsUpdate()
    {
        _needsUpdate = true;
        if (_taskRunning)
        {
            _cancelTask = true;
        }
        CheckStatus();
    }

    private void CheckStatus()
    {
        if (_lineRenderer == null)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/ClearanceLinesRenderer");
            var newObj = Instantiate(prefab, _highestSelectable.transform);
            newObj.transform.rotation = Quaternion.identity;
            _lineRenderer = newObj.GetComponent<LineRenderer>();
        }

        _lineRenderer.gameObject.SetActive(UI_ToggleClearanceLines.IsActive);

        if (UI_ToggleClearanceLines.IsActive && _needsUpdate)
        {
            if (!_taskRunning)
            {
                UpdateLineRenderer();
            }
        }
    }

    public async void UpdateLineRenderer()
    {
        _taskRunning = true;

        List<Vector3> positions = new();
        Vector3 forwardVector = transform.forward;
        _highestSelectable.SetAssemblyToDefaultRotations();
        var higestOriginalRotation = _highestSelectable.transform.rotation;

        Vector3 originPoint = _highestSelectable.transform.position;
        Vector2 originPointXZ = new Vector2(originPoint.x, originPoint.z);

        MeshFilter[] meshFilters = IncludeChildrenInMeasurement ? GetComponentsInChildren<MeshFilter>() : new[] { GetComponent<MeshFilter>() };

        float farthestDistance = 0f;
        
        if (_meshVertsDatas == null)
        {
            _meshVertsDatas = new();

            for (int j = 0; j < meshFilters.Length; j++)
            {
                var filter = meshFilters[j];
                _meshVertsDatas.Add(new MeshVertsData(filter)
                {
                    Rotation = filter.transform.rotation,
                    Position = filter.transform.position,
                    Vertices = filter.sharedMesh.vertices,
                    Rotations = _rotateMeshWhenFindingFarthestVert ? 361 : 1,
                    LossyScale = filter.transform.lossyScale,
                });
            }
        }
        else
        {
            foreach (var meshVertsData in _meshVertsDatas)
            {
                meshVertsData.Position = meshVertsData.MeshFilter.transform.position;
                meshVertsData.Rotation = meshVertsData.MeshFilter.transform.rotation;
                meshVertsData.LossyScale = meshVertsData.MeshFilter.transform.lossyScale;
            }
        }

        void RecordData(MeshVertsData meshVertsData, bool first, int rotationAmount)
        {
            float farthest = 0f;
            for (int i = 0; i < meshVertsData.Vertices.Length; i++)
            {
                Vector3 vert = meshVertsData.Vertices[i];
                vert.x *= meshVertsData.LossyScale.x;
                vert.y *= meshVertsData.LossyScale.y;
                vert.z *= meshVertsData.LossyScale.z;
                vert = meshVertsData.Rotation * Quaternion.AngleAxis(rotationAmount, forwardVector) * vert;
                Vector3 transformedPoint = vert + meshVertsData.Position;

                if (first && !_medianYEstablished && rotationAmount == 0)
                {
                    if (transformedPoint.y > _highestYValue)
                    {
                        _highestYValue = transformedPoint.y;
                    }

                    if (transformedPoint.y < _lowestYValue)
                    {
                        _lowestYValue = transformedPoint.y;
                    }
                }

                float distance = Vector2.Distance(originPointXZ, new Vector2(transformedPoint.x, transformedPoint.z));
                if (distance > farthest)
                {
                    farthest = distance;
                }

                if (_cancelTask)
                {
                    return;
                }
            }

            lock (_lockObject)
            {
                if (farthest > farthestDistance)
                {
                    farthestDistance = farthest;
                }
            }
        }

        async Task GetFarthestDistance()
        {
            //Vector3[] verts = filter.sharedMesh.vertices;
            foreach (var meshVertsData in _meshVertsDatas)
            {
                bool first = _meshVertsDatas[0] == meshVertsData;
                Task task = Task.Factory.StartNew(() =>
                {
                    if (first)
                    {
                    
                        Parallel.For(0, meshVertsData.Rotations, j =>
                        {
                            RecordData(meshVertsData, first, j);
                        });
                    }
                    else
                    {
                        RecordData(meshVertsData, first, 0);
                    }
                });

                await task;

                if (_cancelTask)
                {
                    return;
                }

                if (first && !_medianYEstablished)
                {
                    _medianY = (_highestYValue + _lowestYValue) / 2f;
                    _medianYEstablished = true;
                }
            }
        }

        for (int i = 0; i < 361; i++)
        {
            _highestSelectable.transform.Rotate(new Vector3(0, 0, 1));
            Vector3 pos = _highestSelectable.transform.right;
            positions.Add(pos);
        }

        _highestSelectable.transform.rotation = higestOriginalRotation;
        _highestSelectable.RestoreArmAssemblyRotations();

        await GetFarthestDistance();

        // object was destroyed while task was running
        if (_lineRenderer == null)
        {
            _needsUpdate = false;
            _taskRunning = false;
            _cancelTask = false;
            return;
        }

        if (_cancelTask)
        {
            _taskRunning = false;
            _cancelTask = false;
            return;
        }

        float localY = _medianY - _highestSelectable.transform.position.y;
        farthestDistance += BufferSize;
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 newPos = positions[i] * farthestDistance;
            newPos.y = localY;
            positions[i] = newPos;
        }

        //Debug.Log($"Received {positions.Count} vertex positions for line renderer");
        
        _lineRenderer.positionCount = positions.Count;
        _lineRenderer.SetPositions(positions.ToArray());      
        
        _taskRunning = false;
        _cancelTask = false;

        if (!_cancelTask)
        {
            _needsUpdate = false;
        }
    }
    #endregion
}