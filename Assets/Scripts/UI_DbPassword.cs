using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

public class UI_DbPassword : MonoBehaviour
{
    public static UI_DbPassword Instance { get; private set; }

    [field: SerializeField] public TMP_InputField InputField_OldPassword { get; set; }
    [field: SerializeField] public TMP_InputField InputField_Password { get; set; }
    [field: SerializeField] public TextMeshProUGUI Text_EnterOldPassword { get; set; }
    [field: SerializeField] public TextMeshProUGUI Text_EnterPassword { get; set; }
    [field: SerializeField] public TextMeshProUGUI Text_PasswordDirections { get; set; }
    [field: SerializeField] public Button ButtonSubmit { get; set; }
    [field: SerializeField] public Button ButtonCancel { get; set; }

    private const string _passwordDirections = "Password must have eight characters, at least one number, at least one uppercase letter, at least one lowercase letter and at least one special character.";

    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InputField_Password.onValueChanged.AddListener(ValueChanged);
        gameObject.SetActive(false);

        ButtonCancel.onClick
            .AddListener(() => gameObject.SetActive(false));
    }

    private void OnDestroy()
    {
        InputField_Password.onValueChanged.RemoveListener(ValueChanged);
    }

    private void ValueChanged(string arg0)
    {
        bool valid = !Regex.IsMatch(
            arg0, 
            @"^(.{0,7}|[^0-9]*|[^A-Z]*|[^a-z]*|[a-zA-Z0-9]*)$");

        ButtonSubmit.interactable = valid;
    }

    public static void OpenChangePassword()
    {
        Instance.Text_EnterOldPassword.gameObject.SetActive(true);
        Instance.Text_EnterOldPassword.text = "Enter current password";
        Instance.InputField_OldPassword.gameObject.SetActive(true);
        Instance.Text_PasswordDirections.gameObject.SetActive(true);
        Instance.Text_PasswordDirections.text = _passwordDirections;
        Instance.Text_EnterPassword.text = "Enter new password";

        Instance.ButtonSubmit.onClick.RemoveAllListeners();
        Instance.ButtonSubmit.onClick.AddListener(Instance.ChangePassword);

        Instance.gameObject.SetActive(true);
    }

    public static void OpenEnterPassword()
    {
        Instance.Text_EnterOldPassword.gameObject.SetActive(false);
        Instance.InputField_OldPassword.gameObject.SetActive(false);
        Instance.Text_PasswordDirections.gameObject.SetActive(false);
        Instance.Text_EnterPassword.text = "Enter password";
        Instance.ButtonSubmit.onClick.AddListener(Instance.ValidatePassword);
        Instance.gameObject.SetActive(true);
    }

    public async void ChangePassword()
    {
        ToggleStates(false);

        var task = Database.ChangePassword
            (InputField_Password.text, 
            InputField_OldPassword.text);

        await task;

        if (!Application.isPlaying)
            throw new Exception("App quit during task");

        if (task.Result)
        {
            gameObject.SetActive(false);
        }

        ToggleStates(true);
    }

    private void ToggleStates(bool toggle)
    {
        InputField_OldPassword.interactable = toggle;
        InputField_Password.interactable = toggle;
        ButtonSubmit.interactable = toggle;
        ButtonCancel.interactable = toggle;
    }

    public async void ValidatePassword()
    {
        ToggleStates(false);

        var task = Database.ValidatePassword(InputField_Password.text);
        await task;

        if (!Application.isPlaying)
            throw new Exception("App quit during task");

        ToggleStates(true);

        if (task.Result)
        {
            gameObject.SetActive(false);
        }
    }
}
