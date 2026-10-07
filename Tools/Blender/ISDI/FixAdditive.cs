using System.Linq;
using UnityEditor;
using UnityEngine;
public static class FixImperialAdditive
{
    public static string Main()
    {
        AssetDatabase.Refresh();int count=0;
        foreach(string folder in new[]{"ISDI","TIEInterceptor","TIEBrute","TIEPunisher"})
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Materials/Models/EmpireShips/"+folder}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(!material.name.EndsWith("MeshAdditive"))continue;
            string path=AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")).Replace(".png","_Additive.png");
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));material.SetFloat("_SrcBlendAlpha",1);material.SetFloat("_DstBlendAlpha",10);EditorUtility.SetDirty(material);count++;
        }
        AssetDatabase.SaveAssets();return "Fixed additive transparency on "+count+" source effects without changing the lossless source textures.";
    }
}
