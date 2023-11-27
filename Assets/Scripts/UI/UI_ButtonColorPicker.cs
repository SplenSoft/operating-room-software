using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_ButtonColorPicker : MonoBehaviour
{
    public UnityEvent<Color> ColorPickerEvent;
    [SerializeField] Texture2D colorChart;
    [SerializeField] GameObject chart;

    [SerializeField] RectTransform cursor;
    [SerializeField] Image button;
    [SerializeField] Image cursorColor;

    public void PickColor(BaseEventData data)
    {
        PointerEventData pointer = data as PointerEventData;
        cursor.position = pointer.position;
        Color pickedColor = colorChart.GetPixel(
            (int)(cursor.anchoredPosition.x * (colorChart.width / chart.GetComponent<RectTransform>().rect.width)), 
            (int)(cursor.anchoredPosition.y * (colorChart.height / chart.GetComponent<RectTransform>().rect.height)));
        button.color = pickedColor;
        cursorColor.color = pickedColor;
        ColorPickerEvent.Invoke(pickedColor);
    }
}
