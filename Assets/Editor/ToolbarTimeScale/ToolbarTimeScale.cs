using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender
{
    [InitializeOnLoad]
    public static class ToolbarTimeScale
    {
        private static float[] m_Timescales = new[] { 0f, 0.05f, 0.1f, 0.25f, 0.5f, 1f };
        private static string m_Style = "Dropdown";

        static ToolbarTimeScale()
        {
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
        }

        private static void OnToolbarGUI()
        {
            DropdownButton(
                Array.IndexOf<float>(m_Timescales, Time.timeScale),
                m_Timescales
            );
        }

        private static void DropdownButton(int index, float[] values)
        {
            var label = new GUIContent(ToString(Time.timeScale));
            var style = new GUIStyle(m_Style);
            var width = GUILayout.Width(56);
            
            var button = EditorGUILayout.DropdownButton(label, FocusType.Passive, style, width);
            if (button)
            {
                var menu = new GenericMenu();

                for (var i = 0; i < values.Length; i++)
                    menu.AddItem(new GUIContent(ToString(values[i])), i == index, OnSelect, values[i]);

                menu.DropDown(GUILayoutUtility.GetLastRect());
            }
        }

        private static void OnSelect(object i)
        {
            Time.timeScale = (float)i;
        }

        private static string ToString(float timeScale)
        {
            return (int)(timeScale * 100) + "%";
        }
    }
}