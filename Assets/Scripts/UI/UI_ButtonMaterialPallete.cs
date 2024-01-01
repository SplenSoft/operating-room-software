using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ButtonMaterialPallete : MonoBehaviour
{
    Button button;
    MaterialPalette palette; // internal reference to the material palette object
    public UI_MaterialPallete ui_pallete;

    private void Awake()
    {
        Selectable.SelectionChanged += (o, e) => 
        {
            ui_pallete.ClearPalleteOptions();
            bool b = false;
            if(Selectable.SelectedSelectable.gameObject.GetComponent<MaterialPalette>() != null) b = true;

            gameObject.SetActive(b);

            if(b)
            {
                palette = Selectable.SelectedSelectable.gameObject.GetComponent<MaterialPalette>();
            }
        };

        button.onClick.AddListener(() => DisplayPallete());

        gameObject.SetActive(false);
    }

    void DisplayPallete()
    {
        if(ui_pallete.pallete.Count > 0)
        {
            ui_pallete.ClearPalleteOptions();
        }
        else
        {
            ui_pallete.LoadPalleteOptions(palette.materials);
        }
    }
}
