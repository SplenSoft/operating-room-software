using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton class
/// </summary>
public class InputHandler : MonoBehaviour
{
    private static InputHandler Instance { get; set; }
    public static Vector2 MouseDeltaPixels { get; private set; }
    public static Vector2 MouseDeltaScreenPercentage { get; private set; }
    public static EventHandler<KeyStateChangedEventArgs> KeyStateChanged;

    private int[] values;
    private KeyState[] keys;
    private float _timeClickHeldDown;
    private bool _isClicking;
    private Vector2 _mousePosLastFrame;

    private void Awake()
    {
        Instance = this;
        values = (int[])System.Enum.GetValues(typeof(KeyCode));
        keys = new KeyState[values.Length];
    }

    private void Update()
    {
        for (int i = 0, n = values.Length; i < n; i++)
        {
            var keyCode = (KeyCode)values[i];

            var newValue = Input.GetKeyDown(keyCode) ? KeyState.PressedThisFrame : 
                Input.GetKeyUp(keyCode) ? KeyState.ReleasedThisFrame : 
                Input.GetKey(keyCode) ? KeyState.HeldThisFrame : 
                KeyState.None;

            bool valueChanged = newValue != keys[i];
            keys[i] = newValue;
            if (valueChanged)
            {
                KeyStateChanged?.Invoke(this, new KeyStateChangedEventArgs(keyCode, newValue));
            }
            
        }

        if (Input.GetMouseButtonDown(0))
        {
            _isClicking = true;
            _timeClickHeldDown = 0f;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _isClicking = false;
        }
        else if (Input.GetMouseButton(0))
        {
            _timeClickHeldDown += Time.deltaTime;
        }

        UpdateMouseDelta();
    }

    private void UpdateMouseDelta()
    {
        MouseDeltaPixels = (Vector2)Input.mousePosition - _mousePosLastFrame;
        _mousePosLastFrame = Input.mousePosition;
        MouseDeltaScreenPercentage =  new Vector2(MouseDeltaPixels.x / Screen.width, MouseDeltaPixels.y / Screen.height);
    }
}

public enum KeyState
{
    None,
    PressedThisFrame,
    HeldThisFrame,
    ReleasedThisFrame
}

public class KeyStateChangedEventArgs : EventArgs
{
    public KeyStateChangedEventArgs(KeyCode keyCode, KeyState state)
    {
        KeyCode = keyCode;
        KeyState = state;
    }
    public KeyCode KeyCode { get; }
    public KeyState KeyState { get; }
}
