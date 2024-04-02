using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Makes this <see cref="GameObject"/> a scene root object 
/// on <see cref="Start"/> (removes all parents) 
/// </summary>
public class MoveToRootOnStart : MonoBehaviour
{
    /// <summary>
    /// Fires when object becomes a root object
    /// </summary>
    public UnityEvent OnMoved { get; } = new();

    public bool Moved { get; private set; }

    private void Start()
    {
        transform.parent = null;
        Moved = true;
        OnMoved?.Invoke();
    }
}