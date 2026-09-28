using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    public static class AutodeskUnitMaterialConversion
    {
        public static int Convert(IEnumerable<Material> materials, Shader shipLit,
            IEnumerable<string> unitPrefabPaths, StringBuilder report)
        {
            Material[] candidates = materials.Where(m => m.shader.name == AutodeskMaterialConverter.SOURCE_SHADER_NAME).ToArray();
            if (candidates.Length == 0) return 0;

            HashSet<string> unitPaths = new HashSet<string>(unitPrefabPaths);
            HashSet<string> candidatePaths = new HashSet<string>(candidates.Select(AssetDatabase.GetAssetPath));
            HashSet<string> sharedPaths = new HashSet<string>();
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/") || unitPaths.Contains(path) ||
                    !(path.EndsWith(".prefab") || path.EndsWith(".unity") || path.EndsWith(".asset"))) continue;
                foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                    if (candidatePaths.Contains(dependency)) sharedPaths.Add(dependency);
            }

            Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
            int converted = 0;
            foreach (Material source in candidates)
            {
                string path = AssetDatabase.GetAssetPath(source);
                Material target = source;
                if (sharedPaths.Contains(path))
                {
                    string copyPath = Path.Combine(Path.GetDirectoryName(path),
                        Path.GetFileNameWithoutExtension(path) + "_ShipLit.mat").Replace('\\', '/');
                    target = AssetDatabase.LoadAssetAtPath<Material>(copyPath);
                    if (target == null)
                    {
                        if (!AssetDatabase.CopyAsset(path, copyPath))
                            throw new InvalidOperationException($"Could not copy shared material: {path}");
                        target = AssetDatabase.LoadAssetAtPath<Material>(copyPath);
                    }
                    replacements.Add(source, target);
                    report.AppendLine($"  copied/reused (shared outside units): {copyPath}");
                }

                if (target.shader == shipLit) continue;
                AutodeskMaterialConverter.Convert(target, shipLit);
                converted++;
                report.AppendLine($"  converted Autodesk: {AssetDatabase.GetAssetPath(target)}");
            }

            if (replacements.Count > 0)
                RepointUnitPrefabs(unitPaths, replacements, report);
            report.AppendLine($"  {replacements.Count} shared materials use unit-only copies");
            AssetDatabase.SaveAssets();
            return converted;
        }

        private static void RepointUnitPrefabs(IEnumerable<string> paths,
            Dictionary<Material, Material> replacements, StringBuilder report)
        {
            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab.GetComponentsInChildren<MeshRenderer>(true)
                    .SelectMany(r => r.sharedMaterials).Any(m => m != null && replacements.ContainsKey(m))) continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Material[] slots = renderer.sharedMaterials;
                        bool changed = false;
                        for (int i = 0; i < slots.Length; i++)
                        {
                            if (slots[i] == null || !replacements.TryGetValue(slots[i], out Material replacement)) continue;
                            slots[i] = replacement;
                            changed = true;
                        }
                        if (changed) renderer.sharedMaterials = slots;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    report.AppendLine($"  repointed unit prefab: {path}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }
    }
}
