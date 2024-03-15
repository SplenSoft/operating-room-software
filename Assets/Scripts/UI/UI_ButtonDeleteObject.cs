using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ButtonDeleteObject : MonoBehaviour
{
    private void Awake()
    {
        Selectable.SelectionChanged += UpdateActiveState;

        gameObject.SetActive(false);
    }

    private void UpdateActiveState()
    {
        if (!Application.isPlaying) return;
        if (this == null || gameObject == null) return;

        bool active = Selectable.SelectedSelectable != null;
        if(active) if(!Selectable.SelectedSelectable.IsDestructible) return;
        gameObject.SetActive(active);
    }

    private void OnDestroy()
    {
        Selectable.SelectionChanged -= UpdateActiveState;
    }

    public void DeleteSelectedSelectable()
    {
        UI_DialogPrompt.Open("Are you sure you want to delete this object?",
            new ButtonAction
            {
                ButtonText = "Yes",
                Action = () =>
                    {
                        var selectable = Selectable.SelectedSelectable;
                        Selectable.DeselectAll();
                        Destroy(selectable.gameObject);
                    },
            },
            new ButtonAction
            {
                ButtonText = "Cancel"
            }
        );
    }
}