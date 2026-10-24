using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

// Re-applies the recorded pass-2 outcome to importers whose .meta write failed (file locked during the pass).
// Toggles through another value first so the importer is dirty and the .meta is rewritten.
public static class DesktopCompressionReconcile
{
    public static string Main()
    {
        JArray mismatches = JArray.Parse(File.ReadAllText("output/DesktopAssetCompression/Pass2/reconcile.json"));
        foreach (JToken item in mismatches)
        {
            string path = (string)item["path"];
            int want = (int)item["want"];
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer is ModelImporter model)
            {
                model.meshCompression = (ModelImporterMeshCompression)(want == 0 ? 1 : 0);
                model.SaveAndReimport();
                model.meshCompression = (ModelImporterMeshCompression)want;
                model.SaveAndReimport();
            }
            else
            {
                TextureImporter texture = (TextureImporter)importer;
                texture.textureCompression = want == 0 ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
                texture.SaveAndReimport();
                texture.textureCompression = want == 0 ? TextureImporterCompression.Uncompressed
                    : want == 1 ? TextureImporterCompression.Compressed : TextureImporterCompression.CompressedHQ;
                texture.SaveAndReimport();
            }
        }
        AssetDatabase.SaveAssets();
        return JsonConvert.SerializeObject(new { reapplied = mismatches.Count });
    }
}
