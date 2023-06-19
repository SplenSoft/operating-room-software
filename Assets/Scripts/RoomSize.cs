using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Singleton class
/// </summary>
public class RoomSize : MonoBehaviour
{
    public static EventHandler RoomSizeChanged;
    private static RoomSize Instance { get; set; }
    private readonly float _minimumSizeInFeet = 6f;

    [field: SerializeField] private TMP_InputField InputFieldWidth { get; set; }
    [field: SerializeField] private TMP_InputField InputFieldHeight { get; set; }
    [field: SerializeField] private TMP_InputField InputFieldDepth { get; set; }

    private void Awake()
    {
        Instance = this;
        InputFieldWidth.onEndEdit.AddListener(text => EnforceDimensionSize(InputFieldWidth, text));
        InputFieldHeight.onEndEdit.AddListener(text => EnforceDimensionSize(InputFieldHeight, text));
        InputFieldDepth.onEndEdit.AddListener(text => EnforceDimensionSize(InputFieldDepth, text));
    }

    private void EnforceDimensionSize(TMP_InputField inputField, string text)
    {
        if (IsDimensionTooSmall(text))
            inputField.SetTextWithoutNotify(_minimumSizeInFeet.ToString());
    }

    private bool IsDimensionTooSmall(string text)
    {
        if (float.TryParse(text, out float result))
        {
            if (result < _minimumSizeInFeet)
            {
                Debug.LogError($"Room dimensions must be a minimum of {_minimumSizeInFeet} feet");
                return true;
            }
        }
        return false;
    }

    public void OnButtonConfirm()
    {
        RoomSizeChanged?.Invoke(this, null);
        gameObject.SetActive(false);
    }

    /// <returns>Dimension in meters</returns>
    public static float GetDimension(RoomDimension dimension)
    {
        return dimension switch
        {
            RoomDimension.Width => float.Parse(Instance.InputFieldWidth.text),
            RoomDimension.Height => float.Parse(Instance.InputFieldHeight.text),
            RoomDimension.Depth => float.Parse(Instance.InputFieldDepth.text),
            _ => throw new ArgumentException($"Unhandled argument {dimension}"),
        };
    }
}

public enum RoomDimension
{
    Width,
    Height,
    Depth
}