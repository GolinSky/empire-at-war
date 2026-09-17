using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    public static class BattleInstancingAudit
    {
        private const string MENU_PATH = "Tools/Performance/Audit Battle Instancing";
        private const string SHIP_FOLDER = "Assets/Prefabs/Models/Ships";
        private const string PROJECTILE_FOLDER = "Assets/Prefabs/Vfx";
        private const string REPORT_PATH = "Library/BattleInstancingAudit.tsv";

        [MenuItem(MENU_PATH)]
        public static void Run()
        {
            var report = new StringBuilder();
            report.AppendLine("Prefab\tRenderer\tType\tMesh\tSubmesh\tMaterial\tShader\tMaterial instancing\tParticle mode\tSRP / instancing blocker\tRepeated renderer references\tInstanced draw evidence");
            var rows = new List<string>();
            var groups = new Dictionary<string, int>();
            AppendFolder(SHIP_FOLDER, null, rows, groups);
            AppendFolder(PROJECTILE_FOLDER, "Projectile", rows, groups);

            foreach (string row in rows)
            {
                string[] fields = row.Split('\t');
                string key = fields[3] + "\t" + fields[4] + "\t" + fields[5];
                string drawEvidence = fields[2] == "MeshRenderer"
                    ? "Not captured; verify Draw Mesh (Instanced) in Frame Debugger"
                    : "Not applicable to repeated Mesh Renderer instancing";
                report.Append(row).Append('\t').Append(groups[key]).Append('\t').AppendLine(drawEvidence);
            }

            string path = Path.GetFullPath(REPORT_PATH);
            File.WriteAllText(path, report.ToString());
            Debug.Log($"Battle instancing audit: {rows.Count} renderer/material rows written to {path}. Repetition is prefab-reference evidence only; no instanced draws or speedup are claimed.");
        }

        private static void AppendFolder(string folder, string nameFilter, List<string> rows, Dictionary<string, int> groups)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (nameFilter != null && !Path.GetFileNameWithoutExtension(path).Contains(nameFilter))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (Path.GetFileNameWithoutExtension(path) == "LaserProjectile")
                {
                    foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        SerializedProperty laserMaterial = new SerializedObject(component).FindProperty("laserMaterial");
                        if (laserMaterial == null)
                        {
                            continue;
                        }

                        Material material = laserMaterial.objectReferenceValue as Material;
                        string identity = GetAssetIdentity(material);
                        string key = "None\t0\t" + identity;
                        groups.TryGetValue(key, out int count);
                        groups[key] = count + 1;
                        rows.Add(string.Join("\t", path, component.name, "Runtime LineRenderer", "None", "0", identity,
                            material == null || material.shader == null ? "None" : material.shader.name,
                            material != null && material.enableInstancing ? "Enabled" : "Disabled", "N/A",
                            "LineRenderer is created at runtime; material-checkbox mesh instancing is inapplicable"));
                    }
                }

                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh = null;
                    string particleMode = "N/A";
                    string blocker = "SRP compatibility and actual draw path require Frame Debugger inspection";
                    if (renderer is MeshRenderer)
                    {
                        MeshFilter filter = renderer.GetComponent<MeshFilter>();
                        mesh = filter != null ? filter.sharedMesh : null;
                        if (mesh == null)
                        {
                            blocker = "Missing shared mesh";
                        }
                    }
                    else if (renderer is SkinnedMeshRenderer skinned)
                    {
                        mesh = skinned.sharedMesh;
                        blocker = "Skinned Mesh Renderer is not a material-checkbox instancing candidate";
                    }
                    else if (renderer is ParticleSystemRenderer particle)
                    {
                        particleMode = particle.renderMode.ToString();
                        mesh = particle.renderMode == ParticleSystemRenderMode.Mesh ? particle.mesh : null;
                        blocker = particle.renderMode == ParticleSystemRenderMode.Mesh
                            ? "Mesh particle shader/variant and actual draws require inspection"
                            : "Particle renderer is not in Mesh mode";
                    }
                    else
                    {
                        blocker = "Renderer type is not a repeated Mesh Renderer candidate";
                    }

                    Material[] materials = renderer.sharedMaterials;
                    for (int submesh = 0; submesh < materials.Length; submesh++)
                    {
                        Material material = materials[submesh];
                        string meshIdentity = GetAssetIdentity(mesh);
                        string materialIdentity = GetAssetIdentity(material);
                        string key = meshIdentity + "\t" + submesh + "\t" + materialIdentity;
                        groups.TryGetValue(key, out int count);
                        groups[key] = count + 1;
                        rows.Add(string.Join("\t", path, renderer.name, renderer.GetType().Name,
                            meshIdentity, submesh.ToString(), materialIdentity,
                            material == null || material.shader == null ? "None" : material.shader.name,
                            material != null && material.enableInstancing ? "Enabled" : "Disabled",
                            particleMode, blocker));
                    }
                }
            }
        }

        private static string GetAssetIdentity(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "None";
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
            {
                return AssetDatabase.GetAssetPath(asset) + ":" + asset.name + " [" + guid + "/" + localId + "]";
            }

            return asset.name + " [not an asset]";
        }
    }
}
