using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_MaterialPalleteSwatch : MonoBehaviour
{
    private Image image;
    private TMP_Text m_name;
    private Button b_ApplySwatch;
    private int element = 0;

    private void Awake()
    {
        b_ApplySwatch = GetComponent<Button>();
        image = GetComponent<Image>();
        m_name = GetComponentInChildren<TMP_Text>();

        b_ApplySwatch.onClick.AddListener(() => AssignSwatchToSelectable());
    }

    public void AssignMaterialToSwatch(Material m, int zone)
    {
        image.material = m;
        m_name.text = m.name;
        element = zone;
    }

    private void AssignSwatchToSelectable()
    {
        Selectable.SelectedSelectables[0].GetComponent<MaterialPalette>().Assign(image.material, element);
    }
}
