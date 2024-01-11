using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ButtonMaterialPallete : MonoBehaviour
{
    Button button;
    MaterialPalette palette; // internal reference to the material palette object
    public UI_MaterialPallete ui_pallete;
    public TMP_Text label;
    private int elementIncrement;

    private void Awake()
    {
        button = GetComponent<Button>();

        Selectable.SelectionChanged += (o, e) =>
        {
            bool b = false;
            palette = null;
            ui_pallete.ClearPalleteOptions();
            try
            {
                b = Selectable.SelectedSelectable.gameObject.TryGetComponent<MaterialPalette>(out MaterialPalette m);
                palette = m;
                elementIncrement = 0;
                label.text = "Show Material Palette";
            }
            catch
            {
                b = false;
            }

            gameObject.SetActive(b);
        };

        button.onClick.AddListener(() => DisplayPallete());

        gameObject.SetActive(false);
    }

    void DisplayPallete()
    {
        elementIncrement++;
        if (elementIncrement > palette.elements.Count())
        {
            ui_pallete.ClearPalleteOptions();
            label.text = "Show Material Palette";
            elementIncrement = 0;
            return;
        }
        else if(elementIncrement < palette.elements.Count())
        {
            label.text = "Next Material Region";
        }
        else
        {
            label.text = "Hide Material Palette";
        }

        ui_pallete.LoadPalleteOptions(palette.elements[elementIncrement - 1].materials, elementIncrement - 1);
    }
}
