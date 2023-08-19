using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ButtonOpenObjectMenu : MonoBehaviour
{
    private void Awake()
    {
        CameraManager.CameraChanged.AddListener(UpdateVisibility);
        UI_ToggleShowCeilingObjects.CeilingObjectVisibilityToggled.AddListener(UpdateVisibility);
    }

    private void UpdateVisibility()
    {
        gameObject.SetActive(CanBeVisible());
    }

    private bool CanBeVisible()
    {
        bool isOrthoCeilingCam = CameraManager.ActiveCamera.GetComponent<OperatingRoomCamera>().CameraType == OperatingRoomCameraType.OrthoCeiling;
        bool isFreeLookCam = CameraManager.ActiveCamera == FreeLookCam.Instance.VirtualCamera;
        return isFreeLookCam || isOrthoCeilingCam;
    }
}
