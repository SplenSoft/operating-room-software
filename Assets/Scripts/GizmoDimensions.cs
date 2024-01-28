using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GizmoDimensions : MonoBehaviour
{
    private TMP_Dropdown _dropdown;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();

        _dropdown.onValueChanged.AddListener(UpdateGizmoDimension);
        Selectable.SelectionChanged += UpdateActiveState;

        gameObject.SetActive(false);
    }

    void UpdateActiveState()
    {
        if (Selectable.SelectedSelectable != null)
        {
            if(Selectable.SelectedSelectable.AllowInverseControl)
            {
                gameObject.SetActive(true);
                Invoke("DelayedSync", 0.05f);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void DelayedSync()
    {
        _dropdown.value = 0;
        UpdateGizmoDimension(0);
    }

    void UpdateGizmoDimension(int selection)
    {
        GizmoHandler handler = Selectable.SelectedSelectable.GetComponent<GizmoHandler>();
        if(handler == null) return;
        if (selection == 0)
        {   
            handler._translateGizmo.Gizmo.MoveGizmo.Set2DModeEnabled(false);
        }
        else
        {
            handler._translateGizmo.Gizmo.MoveGizmo.Set2DModeEnabled(true);
        }
    }
}
