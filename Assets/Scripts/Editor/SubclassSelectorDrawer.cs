using System;
using EmpireAtWar.Utils;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public sealed class SubclassSelectorDrawer : PropertyDrawer
    {
        private const string NONE_LABEL = "None";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect dropdownRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(GetTypeName(property)), FocusType.Keyboard))
            {
                ShowTypeMenu(property);
            }

            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }

        private void ShowTypeMenu(SerializedProperty property)
        {
            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;
            Type currentType = property.managedReferenceValue?.GetType();

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent(NONE_LABEL), currentType == null,
                () => Assign(serializedObject, propertyPath, null));
            foreach (Type type in TypeCache.GetTypesDerivedFrom(fieldInfo.FieldType))
            {
                if (type.IsAbstract || type.IsGenericType)
                {
                    continue;
                }

                Type selectedType = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), type == currentType,
                    () => Assign(serializedObject, propertyPath, Activator.CreateInstance(selectedType)));
            }

            menu.ShowAsContext();
        }

        private static void Assign(SerializedObject serializedObject, string propertyPath, object value)
        {
            serializedObject.Update();
            serializedObject.FindProperty(propertyPath).managedReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static string GetTypeName(SerializedProperty property)
        {
            string fullTypeName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullTypeName))
            {
                return NONE_LABEL;
            }

            string typeName = fullTypeName.Substring(fullTypeName.LastIndexOf('.') + 1);
            return ObjectNames.NicifyVariableName(typeName);
        }
    }
}
