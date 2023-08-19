using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UI_DialogPrompt : MonoBehaviour
{
    private static UI_DialogPrompt Instance { get; set; }
    [field: SerializeField] private Button ButtonYes { get; set; }

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public static void Open(Action onButtonYes)
    {
        Instance.ButtonYes.onClick.RemoveAllListeners();
        Instance.ButtonYes.onClick.AddListener(() => 
        {
            onButtonYes?.Invoke();
            Instance.gameObject.SetActive(false);
        });
        Instance.gameObject.SetActive(true);
    }
}
