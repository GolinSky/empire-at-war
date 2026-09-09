using EmpireAtWar.Ship;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.EditorWindows.ShipModelEditor
{
    [CustomEditor(typeof(ShipComponentsData))]
    public class ShipModelCustomEditor: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Open Editor"))
            {
                ShipModelEditorWindow.Open((ShipComponentsData)target);
            }
        }
    }
}