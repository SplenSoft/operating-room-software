using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class RoomBoundary : MonoBehaviour
{
    public UnityEvent VisibilityStatusChanged { get; } = new();
    private static readonly float _mouseMoveSensitivityX = 10f;
    private static readonly float _mouseMoveSensitivityY = 10f;
    private static readonly float _scrollSensitivity = 0.5f;
    public static List<RoomBoundary> Instances { get; private set; } = new();
    public static readonly float DefaultWallThickness = 0.375f.ToMeters(); //4.5 inches
    [field: SerializeField] public RoomBoundaryType RoomBoundaryType { get; private set; }
    [field: SerializeField] private CinemachineVirtualCamera VirtualCamera { get; set; }
    private bool VirtualCameraActive => (CinemachineVirtualCamera)CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera == VirtualCamera;
    public MeshRenderer MeshRenderer { get; private set; }
    public Collider Collider { get; private set; }
    [field: SerializeField] public GameObject baseboard { get; private set; }
    private CinemachineTransposer _transposer;
    private static Dictionary<RoomBoundaryType, RoomBoundary> RoomBoundariesByType { get; set; } = new();

    private void Awake()
    {
        Instances.Add(this);
        RoomBoundariesByType[RoomBoundaryType] = this;

        MeshRenderer = GetComponent<MeshRenderer>();
        Collider = GetComponent<Collider>();

        if (VirtualCamera != null)
        {
            CameraManager.Register(VirtualCamera);
            _transposer = VirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        }

        RoomSize.RoomSizeChanged += SetSize;
    }

    void SetSize(RoomDimension dimension)
    {
        float height = dimension.Height.ToMeters();
        float width = dimension.Width.ToMeters();
        float depth = dimension.Depth.ToMeters();

        Vector3 basePosition = new Vector3(0, 0, 0);
        if (baseboard != null)
        {
            basePosition = baseboard.transform.localPosition;
            baseboard.transform.SetParent(null);
        }

        switch (RoomBoundaryType)
        {
            case RoomBoundaryType.Ceiling:
                transform.localScale = new Vector3(width, DefaultWallThickness, depth);
                transform.position = new Vector3(0, height + (transform.localScale.y / 2), 0);
                break;
            case RoomBoundaryType.Floor:
                transform.localScale = new Vector3(width, DefaultWallThickness, depth);
                transform.position = new Vector3(0, 0 - (transform.localScale.y / 2), 0);
                break;
            case RoomBoundaryType.WallSouth:
                transform.localScale = new Vector3(width, height, DefaultWallThickness);
                transform.position = new Vector3(0, height / 2f, 0 - (depth / 2f) - (transform.localScale.z / 2));
                break;
            case RoomBoundaryType.WallWest:
                transform.localScale = new Vector3(DefaultWallThickness, height, depth);
                transform.position = new Vector3(0 - (width / 2f) - (transform.localScale.x / 2f), height / 2f, 0);
                break;
            case RoomBoundaryType.WallEast:
                transform.localScale = new Vector3(DefaultWallThickness, height, depth);
                transform.position = new Vector3(0 + (width / 2f) + (transform.localScale.x / 2f), height / 2f, 0);
                break;
            case RoomBoundaryType.WallNorth:
                transform.localScale = new Vector3(width, height, DefaultWallThickness);
                transform.position = new Vector3(0, height / 2f, 0 + (depth / 2f) + (transform.localScale.z / 2));
                break;
        }

        if (baseboard != null)
        {
            baseboard.transform.SetParent(this.transform);
            baseboard.transform.localPosition = basePosition;
            baseboard.transform.SetParent(null);

            float highest = Math.Max(depth, width);

            baseboard.transform.localScale = new Vector3(
                baseboard.transform.localScale.x,
                baseboard.transform.localScale.y + (highest / 4f),
                baseboard.transform.localScale.z);
        }
    }

    public void OnCameraLive()
    {
        EnableAllMeshRenderersAndColliders();
        ToggleMeshRendererAndCollider(false);

        if (RoomBoundaryType == RoomBoundaryType.Ceiling || RoomBoundaryType == RoomBoundaryType.Floor)
        {
            float dim = Mathf.Max(transform.localScale.x, transform.localScale.z);
            VirtualCamera.m_Lens.OrthographicSize = (dim / 2f) + (dim / 10f);
        }
        else
        {
            VirtualCamera.m_Lens.OrthographicSize = (transform.localScale.y / 2f) + (transform.localScale.y / 10f);
        }
    }

    private void Update()
    {
        if (VirtualCamera != null && VirtualCameraActive)
        {

            HandleCameraMovement();
        }
    }

    private void HandleCameraMovement()
    {
        if (GizmoHandler.GizmoBeingUsed || FullScreenMenu.IsOpen) return;
        float scroll = -GetScrollWheel() * _scrollSensitivity;
        bool move = Input.GetMouseButton(0);
        Vector2 mouseMovement = new Vector2(move ? InputHandler.MouseDeltaScreenPercentage.x * _mouseMoveSensitivityX : 0, move ? InputHandler.MouseDeltaScreenPercentage.y * _mouseMoveSensitivityY : 0);

        switch (RoomBoundaryType)
        {
            case RoomBoundaryType.Ceiling:
                _transposer.m_FollowOffset.x -= mouseMovement.x;
                _transposer.m_FollowOffset.z -= mouseMovement.y;
                break;
            case RoomBoundaryType.WallEast:
                _transposer.m_FollowOffset.y -= mouseMovement.y;
                _transposer.m_FollowOffset.z -= mouseMovement.x;
                break;
            case RoomBoundaryType.WallSouth:
                _transposer.m_FollowOffset.x -= mouseMovement.x;
                _transposer.m_FollowOffset.y -= mouseMovement.y;
                break;
        }

        VirtualCamera.m_Lens.OrthographicSize = Mathf.Max(1f, VirtualCamera.m_Lens.OrthographicSize + scroll);
    }

    private float GetScrollWheel()
    {
        return Input.mouseScrollDelta.y;
    }

    public static void EnableAllMeshRenderersAndColliders()
    {
        Instances.ForEach(item => item.ToggleMeshRendererAndCollider(true));
    }

    public void SetColor(Color c)
    {
        GetComponent<MeshRenderer>().material.color = c;
    }

    /// <summary>
    /// Toggles the walls between opaque and clear
    /// </summary>
    /// <param name="toggle">True = Opaque, False = Clear</param>
    public static void ToggleAllWallOpaque(bool toggle)
    {
        Instances.ForEach(item => item.ToggleWallOpaque(toggle));
    }

    private void ToggleWallOpaque(bool toggle)
    {
        if (RoomBoundaryType == RoomBoundaryType.Floor || RoomBoundaryType == RoomBoundaryType.Ceiling) return;
        Color c = MeshRenderer.material.color;
        c.a = toggle ? 1 : 0;
        MeshRenderer.material.color = c;
    }

    private void OnMouseUpAsButton()
    {
        if (InputHandler.IsPointerOverUIElement()) return;
        Selectable.DeselectAll();
    }

    private void ToggleMeshRendererAndCollider(bool toggle)
    {
        bool oldStatus = MeshRenderer.enabled;
        MeshRenderer.enabled = toggle;
        Collider.enabled = toggle;

        if (toggle != oldStatus)
        {
            VisibilityStatusChanged?.Invoke();
        }
    }

    public static RoomBoundary GetRoomBoundary(RoomBoundaryType roomBoundaryType)
    {
        return RoomBoundariesByType[roomBoundaryType];
    }
}

public enum RoomBoundaryType
{
    Ceiling,
    Floor,
    WallSouth,
    WallNorth,
    WallEast,
    WallWest
}
