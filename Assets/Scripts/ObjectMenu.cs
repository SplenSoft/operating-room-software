using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ObjectMenu : MonoBehaviour
{
    private static ObjectMenu Instance { get; set; }
    public static UnityEvent ActiveStateChanged { get; } = new();

    [field: SerializeField] private GameObject ItemTemplate { get; set; }
    [field: SerializeField] private TextMeshProUGUI ItemTemplateTextObjectName { get; set; }
    //[field: SerializeField] private Button ItemTemplateButtonAddObject { get; set; }
    [field: SerializeField] private List<GameObject> BuiltInSelectablePrefabs { get; set; } = new();
    private AttachmentPoint _attachmentPoint;
    private List<ObjectMenuItem> ObjectMenuItems { get; set; } = new();

    private class ObjectMenuItem
    {
        public Selectable Selectable { get; set; }
        public GameObject GameObject { get; set; }
    }

    private void Awake()
    {
        Instance = this;
        InstantiateMenuItems();
        gameObject.SetActive(false);
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
            bool isMount = item.Selectable.Types.Contains(SelectableType.Mount);
            bool isFurniture = item.Selectable.Types.Contains(SelectableType.Furniture);
            bool isWall = item.Selectable.Types.Contains(SelectableType.Wall);
            item.GameObject.SetActive(isMount || isFurniture || isWall);
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
