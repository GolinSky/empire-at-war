using System;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Ship;
using UnityEditor;

namespace EmpireAtWar.Editor.EditorWindows.ShipModelEditor
{
    public class ShipModelEditorWindow: ExtendedEditorWindow
    {
        public static void Open(ShipModel model)
        {
            ShipModelEditorWindow window = GetWindow<ShipModelEditorWindow>("Game Data Editor");
            window._serializedObject = new SerializedObject(model);
        }

        private void OnGUI()
        {
            SerializedProperty prop = _serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    // // Draw movePoints property manually.
                    // if (prop.name == "movePoints")
                    // {
                    //     if (movingPlatform.boolValue)
                    //     {
                    //         EditorGUI.indentLevel++;
                    //         DrawMovePointsElement(prop);
                    //         EditorGUI.indentLevel--;
                    //     }
                    // }
                    // Draw default property field.
                  //  else
                    {
                        EditorGUILayout.PropertyField(_serializedObject.FindProperty(prop.name), true);
                    }
                } while (prop.NextVisible(false));
            }
		
            _serializedObject.ApplyModifiedProperties();
            //
            //  _serializedProperty =_serializedObject.GetIterator();
            //  
            // // if(_serializedProperty == null) return;
            // DrawProperties(_serializedProperty, true);
        }
    }
}