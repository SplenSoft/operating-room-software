using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private static List<CinemachineVirtualCamera> _cameras = new List<CinemachineVirtualCamera>();

    public static void Register(CinemachineVirtualCamera cam)
    {
        _cameras.Add(cam);
    }

    public static void CycleCam()
    {
        var activeCam = (CinemachineVirtualCamera)CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera;
        activeCam.Priority = 0;
        int indexOfActiveCam = _cameras.IndexOf(activeCam);
        int nextCam = indexOfActiveCam + 1;

        if (nextCam >= _cameras.Count) nextCam = 0;
        _cameras[nextCam].Priority = 11;
    }
}