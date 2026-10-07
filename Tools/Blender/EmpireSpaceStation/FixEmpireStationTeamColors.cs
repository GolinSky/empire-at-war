using System;
using UnityEditor;
using UnityEngine;

public static class FixEmpireStationTeamColors
{
    private const string MATERIAL = "Assets/Art/Materials/Models/SpaceStations/EmpireSpaceStation/EmpireSpaceStation_EmpireStationSlot01.mat";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station material changes require Edit Mode.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL);
        material.SetFloat("_TeamRimStrength", 0);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return "Saved Imperial station material with team rim disabled; authored stripe mask retained.";
    }
}
