using Palmmedia.ReportGenerator.Core.Parser.Filtering;
using Parabox.CSG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cuttable : MonoBehaviour
{
    private static List<Cuttable> ActiveCuttables { get; } = new();

    private Mesh _originalMesh;
    private MeshFilter _filter;
    private Mesh _currentMesh;
    private GizmoHandler _gizmoHandler;
    private Selectable _selectable;

    private void Awake()
    {
        ActiveCuttables.Add(this);

        _filter = GetComponentInChildren<MeshFilter>();
        _gizmoHandler = GetComponent<GizmoHandler>();
        _originalMesh = _filter.sharedMesh;
        _selectable = GetComponent<Selectable>();

        if (_gizmoHandler != null)
        {
            _gizmoHandler.GizmoDragEnded.AddListener(Cut);
        }

        if (_selectable != null)
        {
            _selectable.OnPlaced.AddListener(Cut);
        }
    }

    private void OnDestroy()
    {
        ActiveCuttables.Remove(this);

        if (_gizmoHandler != null)
        {
            _gizmoHandler.GizmoDragEnded.RemoveListener(Cut);
        }

        if (_selectable != null)
        {
            _selectable.OnPlaced.RemoveListener(Cut);
        }
    }

    private void Start()
    {
        Cut();
    }

    public static void UpdateCuts()
    {
        ActiveCuttables.ForEach(x => x.Cut());
    }

    public void Cut()
    {
        // Set back to original mesh
        _filter.sharedMesh = _originalMesh;
        // Get all wallcutters in scene

        var wallCutters = Selectable.ActiveSelectables
            .SelectMany(x => x.GetComponentsInChildren<WallCutter>())
            .ToList();

        foreach ( var wallCutter in wallCutters )
        {
            var result = CSG.Subtract(gameObject, wallCutter.gameObject);
            _filter.sharedMesh = result.mesh;

            if (_currentMesh != null)
            {
                Destroy(_currentMesh);
            }

            _currentMesh = result.mesh;
        }
    }
}
