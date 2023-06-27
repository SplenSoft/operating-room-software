using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectMenu : MonoBehaviour
{
    private static ObjectMenu Instance { get; set; }

    [field: SerializeField] private GameObject ItemTemplate { get; set; }
    [field: SerializeField] private TextMeshProUGUI ItemTemplateTextObjectName { get; set; }
    //[field: SerializeField] private Button ItemTemplateButtonAddObject { get; set; }
    [field: SerializeField] private List<GameObject> BuiltInSelectablePrefabs { get; set; } = new();
    private AttachmentPoint _attachmentPoint;

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
        });
        ItemTemplate.SetActive(false);
    }

    public void Open()
    {
        _attachmentPoint = null;
        gameObject.SetActive(true);
    }

    public static void Open(AttachmentPoint attachmentPoint)
    {
        Instance._attachmentPoint = attachmentPoint;
        Instance.gameObject.SetActive(true);
    }
}
