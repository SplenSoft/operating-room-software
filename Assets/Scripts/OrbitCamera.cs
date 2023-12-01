using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class OrbitCamera : MonoBehaviour
{
    [field: SerializeField] public CinemachineVirtualCamera VirtualCamera { get; private set; }

    public void Awake()
    {
        CameraManager.Register(VirtualCamera);
    }

    void Update()
    {
        if(Input.GetMouseButton(0) && CameraManager.ActiveCamera == VirtualCamera)
        {
            transform.RotateAround(new Vector3(0,0,0), transform.up, -Input.GetAxis("Mouse X")*10);
        }
    }
}
