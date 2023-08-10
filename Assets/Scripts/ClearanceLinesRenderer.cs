using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearanceLinesRenderer : MonoBehaviour
{
    private static readonly float _sizeScalar = 0.01f;
    private static readonly float _sizeScalarOrtho = 0.01f;
    private static readonly float _sizeScalarOrthoMax = 0.07f;
    [field: SerializeField] private LineRenderer LineRenderer { get; set; }

    private void Awake()
    {
        UI_ToggleClearanceLines.ClearanceLinesToggled.AddListener(OnClearanceLinesToggled);
        gameObject.SetActive(UI_ToggleClearanceLines.IsActive);
    }

    private void OnClearanceLinesToggled()
    {
        gameObject.SetActive(UI_ToggleClearanceLines.IsActive);
    }

    private void OnDestroy()
    {
        UI_ToggleClearanceLines.ClearanceLinesToggled.RemoveListener(OnClearanceLinesToggled);
    }

    void Update()
    {
        if (FreeLookCam.IsActive) 
        {
            float distanceToCamera = Vector3.Distance(gameObject.transform.position, Camera.main.transform.position);
            LineRenderer.startWidth = _sizeScalar * distanceToCamera;
            LineRenderer.endWidth = _sizeScalar * distanceToCamera;
        }
        else
        {
            float size = Mathf.Min(_sizeScalarOrthoMax, _sizeScalarOrtho * Camera.main.orthographicSize);
            LineRenderer.startWidth = size;
            LineRenderer.endWidth = size;
        }
    }

    public void SetPositions(List<Vector3> positions)
    {
        Debug.Log($"Received {positions.Count} vertex positions for line renderer");
        LineRenderer.positionCount = positions.Count;
        LineRenderer.SetPositions(positions.ToArray());
    }
}
