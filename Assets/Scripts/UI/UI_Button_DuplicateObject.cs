using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UI_Button_DuplicateObject : MonoBehaviour
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

        bool active = Selectable.SelectedSelectables.Count > 0;

        if (active && !Selectable.SelectedSelectables
        .Any(x => x.IsDestructible))
            return;

        gameObject.SetActive(active);
    }

    private void OnDestroy()
    {
        Selectable.SelectionChanged -= UpdateActiveState;
    }

    public void DeleteSelectedSelectable()
    {
        UI_DialogPrompt.Open("Are you sure you want to Duplicate this object?",
            new ButtonAction
            {
                ButtonText = "Yes",
                Action = () =>
                {
                    var selectables = Selectable.SelectedSelectables;
                    DuplicateObject(selectables[0].gameObject);
                    //Destroy(selectables[0].gameObject);
                    UI_DialogPrompt.Close();
                },
            },
            new ButtonAction
            {
                ButtonText = "Cancel"
            }
        );
    }


    void DuplicateObject(GameObject obj)
    {
        Vector3 objPos = obj.transform.position;
        Debug.Log(objPos);
        GameObject newObj = Instantiate(obj);
        newObj.transform.position = new Vector3(objPos.x+0.3f, objPos.y, objPos.z);
    }
}
