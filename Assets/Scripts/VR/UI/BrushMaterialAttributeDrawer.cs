using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BrushMaterialAttribute))]
class BrushMaterialAttributeeEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.serializedObject.targetObject is IBrushMaterialsProvider)
        {
            IBrushMaterialsProvider materialsProvider = (IBrushMaterialsProvider)property.serializedObject.targetObject;

            var availableTypes = materialsProvider.BrushMaterials;
            var currentType = materialsProvider.BrushMaterial;

            string[] names;
            int current = 0;

            if (availableTypes != null)
            {
                names = new string[availableTypes.Length];

                for (int i = 0; i < availableTypes.Length; i++)
                {
                    names[i] = availableTypes[i].Name;

                    if (availableTypes[i].Equals(currentType))
                    {
                        current = i;
                    }
                }
            }
            else
            {
                names = new string[0];
            }

            var result = EditorGUI.Popup(position, property.displayName, current, names);

            if (availableTypes.Length > 0 && result >= 0)
            {
                var selected = availableTypes[result];
                property.FindPropertyRelative(nameof(selected._name)).stringValue = selected.Name;
                property.FindPropertyRelative(nameof(selected._id)).intValue = selected.ID;
                property.FindPropertyRelative(nameof(selected._texture)).objectReferenceValue = selected.Texture;
            }
        }
        else
        {
            Debug.LogWarning("'" + property.displayName + "' property owner does not implement " + nameof(IBrushMaterialsProvider));
        }
    }
}