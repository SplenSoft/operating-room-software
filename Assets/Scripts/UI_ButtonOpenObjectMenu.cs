using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ButtonOpenObjectMenu : MonoBehaviour
{
    private void Awake()
    {
        CameraManager.CameraChanged.AddListener(() =>
        {
            gameObject.SetActive(CameraManager.ActiveCamera == FreeLookCam.Instance.VirtualCamera);
        });
    }
}
