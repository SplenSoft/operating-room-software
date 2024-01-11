using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public partial class ClearanceLinesRenderer : MonoBehaviour
{
    #region Non-method Members
    private class MeshVertsData
    {
        public MeshVertsData(MeshFilter meshFilter, ClearanceLinesRenderer clearanceLinesRenderer)
        {
            MeshFilter = meshFilter;
            _clearanceLinesRenderer = clearanceLinesRenderer;
        }

        public MeshFilter MeshFilter { get; }
        public Quaternion Rotation { get; set; }
        public Vector3 GlobalPosition { get; set; }
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
        private ClearanceLinesRenderer _clearanceLinesRenderer;

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

        public void RecordData(int rotationAmount, Vector3 forwardVector)
        {
            float farthest = 0f;
            float localHighestY = float.MinValue;
            float localLowestY = float.MaxValue;

            for (int j = 0; j < _clearanceLinesRenderer._meshVertsDatas.Count; j++)
            {
                MeshVertsData vertData = _clearanceLinesRenderer._meshVertsDatas[j];
                for (int i = 0; i < Vertices.Length; i++)
                {
                    Vector3 vert = Vertices[i];
                    vert.x *= LossyScale.x;
                    vert.y *= LossyScale.y;
                    vert.z *= LossyScale.z;
                    vert = Rotation * vert;

                    if (j > 0)
                    {
                        vert += vertData.GlobalPosition - _clearanceLinesRenderer._meshVertsDatas[0].GlobalPosition;
                    }

                    vert = Quaternion.AngleAxis(rotationAmount, forwardVector) * vert;
                    Vector3 transformedPoint = vert + GlobalPosition;

                    if (rotationAmount == 0)
                    {
                        if (transformedPoint.y > localHighestY)
                        {
                            localHighestY = transformedPoint.y;
                        }

                        if (transformedPoint.y < localLowestY)
                        {
                            localLowestY = transformedPoint.y;
                        }
                    }

                    float distance = Vector2.Distance(_clearanceLinesRenderer._originPointXZ, new Vector2(transformedPoint.x, transformedPoint.z));
                    if (distance > farthest)
                    {
                        farthest = distance;
                    }

                    if (_clearanceLinesRenderer._cancelTask)
                    {
                        return;
                    }
                }
            }

            

            lock (_clearanceLinesRenderer._lockObject)
            {
                if (farthest > _clearanceLinesRenderer._farthestDistance)
                {
                    _clearanceLinesRenderer._farthestDistance = farthest;
                }

                if (rotationAmount == 0)
                {
                    if (localHighestY > _clearanceLinesRenderer._highestY)
                    {
                        _clearanceLinesRenderer._highestY = localHighestY;
                    }

                    if (localLowestY < _clearanceLinesRenderer._lowestY)
                    {
                        _clearanceLinesRenderer._lowestY = localLowestY;
                    }
                }
            }
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

    private static List<Vector3> _circlePositions = new List<Vector3>();

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
    private Vector2 _originPointXZ;
    private List<Vector3> _positions = new();
    private float _highestY;
    private float _lowestY;
    private float _farthestDistance;
    private bool _rotateMeshWhenFindingFarthestVert;
    private bool _needsUpdate = true;
    private bool _taskRunning = false;
    private bool _cancelTask = false;
    private float MedianY => ((_highestY + _lowestY) / 2f) - _highestSelectable.transform.position.y;
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

    #region Events
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

    [RuntimeInitializeOnLoadMethod]
    private static void OnAppStart()
    {
        for (int i = 0; i < 360; i++)
        {
            _circlePositions.Add(Quaternion.AngleAxis(i, Vector3.up) * Vector3.forward);
        }
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

    private void ResetVariables()
    {
        _taskRunning = true;
        _highestY = float.MinValue;
        _lowestY = float.MaxValue;
        _farthestDistance = 0f;
        _positions = new List<Vector3>(_circlePositions);       
        _farthestDistance = 0f;
        _originPointXZ = new Vector2(_highestSelectable.transform.position.x, _highestSelectable.transform.position.z);
    }

    private void ResetMeshVertsData()
    {
        if (_meshVertsDatas == null)
        {
            _meshVertsDatas = new();
            MeshFilter[] meshFilters = IncludeChildrenInMeasurement ? GetComponentsInChildren<MeshFilter>() : new[] { GetComponent<MeshFilter>() };
            for (int j = 0; j < meshFilters.Length; j++)
            {
                var filter = meshFilters[j];
                _meshVertsDatas.Add(new MeshVertsData(filter, this)
                {
                    Rotation = filter.transform.rotation,
                    GlobalPosition = filter.transform.position,
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
                meshVertsData.GlobalPosition = meshVertsData.MeshFilter.transform.position;
                meshVertsData.Rotation = meshVertsData.MeshFilter.transform.rotation;
                meshVertsData.LossyScale = meshVertsData.MeshFilter.transform.lossyScale;
            }
        }
    }

    private async Task GetFarthestDistance()
    {
        Task task = Task.Factory.StartNew(() =>
        {
            Parallel.For(0, _meshVertsDatas[0].Rotations, j =>
            {
                _meshVertsDatas[0].RecordData(j, Vector3.down);
            });
        });

        await task;
    }

    private async void UpdateLineRendererArmAssembly()
    {
        ResetVariables();
        ResetMeshVertsData();
        await GetFarthestDistance();

        // object was destroyed while task was running
        if (_lineRenderer == null)
        {
            _needsUpdate = false;
            _taskRunning = false;
            _cancelTask = false;
            return;
        }

        // arm hierarchy scale was updated while task was running
        if (_cancelTask)
        {
            _taskRunning = false;
            _cancelTask = false;
            return;
        }

        _farthestDistance += BufferSize;
        for (int i = 0; i < _positions.Count; i++)
        {
            Vector3 newPos = _positions[i] * _farthestDistance;
            newPos.y = MedianY;
            _positions[i] = newPos;
        }

        //Debug.Log($"Received {positions.Count} vertex positions for line renderer");

        _lineRenderer.positionCount = _positions.Count;
        _lineRenderer.SetPositions(_positions.ToArray());

        _taskRunning = false;
        _cancelTask = false;

        if (!_cancelTask)
        {
            _needsUpdate = false;
        }
    }

    private void UpdateLineRendererDoor()
    {
        
    }

    public void UpdateLineRenderer()
    {
        if (Type == RendererType.ArmAssembly) 
        {
            UpdateLineRendererArmAssembly();
        }
        else if (Type == RendererType.Door)
        {
            UpdateLineRendererDoor();
        }
    }
    #endregion
}