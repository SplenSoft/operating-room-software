using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class OrbitCamera : MonoBehaviour
{
    [field: SerializeField] public CinemachineVirtualCamera VirtualCamera { get; private set; }
    [field: SerializeField] public float minFOV { get; private set; }
    [field: SerializeField] public float maxFOV { get; private set; }
    [field: SerializeField] public OrbitMode orbitMode { get; private set; } = OrbitMode.FreeMove;
    [field: SerializeField] public GameObject orbitTarget { get; private set; }
    private Rigidbody orbitRigidBody;
    private Vector3 previousPosition;

    public void Awake()
    {
        orbitTarget.transform.position = new Vector3(0, 0, 0);
        orbitRigidBody = orbitTarget.GetComponent<Rigidbody>();
        VirtualCamera.LookAt = orbitTarget.transform;

        CameraManager.Register(VirtualCamera);

        RoomSize.RoomSizeChanged += (obj, arg) =>
        {
            orbitTarget.transform.position = new Vector3(0, 0, 0);
        };

        Selectable.SelectionChanged += (obj, arg) =>
        {
            if (orbitMode == OrbitMode.SelectableLocked) UpdateTarget();
        };
    }

    void FixedUpdate()
    {
        if (CameraManager.ActiveCamera != VirtualCamera) return;

        float h = Input.GetAxis("Mouse X");
        float v = Input.GetAxis("Mouse Y");

        if (Input.GetMouseButton(0))
        {
            orbitTarget.transform.Rotate(new Vector3(0, h, 0));
        }

        if (Input.GetMouseButton(1))
        {
            if (orbitMode == OrbitMode.SelectableLocked && Selectable.SelectedSelectable != null) return;

            orbitRigidBody.AddRelativeForce(new Vector3(h, 0, v), ForceMode.Impulse);
        }

        if (Input.GetMouseButtonUp(1))
        {
            orbitRigidBody.velocity = Vector3.zero;
        }

        float fov = VirtualCamera.m_Lens.OrthographicSize;
        fov += -Input.GetAxis("Mouse ScrollWheel") * 0.5f;
        fov = Mathf.Clamp(fov, minFOV, maxFOV);
        VirtualCamera.m_Lens.OrthographicSize = fov;
    }

    void UpdateTarget()
    {
        if (Selectable.SelectedSelectable == null)
        {
            if (previousPosition == null) previousPosition = new Vector3(0, 0, 0);

            orbitTarget.transform.position = previousPosition;
        }
        else
        {
            previousPosition = orbitTarget.transform.position;

            orbitTarget.transform.position = Selectable.SelectedSelectable.transform.position;
        }
    }
}

public enum OrbitMode
{
    SelectableLocked,
    FreeMove,
    Locked
}
