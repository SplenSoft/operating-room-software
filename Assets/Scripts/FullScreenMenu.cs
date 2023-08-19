using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullScreenMenu : MonoBehaviour
{
    private static List<FullScreenMenu> _openMenus = new();
    public static bool IsOpen => _openMenus.Count > 0;

    private void OnEnable()
    {
        if (!_openMenus.Contains(this))
            _openMenus.Add(this);
    }

    private void OnDisable()
    {
        _openMenus.Remove(this);
    }
}