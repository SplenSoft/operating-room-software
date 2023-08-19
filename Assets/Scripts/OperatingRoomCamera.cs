using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OperatingRoomCamera : MonoBehaviour
{
    [field: SerializeField] public OperatingRoomCameraType CameraType { get; private set; }
    public static OperatingRoomCamera LiveCamera { get; private set; }

    public void OnCameraLive()
    {
        LiveCamera = this;
    }
}

public enum OperatingRoomCameraType
{
    OrthoCeiling,
    FreeLook,
    OrthoSide
}