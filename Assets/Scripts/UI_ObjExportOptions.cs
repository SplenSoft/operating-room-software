using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UI_ObjExportOptions : MonoBehaviour
{
    private static UI_ObjExportOptions Instance { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeFloor { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeFloorObjects { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeCeiling { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeCeilingObjects { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeWalls { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeWallObjects { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeArmAssemblies { get; set; }

    [field: SerializeField]
    private Toggle ToggleIncludeAssemblyHeads { get; set; }

    [field: SerializeField]
    private Button ButtonExportSelectedObject { get; set; }

    public static void Open()
    {
        Instance.gameObject.SetActive(true);
    }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ButtonExportSelectedObject.gameObject.SetActive(Selectable.SelectedSelectables.Count > 0);
    }

    private ObjExportOptions GetOptions()
    {
        return new()
        {
            IncludeFloor = ToggleIncludeFloor.isOn,
            IncludeFloorObjects = ToggleIncludeFloorObjects.isOn,
            IncludeCeiling = ToggleIncludeCeiling.isOn,
            IncludeCeilingObjects = ToggleIncludeCeilingObjects.isOn,
            IncludeWalls = ToggleIncludeWalls.isOn,
            IncludeWallObjects = ToggleIncludeWallObjects.isOn,
            IncludeArmBoomAssemblies = ToggleIncludeArmAssemblies.isOn,
            IncludeArmBoomHeads = ToggleIncludeAssemblyHeads.isOn,
        };
    }

    public void ExportAllObjects()
    {
        ObjExporter.DoExport(
            true, 
            Selectable.ActiveSelectables,
            GetOptions());

        gameObject.SetActive(false);
    }

    public void ExportSelectedObject()
    {
        if (Selectable.SelectedSelectables.Count == 0)
            return;

        if (Selectable.SelectedSelectables[0].TryGetArmAssemblyRoot(out GameObject obj))
        {
            ObjExporter.DoExport(true, obj, GetOptions());
        }
        else
        {

            ObjExporter.DoExport(true, Selectable.SelectedSelectables[0].gameObject, GetOptions());
        }

        gameObject.SetActive(false);
    }
}