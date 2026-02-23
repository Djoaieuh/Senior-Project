// Assets/Editor/MasterItemDatabaseEditor.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MasterItemDatabase))]
public class MasterItemDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Populate From Project", GUILayout.Height(30)))
        {
            // Invoke the context menu method via reflection
            var method = target.GetType().GetMethod(
                "AutoPopulate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            method?.Invoke(target, null);
        }
    }
}