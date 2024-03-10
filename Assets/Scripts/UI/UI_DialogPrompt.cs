using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DialogPrompt : MonoBehaviour
{
    private static UI_DialogPrompt Instance { get; set; }
    [field: SerializeField] private Button ButtonTemplate { get; set; }
    [field: SerializeField] private TextMeshProUGUI Text { get; set; }

    private List<Button> _instantiatedButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public static void Open(string text, params ButtonAction[] buttonActions)
    {
        Instance.Text.text = text;

        while (Instance._instantiatedButtons.Count < buttonActions.Length) 
        {
            var newObj = Instantiate(Instance.ButtonTemplate.gameObject, 
                Instance.ButtonTemplate.transform.parent);

            Instance._instantiatedButtons.Add (newObj.GetComponent<Button>());
        }

        Instance._instantiatedButtons.ForEach(button => button.gameObject.SetActive(false));

        for (int i = 0; i < buttonActions.Length; i++)
        {
            var button = Instance._instantiatedButtons[i];
            var buttonAction = buttonActions[i];

            button.gameObject.SetActive(true);

            button.GetComponentInChildren<TextMeshProUGUI>()
                .text = buttonAction.ButtonText.ToString();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => buttonAction.Action?.Invoke());
            button.onClick.AddListener(() => Instance.gameObject.SetActive(false));
        }
        Instance.gameObject.SetActive(true);
    }
}

public class ButtonAction
{
    public string ButtonText { get; set; }
    public Action Action { get; set; }
}