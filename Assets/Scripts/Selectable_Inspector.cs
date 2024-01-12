#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public partial class Selectable : MonoBehaviour
{
    [CustomEditor(typeof(Selectable))]
    public class Selectable_Inspector : Editor
    {
        private Selectable _component;

        void OnEnable()
        {
            _component = target as Selectable; 
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            if(string.IsNullOrEmpty(_component.GUID))
            {
                if(GUILayout.Button("Generate GUID"))
                {
                    _component.GUID = System.Guid.NewGuid().ToString();
                }
            }

            EditorGUI.EndChangeCheck();

            DrawDefaultInspector();
        }
    }
}
#endif