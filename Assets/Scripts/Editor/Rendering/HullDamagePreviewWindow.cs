using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Services.Player;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Shows hull damage on the selected unit outside Play Mode: uploads the shared look from
    /// <see cref="HullDamageSettings"/> and sets the selected unit's damage level. Nothing is saved to the unit.
    /// </summary>
    public sealed class HullDamagePreviewWindow : EditorWindow
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Players/HullDamageSettings.asset";

        private HullDamageSettings _settings;
        private float _hull = 1f;

        [MenuItem("Tools/Empire At War/Hull Damage Preview")]
        private static void Open()
        {
            GetWindow<HullDamagePreviewWindow>("Hull Damage");
        }

        private void OnEnable()
        {
            _settings = AssetDatabase.LoadAssetAtPath<HullDamageSettings>(SETTINGS_PATH);
        }

        private void OnSelectionChange() => Repaint();

        private void OnGUI()
        {
            _settings = (HullDamageSettings)EditorGUILayout.ObjectField("Settings", _settings,
                typeof(HullDamageSettings), false);
            UnitRenderStateComponent unit = Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponentInParent<UnitRenderStateComponent>();
            if (_settings == null || unit == null)
            {
                EditorGUILayout.HelpBox("Select a unit with a UnitRenderStateComponent (scene or prefab stage).",
                    MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _hull = EditorGUILayout.Slider("Hull", _hull, 0f, 1f);
            bool apply = GUILayout.Button("Apply (also after editing settings)");
            if (EditorGUI.EndChangeCheck() || apply)
            {
                HullDamageService.Upload(_settings);
                unit.SetHullDamage(_hull);
                SceneView.RepaintAll();
            }
        }
    }
}
