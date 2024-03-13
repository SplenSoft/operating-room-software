using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton object that displays a menu to instantiate selectables.
/// </summary>
public class ObjectMenu : MonoBehaviour
{
    private static bool _selectCompatibleObjectsMode;
    public static ObjectMenu Instance { get; private set; }
    public static UnityEvent ActiveStateChanged { get; } = new();
    public static UnityEvent LastOpenedSelectableChanged { get; } = new();
    private static bool _initialized;

    [field: SerializeField] 
    private GameObject ItemTemplate { get; set; }

    [field: SerializeField] 
    private TextMeshProUGUI ItemTemplateTextObjectName { get; set; }

    private AttachmentPoint _attachmentPoint;
    private List<ObjectMenuItem> ObjectMenuItems 
    { get; set; } = new();

    private class ObjectMenuItem
    {
        public SelectableData SelectableData { get; set; }
        public GameObject GameObject { get; set; }
        public string customFile { get; set; }
    }

    public static Selectable LastOpenedSelectable 
    { get; private set; }

    public static SelectableData LastOpenedSelectableData 
    { get; private set; }

    #region Monobehaviour

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        _initialized = false;
        _selectCompatibleObjectsMode = false;
    }

    private IEnumerator Start()
    {
        var loadingToken = Loading.GetLoadingToken();

        yield return new WaitUntil
            (() => SelectableAssetBundles.Initialized);

        Initialize();
        loadingToken.Done();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        _selectCompatibleObjectsMode = false;
        ActiveStateChanged?.Invoke();
    }

    private void OnEnable()
    {
        ActiveStateChanged?.Invoke();
    }

    #endregion

    public static void Regenerate()
    {
        foreach (var item in Instance.ObjectMenuItems)
        {
            Destroy(item.GameObject);
        }

        Instance.ObjectMenuItems.Clear();
        Instance.Initialize();
    }

    private async void Initialize()
    {
        var loadingToken = Loading.GetLoadingToken();

        while (!Database.Initialized || !SelectableAssetBundles.Initialized)
        {
            await Task.Yield();
            if (!Application.isPlaying) return;
        }

        SelectableAssetBundles.GetSelectableData()
        .ToList().ForEach(async data =>
        {
            // if we still need this, we can add it to SelectableData
            //if (prefab.TryGetComponent(out ObjectMenuIgnore ignore))
            //{
            //    return;
            //}

            // todo: Get data from database
            string objectName = data.PrefabName;

            var task = Database.GetMetaData(data.AssetBundleName, data.MetaData);
            await task;

            if (!Application.isPlaying)
                throw new Exception($"App quit while downloading");

            if (task.Result.ResultType == Database.MetaDataOpertaionResultType.Success)
            {
                objectName = task.Result.MetaData.Name;
            }

            ItemTemplateTextObjectName.text = objectName;
            var newMenuItem = Instantiate(ItemTemplate, ItemTemplate.transform.parent);
            newMenuItem.GetComponentInChildren<Button>().onClick.AddListener(async () =>
            {
                if (_selectCompatibleObjectsMode)
                {
                    UI_ObjectEditor.AddCompatibleObject(data.AssetBundleName);
                    return;
                }

                gameObject.SetActive(false);

                var task = data.GetPrefab();
                await task;

                if (!Application.isPlaying) return;

                GameObject prefab = task.Result;

                var newSelectableGameObject = Instantiate(prefab);
                var selectable2 = newSelectableGameObject.GetComponent<Selectable>();
                LastOpenedSelectable = selectable2;
                LastOpenedSelectableData = data;
                LastOpenedSelectableChanged?.Invoke();

                if (SceneManager.GetActiveScene().name == "ObjectEditor")
                {
                    return;
                }

                if (_attachmentPoint != null)
                {
                    _attachmentPoint.SetAttachedSelectable(selectable2);
                    selectable2.ParentAttachmentPoint = _attachmentPoint;

                    newSelectableGameObject.transform
                        .SetPositionAndRotation
                        (_attachmentPoint.transform.position,
                        _attachmentPoint.transform.rotation);

                    newSelectableGameObject.transform.parent = _attachmentPoint.transform;
                }
                else
                {
                    selectable2.StartRaycastPlacementMode();
                }
            });

            ObjectMenuItems.Add(new ObjectMenuItem { SelectableData = data, GameObject = newMenuItem });
        });

        AddSavedRoomConfigs();

        ItemTemplate.SetActive(false);

        _initialized = true;

        loadingToken.Done();
        Database.SetIsUpToDate();
    }

    private void AddSavedRoomConfigs()
    {
        if (Directory.Exists(Application.persistentDataPath + "/Saved/Configs/"))
        {
            string[] files = Directory.GetFiles(Application.persistentDataPath + "/Saved/Configs/");
            foreach (string f in files.Where(x => x.EndsWith(".json")))
            {
                AddCustomMenuItem(f);
            }
        }

        AddCustomMenuItem(Application.streamingAssetsPath + "/Sample_Arm_Config.json");
    }

    public async void AddCustomMenuItem(string f)
    {
        ItemTemplate.SetActive(true);
        string configName = Path.GetFileName(f).Replace(".json", "").Replace("_", " ");

        ItemTemplateTextObjectName.text = configName;
        GameObject newMenuItem = Instantiate(ItemTemplate, ItemTemplate.transform.parent);
        newMenuItem.GetComponentInChildren<Button>().onClick.AddListener(async () =>
        {
            gameObject.SetActive(false);
            GameObject newSelectable = await ConfigurationManager.Instance.LoadConfig(f);
            if (newSelectable == null)
            {
                Debug.LogError("Something went wrong with LoadConfig!!");
            }

            Selectable selectable = newSelectable.GetComponent<Selectable>();
            selectable.StartRaycastPlacementMode();
        });

        ObjectMenuItems.Add(new ObjectMenuItem { GameObject = newMenuItem, customFile = f });
        ItemTemplate.SetActive(false);
    }

    private async void FilterMenuItems(AttachmentPoint attachmentPoint)
    {
        var selectableData = SelectableAssetBundles.GetSelectableData()
            .Where(x => x.MetaData.AttachmentPointGuidMetaData
            .FirstOrDefault(y => y.Guid == attachmentPoint.MetaData.Guid) != default).First();

        var task = Database.GetMetaData(
            selectableData.AssetBundleName, 
            selectableData.MetaData);

        await task;

        if (!Application.isPlaying)
            throw new Exception($"App quit during task");

        if (task.Result.ResultType != Database.MetaDataOpertaionResultType.Success)
        {
            UI_DialogPrompt.Open($"Error: {task.Result.ErrorMessage}");
            return;
        }

        var metaData = task.Result.MetaData;

        var apData = metaData
            .AttachmentPointGuidMetaData
            .First(x => x.Guid == attachmentPoint.MetaData.Guid);

        ObjectMenuItems.ForEach(async item =>
        {
            if (item.SelectableData == null)
            {
                item.GameObject.SetActive(false);
                return;
            }

            var task = Database.GetMetaData(
                item.SelectableData.AssetBundleName, 
                item.SelectableData.MetaData);

            await task;

            if (!Application.isPlaying)
                throw new Exception($"App quit during task");

            if (task.Result.ResultType != Database.MetaDataOpertaionResultType.Success)
            {
                UI_DialogPrompt.Open($"Error: {task.Result.ErrorMessage}");
                return;
            }

            var compareMetaData = task.Result.MetaData;

            foreach (var category in compareMetaData.Categories)
            {
                if (apData.MetaData
                    .AllowedSelectableCategories.Contains(category))
                {
                    item.GameObject.SetActive(true);
                    return;
                }
            }

            if (apData.MetaData
                .AllowedSelectableAssetBundleNames
                .Contains(item.SelectableData.AssetBundleName))
            {
                item.GameObject.SetActive(true);
                return;
            }

            item.GameObject.SetActive(false);
        });
    }

    private void FilterMenuItems(List<string> assetBundleNames)
    {
        ObjectMenuItems.ForEach(item =>
        {
            if (item.SelectableData == null)
            {
                item.GameObject.SetActive(false);
                return;
            }

            if (assetBundleNames.Contains(item.SelectableData.AssetBundleName))
            {
                item.GameObject.SetActive(false);
                return;
            }

            item.GameObject.SetActive(true);
        });
    }

    private void ClearMenuFilter()
    {
        ObjectMenuItems.ForEach(item =>
        {
            if (item.SelectableData == null)
            {
                item.GameObject.SetActive(true);
                return;
            }

            if (SceneManager.GetActiveScene().name == "ObjectEditor")
            {
                item.GameObject.SetActive(true);
                return;
            }

            item.GameObject.SetActive
                (item.SelectableData
                .MetaData.IsStandalone);
        });
    }

    public static void OpenToSelectCompatibleObjects(List<string> assetBundleNames)
    {
        _selectCompatibleObjectsMode = true;
        Instance.FilterMenuItems(assetBundleNames);
        Instance.gameObject.SetActive(true);
    }

    public async void Open()
    {
        while (!_initialized) 
            await Task.Yield();

        if (!Application.isPlaying) return;

        ClearMenuFilter();
        _attachmentPoint = null;
        gameObject.SetActive(true);
    }

    public static async void Open(AttachmentPoint attachmentPoint)
    {
        while (!_initialized)
            await Task.Yield();

        if (!Application.isPlaying) return;

        Instance._attachmentPoint = attachmentPoint;
        Instance.FilterMenuItems(attachmentPoint);
        Instance.gameObject.SetActive(true);
    }
}