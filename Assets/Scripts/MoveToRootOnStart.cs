using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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

    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "ObjectEditor")
            return;

        Selectable.ActiveSelectables.ForEach(x =>
        {
            if (x.RelatedSelectables.Contains(_selectable))
            {
                x.RelatedSelectables.Remove(_selectable);
            }
        });

        _selectable.RelatedSelectables = new() { _selectable };

        transform.parent = null;
        Moved = true;
        Debug.Log($"Set {gameObject.name} as root obj");
        OnMoved?.Invoke();
    }
}