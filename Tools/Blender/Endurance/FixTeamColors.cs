using UnityEditor;
using UnityEngine;

public static class FixEnduranceTeamColors
{
    public static string Main()
    {
        int count=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Materials/Models/RebellionShips/Endurance","Assets/Art/Materials/Wrecks/Endurance"}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(material.shader.name!="EmpireAtWar/Ship Lit" && material.shader.name!="EmpireAtWar/Ship Wreck")continue;
            material.SetFloat("_TeamMaskStrength",0);
            material.SetFloat("_TeamLiveryStrength",1);
            material.SetFloat("_TeamLiveryHue",0);
            material.SetFloat("_TeamLiveryHueRange",.06f);
            material.SetFloat("_TeamLiveryMinSaturation",.5f);
            EditorUtility.SetDirty(material);count++;
        }
        AssetDatabase.SaveAssets();
        return "Saved red-paint-only team colors on "+count+" Endurance hull and wreck materials.";
    }
}
