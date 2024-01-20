using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.UI;

public class ObjectMenu : MonoBehaviour
{
    public static ObjectMenu Instance { get; private set; }
    public static UnityEvent ActiveStateChanged { get; } = new();

    [field: SerializeField] private GameObject ItemTemplate { get; set; }
    [field: SerializeField] private TextMeshProUGUI ItemTemplateTextObjectName { get; set; }
    [field: SerializeField] private List<GameObject> BuiltInSelectablePrefabs { get; set; } = new();
    private AttachmentPoint _attachmentPoint;
    private List<ObjectMenuItem> ObjectMenuItems { get; set; } = new();

    private class ObjectMenuItem
    {
        public Selectable Selectable { get; set; }
        public GameObject GameObject { get; set; }
        public string customFile { get; set; }
    }

    private void Awake()
    {
        Instance = this;
        InstantiateMenuItems();
        gameObject.SetActive(false);
    }

    public GameObject GetPrefabByGUID(string guid)
    {
        return BuiltInSelectablePrefabs.Single(s => s.GetComponent<Selectable>().GUID == guid);
    }

    private void InstantiateMenuItems()
    {
        BuiltInSelectablePrefabs.ForEach(prefab =>
        {
            var selectable = prefab.GetComponent<Selectable>();
            ItemTemplateTextObjectName.text = selectable.Name;
            var newMenuItem = Instantiate(ItemTemplate, ItemTemplate.transform.parent);
            newMenuItem.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
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
        });

        if (Directory.Exists(Application.persistentDataPath + "/Saved/Configs/"))
        {
            string[] files = Directory.GetFiles(Application.persistentDataPath + "/Saved/Configs/");
            foreach (string f in files.Where(x => x.EndsWith(".json")))
            {
                AddCustomMenuItem(f);
            }
        }

        AddCustomMenuItem(Application.streamingAssetsPath + "/Sample_Arm_Config.json");

        ItemTemplate.SetActive(false);
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

    private void OnDisable()
    {
        ActiveStateChanged?.Invoke();
    }

    private void OnEnable()
    {
        ActiveStateChanged?.Invoke();
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

            bool isMount = item.Selectable.Types.Contains(SelectableType.Mount);
            bool isFurniture = item.Selectable.Types.Contains(SelectableType.Furniture);
            bool isWall = item.Selectable.Types.Contains(SelectableType.Wall);
            bool isCeilingLight = item.Selectable.Types.Contains(SelectableType.CeilingLight);
            bool isDoor = item.Selectable.Types.Contains(SelectableType.Door);
            item.GameObject.SetActive(isMount || isFurniture || isWall || isCeilingLight || isDoor);
        });
    }

    public void Open()
    {
        ClearMenuFilter();
        _attachmentPoint = null;
        gameObject.SetActive(true);
    }

    public static void Open(AttachmentPoint attachmentPoint)
    {
        Instance._attachmentPoint = attachmentPoint;
        Instance.FilterMenuItems(attachmentPoint);
        Instance.gameObject.SetActive(true);
    }
}
