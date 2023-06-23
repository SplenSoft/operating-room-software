using HighlightPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public static EventHandler SelectionChanged;
    public static Selectable SelectedSelectable { get; private set; }
    public bool IsSelected => SelectedSelectable == this;
    private bool _isRaycastPlacementMode;
    private bool _hasBeenPlaced;
    private Transform _virtualParent;

    [field: SerializeField] private HighlightEffect HighlightEffect { get; set; }
    [field: SerializeField] public Sprite Thumbnail { get; private set; }
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public string Description { get; private set; }


    private void Awake()
    {
        InputHandler.KeyStateChanged += InputHandler_KeyStateChanged;
    }

    private void OnDestroy()
    {
        InputHandler.KeyStateChanged -= InputHandler_KeyStateChanged;
    }

    public void OnMouseUpAsButton()
    {
        Select();
    }

    public async void StartRaycastPlacementMode()
    {
        DeselectAll();
        HighlightEffect.highlighted = true;
        await Task.Yield();
        if (!Application.isPlaying) return;

        _isRaycastPlacementMode = true;
    }

    private void Update()
    {
        UpdateRaycastPlacementMode();
    }

    private async void UpdateRaycastPlacementMode()
    {
        if (!_isRaycastPlacementMode || _hasBeenPlaced) return;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, 1 << LayerMask.NameToLayer("Wall"), QueryTriggerInteraction.Ignore))
        {
            transform.SetPositionAndRotation(raycastHit.point, Quaternion.LookRotation(raycastHit.normal));
            transform.Rotate(90, 0, 0);
            _virtualParent = raycastHit.collider.transform;
        }

        if (Input.GetMouseButtonUp(0))
        {
            HighlightEffect.highlighted = false;
            _hasBeenPlaced = true;
            SendMessage("SelectablePositionChanged");
            SendMessage("VirtualParentChanged", _virtualParent);
            await Task.Yield();
            if (!Application.isPlaying) return;

            _isRaycastPlacementMode = false;
        }
    }

    private void InputHandler_KeyStateChanged(object sender, KeyStateChangedEventArgs e)
    {
        if (e.KeyCode == KeyCode.Escape && e.KeyState == KeyState.ReleasedThisFrame)
        {
            Deselect();

            if (_isRaycastPlacementMode)
            {
                Destroy(gameObject);
            }
        }
        else if (e.KeyCode == KeyCode.Delete && e.KeyState == KeyState.ReleasedThisFrame && IsSelected)
        {
            Deselect();
            Destroy(gameObject);
        }
    }

    public static void DeselectAll()
    {
        if (SelectedSelectable != null)
        {
            if (SelectedSelectable.GetComponent<GizmoHandler>().GizmoUsedLastFrame) return;
            SelectedSelectable.Deselect();
            SelectionChanged?.Invoke(null, null);
        }
    }

    private void Deselect()
    {
        if (!IsSelected) return;
        SelectedSelectable = null;
        HighlightEffect.highlighted = false;
        SendMessage("SelectableDeselected");
    }

    private void Select()
    {
        if (IsSelected || _isRaycastPlacementMode) return;
        if (SelectedSelectable != null)
        {
            SelectedSelectable.Deselect();
        }
        SelectedSelectable = this;
        HighlightEffect.highlighted = true;
        SelectionChanged?.Invoke(this, null);
        SendMessage("SelectableSelected");
    }
}
