using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button))]
public class UI_ButtonTextController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Text Color Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;

    [Header("Text Size Settings")]
    public float normalTextSize = 14f;
    public float hoverTextSize = 16f;

    [Header("Cursor Settings")]
    public Texture2D hoverCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    private TextMeshProUGUI buttonText;
    private bool isSelected = false;

    private void Awake()
    {
        // Get the Text component of the button
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText == null)
        {
            Debug.LogError("No Text component found on the button.");
            enabled = false;
            return;
        }

        // Set the initial text properties
        buttonText.color = normalColor;
        buttonText.fontSize = Mathf.RoundToInt(normalTextSize);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected)
        {
            buttonText.color = hoverColor;
            buttonText.fontSize = Mathf.RoundToInt(hoverTextSize);

            // Change the cursor if a hover cursor texture is provided
            if (hoverCursor != null)
            {
                Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
        {
            buttonText.color = normalColor;
            buttonText.fontSize = Mathf.RoundToInt(normalTextSize);

            // Reset the cursor to default
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = !isSelected;
        buttonText.color = isSelected ? selectedColor : normalColor;

        // Keep the font size at normal or hover size based on hover state
        buttonText.fontSize = isSelected ? Mathf.RoundToInt(hoverTextSize) : Mathf.RoundToInt(normalTextSize);
    }


    private void OnDisable()
    {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

    }
}
