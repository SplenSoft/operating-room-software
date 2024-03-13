using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Save : MonoBehaviour
{
    [Header("Constant UI")]
    public GameObject savePanel;
    public Button b_Save;
    public Button b_Confirm;
    public Button[] b_Cancel;
    public TMP_InputField fileName;

    [Header("Dynamic UI")]
    public TMP_Text header;

    void Start()
    {
        b_Save.onClick.AddListener(() =>
        {
            if (Selectable.SelectedSelectable == null)
            {
                header.text = "Save Room";
            }
            else
            {
                header.text = "Save Configuration";
            }

            FreeLookCam.Instance.isLocked = true;
            savePanel.SetActive(true);
        });

        b_Confirm.onClick.AddListener(() =>
        {
            if (Selectable.SelectedSelectable == null)
            {
                ConfigurationManager.Instance.SaveRoom(fileName.text.Replace(" ", "_"));
            }
            else
            {
                ConfigurationManager.Instance.SaveConfiguration(fileName.text.Replace(" ", "_"));
            }

            fileName.text = "";
            FreeLookCam.Instance.isLocked = false;
            savePanel.SetActive(false);
        });

        foreach (Button b in b_Cancel)
        {
            b.onClick.AddListener(() =>
            {
                savePanel.SetActive(false);
                FreeLookCam.Instance.isLocked = false;
            });
        }

        savePanel.SetActive(false);
    }
}
