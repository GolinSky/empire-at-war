using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityToolbarExtender
{
    [InitializeOnLoad]
    public static class ToolbarTimeScale
    {
        private const string ELEMENT_PATH = "Empire At War/Time Scale";

        private static readonly float[] _timescales = { 0f, 0.05f, 0.1f, 0.25f, 0.5f, 1f };

        private static float _lastTimeScale;

        static ToolbarTimeScale()
        {
            _lastTimeScale = Time.timeScale;
            EditorApplication.update += RefreshWhenTimeScaleChanges;
        }

        [MainToolbarElement(ELEMENT_PATH, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static MainToolbarElement CreateTimeScaleDropdown()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent(ToString(Time.timeScale), "Select simulation time scale."),
                ShowTimeScaleMenu);
        }

        private static void ShowTimeScaleMenu(Rect dropdownRect)
        {
            var menu = new GenericMenu();
            foreach (var timeScale in _timescales)
            {
                menu.AddItem(new GUIContent(ToString(timeScale)), Mathf.Approximately(timeScale, Time.timeScale), SetTimeScale, timeScale);
            }

            menu.DropDown(dropdownRect);
        }

        private static void SetTimeScale(object value)
        {
            Time.timeScale = (float)value;
            RefreshTimeScaleDropdown();
        }

        private static void RefreshWhenTimeScaleChanges()
        {
            if (Mathf.Approximately(_lastTimeScale, Time.timeScale))
            {
                return;
            }

            RefreshTimeScaleDropdown();
        }

        private static void RefreshTimeScaleDropdown()
        {
            _lastTimeScale = Time.timeScale;
            MainToolbar.Refresh(ELEMENT_PATH);
        }

        private static string ToString(float timeScale)
        {
            return (int)(timeScale * 100) + "%";
        }
    }
}
