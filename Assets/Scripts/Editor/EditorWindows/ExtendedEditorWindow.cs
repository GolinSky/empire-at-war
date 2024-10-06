using UnityEditor;

namespace EmpireAtWar.Editor.EditorWindows
{
    public class ExtendedEditorWindow: EditorWindow
    {
        protected SerializedObject _serializedObject;
        protected SerializedProperty _serializedProperty;

        protected void DrawProperties(SerializedProperty serializedProperty, bool drawChildren)
        {
            string lastPropertyPath = string.Empty;
            foreach (SerializedProperty property in serializedProperty)
            {
                if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
                {
                    EditorGUILayout.BeginHorizontal();
                    property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName);
                    EditorGUILayout.EndHorizontal();

                    if (property.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawProperties(property, drawChildren);
                        EditorGUI.indentLevel--;
                    }
                   
                }
                else
                {
                    if (!string.IsNullOrEmpty(lastPropertyPath) 
                        && property.propertyPath.Contains(lastPropertyPath))
                    {
                        continue;
                    }
                
                    lastPropertyPath = property.propertyPath;
                
                    EditorGUILayout.PropertyField(property, drawChildren);
                }
            }
        }
    }
}