using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(ObjectPreview))]
public class ObjectPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Preview"))
        {
            ((ObjectPreview)target).UpdateTexture = true;
        }
    }
}
