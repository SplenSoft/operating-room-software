using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingsMenu : MonoBehaviour
{
    [field: SerializeField] private static GameObject _container { get; set; }
    [field: SerializeField] private Button b_CloseMenu { get; set; }
    
    void Awake()
    {
        _container = gameObject.transform.GetChild(0).gameObject;
        b_CloseMenu.onClick.AddListener(()=> ToggleShowSettings(false));
    }

    public static void ToggleShowSettings(bool show)
    {
        _container.SetActive(show);
    }
}
