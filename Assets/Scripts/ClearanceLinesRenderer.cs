using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearanceLinesRenderer : MonoBehaviour
{
    #region Non-method Members
    private static readonly float _sizeScalar = 0.0035f;
    private static readonly float _sizeScalarOrtho = 0.005f;
    private static readonly float _sizeScalarOrthoMax = 0.05f;

    [SerializeField, ReadOnly] private LineRenderer _lineRenderer;

    [field: SerializeField, Tooltip("Should be \"true\" on heads that can have attachements (i.e. boom head that can have added shelves)")] 
    private bool IncludeChildrenInMeasurement { get; set; }

    private Selectable _highestSelectable;

    /// <summary> Can be null, be sure to check</summary>
    private Selectable _selectable;
    private List<Selectable> _trackedParentSelectables = new();
    private bool _rotateMeshWhenFindingFarthestVert;
    private bool _needsUpdate = true;
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
            UpdateLineRenderer();
        }
    }

    public void UpdateLineRenderer()
    {
        List<Vector3> positions = new();

        _highestSelectable.SetAssemblyToDefaultRotations();
        var higestOriginalRotation = _highestSelectable.transform.rotation;

        Vector3 originPoint = _highestSelectable.transform.position;
        Vector2 originPointXZ = new Vector2(originPoint.x, originPoint.z);

        MeshFilter[] meshFilters = IncludeChildrenInMeasurement ? GetComponentsInChildren<MeshFilter>() : new[] { GetComponent<MeshFilter>() };

        float farthestDistance = 0f;
        bool medianYEstablished = false;
        float highestYValue = float.MinValue;
        float lowestYValue = float.MaxValue;
        float medianY = 0f;

        void GetFarthestDistance()
        {
            for (int j = 0; j < meshFilters.Length; j++)
            {
                var filter = meshFilters[j];
                Vector3[] verts = filter.sharedMesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 transformedPoint = filter.transform.TransformPoint(verts[i]);

                    if (j == 0 && !medianYEstablished)
                    {
                        if (transformedPoint.y > highestYValue)
                        {
                            highestYValue = transformedPoint.y;
                        }

                        if (transformedPoint.y < lowestYValue)
                        {
                            lowestYValue = transformedPoint.y;
                        }
                    }

                    float distance = Vector2.Distance(originPointXZ, new Vector2(transformedPoint.x, transformedPoint.z));
                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                    }
                }

                if (j == 0 && !medianYEstablished)
                {
                    medianY = (highestYValue + lowestYValue) / 2f;
                    medianYEstablished = true;
                }
            }
        }

        if (_rotateMeshWhenFindingFarthestVert)
        {
            for (int i = 0; i < 361; i++)
            {
                _selectable.transform.Rotate(new Vector3(0, 0, 1));
                GetFarthestDistance();
            }
        }
        else
        {
            GetFarthestDistance();
        }

        float localY = medianY - _highestSelectable.transform.position.y;
        for (int i = 0; i < 361; i++)
        {
            _highestSelectable.transform.Rotate(new Vector3(0, 0, 1));
            Vector3 pos = _highestSelectable.transform.right * farthestDistance;
            pos.y = localY;
            positions.Add(pos);
        }

        _highestSelectable.transform.rotation = higestOriginalRotation;
        _highestSelectable.RestoreArmAssemblyRotations();

        Debug.Log($"Received {positions.Count} vertex positions for line renderer");
        _lineRenderer.positionCount = positions.Count;
        _lineRenderer.SetPositions(positions.ToArray());
        _needsUpdate = false;
    }
    #endregion
}