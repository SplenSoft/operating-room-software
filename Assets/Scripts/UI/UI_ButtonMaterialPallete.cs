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
        button = GetComponent<Button>();

        Selectable.SelectionChanged += (o, e) => 
        {
            palette = null;
            ui_pallete.ClearPalleteOptions();
            bool b = Selectable.SelectedSelectable.gameObject.TryGetComponent<MaterialPalette>(out MaterialPalette m);

            gameObject.SetActive(b);

            if(b)
            {
                palette = m;
            }
        };

        button.onClick.AddListener(() => DisplayPallete());

        gameObject.SetActive(false);
    }

    void DisplayPallete()
    {
        if(palette == null || ui_pallete.pallete.Count != 0)
        {
            ui_pallete.ClearPalleteOptions();
        }
        else
        {
            ui_pallete.LoadPalleteOptions(palette.materials);
        }
    }
}
