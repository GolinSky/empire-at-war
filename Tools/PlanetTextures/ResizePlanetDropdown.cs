using EmpireAtWar.Entities.MainMenu.Skirmish;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ResizePlanetDropdown
{
    private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
    private const float TEMPLATE_HEIGHT = 496f;

    public static string Main()
    {
        var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        try
        {
            using (var view = new SerializedObject(root.GetComponent<SkirmishUi>()))
            {
                var dropdown = (TMP_Dropdown)view.FindProperty("planetsDropdown").objectReferenceValue;
                RectTransform template = dropdown.template;
                float previousHeight = template.rect.height;
                // Ten 48-unit rows plus the existing 16-unit vertical padding.
                template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, TEMPLATE_HEIGHT);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                AssetDatabase.SaveAssets();
                return $"Planet dropdown template height: {previousHeight} -> {template.rect.height}; width and item sizes preserved.";
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
