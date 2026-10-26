using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ImportWornMasks
{
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Import masks in Edit Mode.");
        string[] paths = {
            "Assets/Art/Textures/Models/EmpireShips/ISDI/ISDI_TeamStripes_TeamMask.png",
            "Assets/Art/Textures/Models/EmpireShips/ISDIIReplacement/ISDII_WornTeamStripeMask.png",
            "Assets/Art/Textures/Models/EmpireShips/VictoryI/VictoryI_EV_Victory_SD_TeamMask.png",
            "Assets/Art/Textures/Models/EmpireShips/VictoryII/VictoryII_EV_Victory_SD_TeamMask.png",
            "Assets/Art/Textures/Models/EmpireShips/Victory/Victory_Hull_TeamMask.png",
            "Assets/Art/Textures/Models/RepublicShips/Imperator/Imperator_Hull_TeamMask.png",
            "Assets/Art/Textures/Models/EmpireShips/AcclamatorAssault/AcclamatorAssault_HullStripeMask.png"
        };
        AssetDatabase.Refresh();
        foreach (string path in paths)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[1]);
        var hull = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/ISDIIReplacement/ISDIIReplacement_ISDII_Hull_Material_HighContrast_MeshBumpColorize.mat");
        var materials = new[] {
            "Assets/Art/Materials/Models/EmpireShips/ISDIIReplacement/ISDII_TeamStripes.mat",
            "Assets/Art/Materials/Wrecks/Empire/ISDII/ISDII_TeamStripes_Wreck.mat"
        };
        foreach (string path in materials)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) throw new InvalidOperationException("Missing stripe material: " + path);
            material.SetTexture("_BaseMap", hull.GetTexture("_BaseMap"));
            material.SetTexture("_BumpMap", hull.GetTexture("_BumpMap"));
            material.SetColor("_BaseColor", hull.GetColor("_BaseColor"));
            material.SetTexture("_TeamMaskMap", mask);
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.ForceReserializeAssets(paths.Concat(materials));
        AssetDatabase.SaveAssets();
        return "Imported seven worn masks and saved ISD II live/wreck mask references.";
    }
}
