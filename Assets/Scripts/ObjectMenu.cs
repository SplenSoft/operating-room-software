using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Singleton object that displays a menu to instantiate selectables.
/// </summary>
public class ObjectMenu : MonoBehaviour
{
    public static ObjectMenu Instance { get; private set; }
    public static UnityEvent ActiveStateChanged { get; } = new();
    private static bool _initialized;

    [field: SerializeField] 
    private GameObject ItemTemplate { get; set; }

    [field: SerializeField] 
    private TextMeshProUGUI ItemTemplateTextObjectName { get; set; }

    private AttachmentPoint _attachmentPoint;
    private List<ObjectMenuItem> ObjectMenuItems { get; set; } = new();

    private class ObjectMenuItem
    {
        public Selectable Selectable { get; set; }
        public GameObject GameObject { get; set; }
        public string customFile { get; set; }
    }

    #region Monobehaviour

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _initialized = false;
    }

    private IEnumerator Start()
    {
        var loadingToken = Loading.GetLoadingToken();

        yield return new WaitUntil
            (() => SelectableAssetBundles.Initialized);

        Initialize();
        loadingToken.Done();
    }

    private void OnDisable()
    {
        ActiveStateChanged?.Invoke();
    }

    private void OnEnable()
    {
        ActiveStateChanged?.Invoke();
    }

    #endregion

    private async void Initialize()
    {
        var loadingToken = Loading.GetLoadingToken();

        while (!SelectableAssetBundles.Initialized) 
            await Task.Yield();

        if (!Application.isPlaying) return;

        foreach (var data in SelectableAssetBundles.GetSelectableData())
        {
            // if we still need this, we can add it to SelectableData
            //if (prefab.TryGetComponent(out ObjectMenuIgnore ignore))
            //{
            //    return;
            //}

            // todo: Get data from database

            //var selectable = prefab.GetComponent<Selectable>();
            ItemTemplateTextObjectName.text = data.PrefabName;
            var newMenuItem = Instantiate(ItemTemplate, ItemTemplate.transform.parent);
            newMenuItem.GetComponentInChildren<Button>().onClick.AddListener(async () =>
            {
                gameObject.SetActive(false);

                var task = data.GetPrefab();
                await task;

                if (!Application.isPlaying) return;

                GameObject prefab = task.Result;

                var newSelectableGameObject = Instantiate(prefab);
                var selectable2 = newSelectableGameObject.GetComponent<Selectable>();
                if (_attachmentPoint != null)
                {
                    _attachmentPoint.SetAttachedSelectable(selectable2);
                    selectable2.ParentAttachmentPoint = _attachmentPoint;
                    newSelectableGameObject.transform.SetPositionAndRotation(_attachmentPoint.transform.position, _attachmentPoint.transform.rotation);
                    newSelectableGameObject.transform.parent = _attachmentPoint.transform;
                }
                else
                {
                    selectable2.StartRaycastPlacementMode();
                }
            });

            ObjectMenuItems.Add(new ObjectMenuItem { Selectable = selectable, GameObject = newMenuItem });
        }

        AddSavedRoomConfigs();

        ItemTemplate.SetActive(false);

        _initialized = true;

        loadingToken.Done();
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
            GameObject newSelectable = await ConfigurationManager._instance.LoadConfig(f);
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

    private void FilterMenuItems(AttachmentPoint attachmentPoint)
    {
        ObjectMenuItems.ForEach(item =>
        {
            //if (attachmentPoint.AllowedSelectableTypes.Count == 0 && attachmentPoint.AllowedSelectables.Count == 0)
            //{
            //    item.GameObject.SetActive(false);
            //    return;
            //}

            if (item.Selectable == null)
            {
                item.GameObject.SetActive(false);
                return;
            }

            foreach (var type in item.Selectable.Types)
            {
                if (attachmentPoint.AllowedSelectableTypes.Contains(type))
                {
                    item.GameObject.SetActive(true);
                    return;
                }
            }

            if (attachmentPoint.AllowedSelectables.Contains(item.Selectable))
            {
                item.GameObject.SetActive(true);
                return;
            }
            item.GameObject.SetActive(false);
        });
    }

    private void ClearMenuFilter()
    {
        ObjectMenuItems.ForEach(item =>
        {
            if (item.Selectable == null)
            {
                item.GameObject.SetActive(true);
                return;
            }

            bool isMount = item.Selectable.Types.Contains(SpecialSelectableType.Mount);
            bool isFurniture = item.Selectable.Types.Contains(SpecialSelectableType.Furniture);
            bool isWall = item.Selectable.Types.Contains(SpecialSelectableType.Wall);
            bool isCeilingLight = item.Selectable.Types.Contains(SpecialSelectableType.CeilingLight);
            bool isDoor = item.Selectable.Types.Contains(SpecialSelectableType.Door);
            item.GameObject.SetActive(isMount || isFurniture || isWall || isCeilingLight || isDoor);
        });
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
