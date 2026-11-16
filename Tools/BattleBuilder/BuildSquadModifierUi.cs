using System;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.MainMenu.Settings;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class BuildSquadModifierUi
{
    private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab";

    public static string Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        try
        {
            SettingsUi view = root.GetComponent<SettingsUi>();
            SerializedObject fields = new SerializedObject(view);
            SerializedProperty target = fields.FindProperty("squadModifierRow");
            if (target.objectReferenceValue == null)
            {
                SettingsDropdownRow source = (SettingsDropdownRow)fields.FindProperty("qualityRow").objectReferenceValue;
                SettingsToggleRow cameraRow = (SettingsToggleRow)fields.FindProperty("invertZoomRow").objectReferenceValue;
                SettingsDropdownRow row = UnityEngine.Object.Instantiate(source, cameraRow.transform.parent);
                row.name = "SquadModifierRow";
                RectTransform rect = (RectTransform)row.transform;
                RectTransform previous = (RectTransform)cameraRow.transform;
                rect.anchoredPosition = previous.anchoredPosition + new Vector2(0, -previous.rect.height - 12);
                row.transform.SetSiblingIndex(cameraRow.transform.GetSiblingIndex() + 1);
                SerializedObject rowFields = new SerializedObject(row);
                TMP_Dropdown dropdown = (TMP_Dropdown)rowFields.FindProperty("dropdown").objectReferenceValue;
                foreach (TMP_Text label in row.GetComponentsInChildren<TMP_Text>(true))
                    if (label.text == "Overall quality") label.text = "Squad modifier (Mac)";
                foreach (TooltipTrigger trigger in row.GetComponentsInChildren<TooltipTrigger>(true))
                    UnityEngine.Object.DestroyImmediate(trigger);
                dropdown.ClearOptions();
                dropdown.AddOptions(new System.Collections.Generic.List<string> { "Control", "Command" });
                dropdown.SetValueWithoutNotify(0);
                target.objectReferenceValue = row;
                fields.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        return "SettingsUi squad modifier dropdown authored and saved.";
    }
}
