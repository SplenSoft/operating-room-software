using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(FullScreenMenu))]
public class UI_PdfExportOptions : MonoBehaviour
{
    public static UI_PdfExportOptions Instance { get; private set; }

    [field: SerializeField]
    private TMP_InputField InputfieldTitle { get; set; }

    [field: SerializeField]
    private TMP_InputField InputfieldSubTitle { get; set; }

    private Selectable _selectable;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
    }

    public void ExportPdf()
    {
        if (_selectable.TryGetArmAssemblyRoot(out _))
        {
            _selectable.ExportElevationPdf(InputfieldTitle.text, InputfieldSubTitle.text);
            gameObject.SetActive(false);
        }
        else
        {
            throw new System.Exception("Something went wrong. Couldn't export PDF");
        }
    }

    public static void Open(Selectable selectable)
    {
        Instance._selectable = selectable;
        Instance.gameObject.SetActive(true);
    }
}