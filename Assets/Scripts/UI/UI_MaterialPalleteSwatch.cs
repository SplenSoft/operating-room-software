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

    private void Awake()
    {
        b_ApplySwatch = GetComponent<Button>();
        image = GetComponent<Image>();
        m_name = GetComponentInChildren<TMP_Text>();

        b_ApplySwatch.onClick.AddListener(() => AssignSwatchToSelectable());
    }

    public void AssignMaterialToSwatch(Material m)
    {
        image.material = m;
        m_name.text = m.name;
    }

    private void AssignSwatchToSelectable()
    {
        Selectable.SelectedSelectable.GetComponent<MaterialPalette>().Assign(image.material);
    }
}
