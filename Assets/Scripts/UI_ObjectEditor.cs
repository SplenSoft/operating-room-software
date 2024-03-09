using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class UI_ObjectEditor : MonoBehaviour
{ 
    [field: SerializeField] 
    private TextMeshProUGUI Title 
    { get; set; }

    [field: SerializeField] 
    private TMP_InputField InputField_ObjectName 
    { get; set; }

    [field: SerializeField]
    private TMP_InputField InputField_ObjectSubPartName
    { get; set; }

    [field: SerializeField] 
    private TextMeshProUGUI List_Keywords 
    { get; set; }

    [field: SerializeField]
    private TextMeshProUGUI List_Categories
    { get; set; }

    [field: SerializeField] 
    private Toggle Toggle_IsEnabled 
    { get; set; }

    private void Awake()
    {
        ObjectMenu.LastOpenedSelectableChanged
            .AddListener(LastOpenedSelectableChanged);
    }

    private void LastOpenedSelectableChanged()
    {
        Selectable selectable = ObjectMenu.LastOpenedSelectable;

        bool active = selectable != null && 
            !selectable.IsDestroyed && 
            Selectable.ActiveSelectables.Count > 0;

        if ((active && !gameObject.activeSelf) || 
        (!active && gameObject.activeSelf))
            gameObject.SetActive(active);

        string assetBundleName = ObjectMenu.LastOpenedSelectableData.AssetBundleName;

        // get metadata from database
    }

    private void OnDestroy()
    {
        ObjectMenu.LastOpenedSelectableChanged
            .RemoveListener(LastOpenedSelectableChanged);
    }
}