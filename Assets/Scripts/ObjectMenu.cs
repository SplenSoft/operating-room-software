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
using FuzzySharp;

/// <summary>
/// Singleton object that displays a menu to instantiate selectables.
/// </summary>
public class ObjectMenu : MonoBehaviour
{
    public class ObjectMenuItem
    {
        public SelectableData SelectableData { get; set; }
        public SelectableMetaData SelectableMetaData { get; set; }
        public GameObject GameObject { get; set; }
        public string CustomFile { get; set; }
        public bool ValidForSearch { get; set; } = true;
        public double LevenshteinRatio { get; set; }
    }

    private static bool _selectCompatibleObjectsMode;
    public static ObjectMenu Instance { get; private set; }
    public static UnityEvent ActiveStateChanged { get; } = new();
    public static UnityEvent LastOpenedSelectableChanged { get; } = new();
    private static bool _initialized;

    private static List<string> _activeAssetBundleNames = new List<string>();

    private const int _minFuzzyRatio = 10;

    [field: SerializeField] 
    private GameObject ItemTemplate { get; set; }

    [field: SerializeField] 
    private TextMeshProUGUI ItemTemplateTextObjectName 
    { get; set; }

    [field: SerializeField]
    private TMP_InputField InputField_Search
    { get; set; }

    private AttachmentPoint _attachmentPoint;

    public List<ObjectMenuItem> ObjectMenuItems 
    { get; private set; } = new();

    public static Selectable LastOpenedSelectable 
    { get; private set; }

    public static SelectableData LastOpenedSelectableData 
    { get; private set; }

    private bool SearchIsActive => 
        !string.IsNullOrWhiteSpace(InputField_Search.text);

    #region Monobehaviour

    private void Awake()
    {
        Instance = this;

        InputField_Search.onValueChanged
            .AddListener(UpdateSearchFilter);
    }

    private void OnDestroy()
    {
        _initialized = false;
        _selectCompatibleObjectsMode = false;

        InputField_Search.onValueChanged
            .RemoveListener(UpdateSearchFilter);
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

    private void ClearSearchValidity()
    {
        InputField_Search.text = string.Empty;
        ObjectMenuItems.ForEach(x => x.ValidForSearch = true);
    }

    private void UpdateSearchFilter(string searchText)
    {
        if (!gameObject.activeSelf) return;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            ObjectMenuItems.ForEach(x => x.ValidForSearch = true);
            RefilterItems();
            return;
        }

        ObjectMenuItems.ForEach(x =>
        {
            if (x.SelectableMetaData == null) return;
            // Name
            double ratio = Fuzz.PartialRatio
                (x.SelectableMetaData.Name, searchText);

            // Categories
            foreach (var category in x.SelectableMetaData.Categories) 
            {
                ratio = Math.Max(ratio, Fuzz.Ratio(searchText, category));
            }

            // Keywords
            foreach (var keyword in x.SelectableMetaData.KeyWords)
            {
                ratio = Math.Max(ratio, Fuzz.Ratio(searchText, keyword));
            }

            x.LevenshteinRatio = ratio;

            if (ratio >= _minFuzzyRatio)
            {
                x.ValidForSearch = true;
            }
            else
            {
                x.LevenshteinRatio = 0;
                x.ValidForSearch = false;
            }
        });

        ObjectMenuItems = ObjectMenuItems.OrderByDescending(x => x.LevenshteinRatio).ToList();

        for (int i = 0; i < ObjectMenuItems.Count; i++)
        {
            ObjectMenuItems[i].GameObject.transform.SetSiblingIndex(i);
        }

        RefilterItems();
    }

    private void RefilterItems()
    {
        if (_selectCompatibleObjectsMode)
        {
            FilterMenuItems(_activeAssetBundleNames);
        }
        else if (_attachmentPoint != null)
        {
            FilterMenuItems(_attachmentPoint);
        }
        else
        {
            ClearMenuFilter();
        }
    }

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

            SelectableMetaData metadata = data.MetaData;

            if (task.Result.ResultType == Database.MetaDataOpertaionResultType.Success)
            {
                objectName = task.Result.MetaData.Name;
                metadata = task.Result.MetaData;
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

            ObjectMenuItems.Add(new ObjectMenuItem 
            { 
                SelectableData = data, 
                GameObject = newMenuItem,
                SelectableMetaData = metadata
            });
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

        ObjectMenuItems.Add(new ObjectMenuItem { GameObject = newMenuItem, CustomFile = f });
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
            .FirstOrDefault(x => x.Guid == attachmentPoint.MetaData.Guid);

        if (apData == default)
        {
            apData = selectableData.MetaData
                .AttachmentPointGuidMetaData
                .First(x => x.Guid == attachmentPoint.MetaData.Guid);
        }

        ObjectMenuItems.ForEach(item =>
        {
            if (item.SelectableData == null)
            {
                item.GameObject.SetActive(false);
                return;
            }

            if (SearchIsActive && !item.ValidForSearch)
            {
                item.GameObject.SetActive(false);
                return;
            }

            var compareMetaData = item.SelectableMetaData;

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
            if (item.SelectableData == null || 
            assetBundleNames.Contains(item.SelectableData.AssetBundleName) || 
            (SearchIsActive && !item.ValidForSearch))
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
                item.GameObject.SetActive
                    (SceneManager.GetActiveScene().name != "ObjectEditor");
                return;
            }

            if (SearchIsActive && !item.ValidForSearch) 
            {
                item.GameObject.SetActive(false);
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
        _activeAssetBundleNames = assetBundleNames;
        _selectCompatibleObjectsMode = true;
        Instance.ClearSearchValidity();
        Instance.FilterMenuItems(assetBundleNames);
        Instance.gameObject.SetActive(true);
    }

    public async void Open()
    {
        while (!_initialized) 
            await Task.Yield();

        if (!Application.isPlaying) return;

        Instance.ClearSearchValidity();
        ClearMenuFilter();
        _attachmentPoint = null;
        gameObject.SetActive(true);
    }

    public static async void Open(AttachmentPoint attachmentPoint)
    {
        while (!_initialized)
            await Task.Yield();

        if (!Application.isPlaying) return;

        Instance.ClearSearchValidity();
        Instance._attachmentPoint = attachmentPoint;
        Instance.FilterMenuItems(attachmentPoint);
        Instance.gameObject.SetActive(true);
    }
}