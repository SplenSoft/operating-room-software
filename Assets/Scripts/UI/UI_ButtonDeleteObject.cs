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

    private void UpdateActiveState(object sender, EventArgs e)
    {
        if (!Application.isPlaying) return;
        if (this == null || gameObject == null) return;

        bool active = Selectable.SelectedSelectable != null;
        gameObject.SetActive(active);
    }

    private void OnDestroy()
    {
        Selectable.SelectionChanged -= UpdateActiveState;
    }

    public void DeleteSelectedSelectable()
    {
        UI_DialogPrompt.Open(() =>
        {
            var selectable = Selectable.SelectedSelectable;
            Selectable.DeselectAll();
            Destroy(selectable.gameObject);
        });
        
    }
}