using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
            .AddListener(UpdateState);

        Selectable.ActiveSelectablesInSceneChanged
            .AddListener(UpdateState);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        ObjectMenu.LastOpenedSelectableChanged
            .RemoveListener(UpdateState);

        Selectable.ActiveSelectablesInSceneChanged
            .RemoveListener(UpdateState);
    }

    private async void UpdateState()
    {
        Selectable selectable = ObjectMenu.LastOpenedSelectable;

        bool active = selectable != null && 
            !selectable.IsDestroyed && 
            Selectable.ActiveSelectables.Count > 0;

        if ((active && !gameObject.activeSelf) || 
        (!active && gameObject.activeSelf))
            gameObject.SetActive(active);

        if (!active) return;

        string assetBundleName = ObjectMenu.LastOpenedSelectableData.AssetBundleName;

        var task = Database.GetMetaData(assetBundleName, selectable.MetaData);
        await task;

        if (task.Result.ResultType != Database.MetaDataOpertaionResultType.Success)
        {
            // todo: show a bad UI
            return;
        }

        //populate UI
        var metadata = JsonConvert.DeserializeObject
            <SelectableMetaData>(task.Result.Message);

        Title.text = metadata.Name;
        InputField_ObjectName.text = metadata.Name;

        HandleList(List_Keywords, metadata.KeyWords, "keywords");
        HandleList(List_Categories, metadata.Categories, "categories");

        Toggle_IsEnabled.SetIsOnWithoutNotify(metadata.Enabled);
    }

    private void HandleList(TextMeshProUGUI inputField, List<string> list, string name)
    {
        if (list.Count == 0)
        {
            inputField.text = $"No {name}";
        }
        else
        {
            var stringBuilder = new StringBuilder();
            list.ForEach(word =>
            {
                stringBuilder.AppendLine(word);
            });
            inputField.text = stringBuilder.ToString();
        }
    }

    
}