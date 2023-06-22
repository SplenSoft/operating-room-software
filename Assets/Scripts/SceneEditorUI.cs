using UnityEngine;

public class SceneEditorUI : MonoBehaviour
{
    public static GizmoMode CurrentGizmoMode { get; private set; }

    public void SetGizmoMode(GizmoMode gizmoMode)
    {
        CurrentGizmoMode = gizmoMode;
    }
}