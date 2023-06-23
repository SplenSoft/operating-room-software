using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomBoundary : MonoBehaviour
{
    private static List<RoomBoundary> _instances = new();
    private readonly float _defaultWallThickness = 0.375f.ToMeters(); //4.5 inches
    [field: SerializeField] private RoomBoundaryType RoomBoundaryType { get; set; }
    [field: SerializeField] private CinemachineVirtualCamera VirtualCamera { get; set; }
    private MeshRenderer _meshRenderer;
    private Collider _collider;

    private void Awake()
    {
        _instances.Add(this);

        _meshRenderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();

        if (VirtualCamera != null)
            CameraManager.Register(VirtualCamera);

        RoomSize.RoomSizeChanged += (obj, arg) =>
        {
            float height = RoomSize.GetDimension(RoomDimension.Height);
            float width = RoomSize.GetDimension(RoomDimension.Width);
            float depth = RoomSize.GetDimension(RoomDimension.Depth);

            switch (RoomBoundaryType)
            {
                case RoomBoundaryType.Ceiling:
                    transform.localScale = new Vector3(width, _defaultWallThickness, depth);
                    transform.position = new Vector3(0, height + (transform.localScale.y / 2), 0);
                    break;
                case RoomBoundaryType.Floor:
                    transform.localScale = new Vector3(width, _defaultWallThickness, depth);
                    transform.position = new Vector3(0, 0 - (transform.localScale.y / 2), 0);
                    break;
                case RoomBoundaryType.WallSouth:
                    transform.localScale = new Vector3(width, height, _defaultWallThickness);
                    transform.position = new Vector3(0, height / 2f, 0 - (depth / 2f) - (transform.localScale.z / 2));
                    break;
                case RoomBoundaryType.WallWest:
                    transform.localScale = new Vector3(_defaultWallThickness, height, depth);
                    transform.position = new Vector3(0 - (width / 2f) - (transform.localScale.x / 2f), height / 2f, 0);
                    break;
                case RoomBoundaryType.WallEast:
                    transform.localScale = new Vector3(_defaultWallThickness, height, depth);
                    transform.position = new Vector3(0 + (width / 2f) + (transform.localScale.x / 2f), height / 2f, 0);
                    break;
                case RoomBoundaryType.WallNorth:
                    transform.localScale = new Vector3(width, height, _defaultWallThickness);
                    transform.position = new Vector3(0, height / 2f, 0 + (depth / 2f) + (transform.localScale.z / 2));
                    break;
            }
        };
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

    public static void EnableAllMeshRenderersAndColliders()
    {
        _instances.ForEach(item => item.ToggleMeshRendererAndCollider(true));
    }

    private void OnMouseUpAsButton()
    {
        if (InputHandler.IsPointerOverUIElement()) return;
        Selectable.DeselectAll();
    }

    private void ToggleMeshRendererAndCollider(bool toggle)
    {
        _meshRenderer.enabled = toggle;
        _collider.enabled = toggle;
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
