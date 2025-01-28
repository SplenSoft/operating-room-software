using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LoadingScreen : MonoBehaviour
{
    private static UI_LoadingScreen _instance;

    [field: SerializeField] private Image LoadingBar { get; set; }
    [SerializeField] private TextMeshProUGUI progress;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Loading.LoadingTokensChanged.AddListener(UpdateState);

    }

    private void Start()
    {
      
        UpdateState();
    }

    private void Update()
    {
        LoadingBar.fillAmount = Loading.GetTotalProgress01();
        progress.text = "Please wait ...: " + Loading.GetTotalProgress01()*100 +"%";
    }

    private void OnDestroy()
    {
        Loading.LoadingTokensChanged.RemoveListener(UpdateState);
    }

    private void UpdateState()
    {
  
        gameObject.SetActive(Loading.LoadingActive);
    }
}
