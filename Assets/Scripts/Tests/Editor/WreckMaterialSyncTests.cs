using System.Collections.Generic;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Wreck;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// A wreck must look exactly like its unit at the swap. Wreck materials are copies of the unit's
    /// Ship Lit materials, so they go stale when those are edited.
    /// Fix failures with Tools/Empire At War/Rendering/Materials/Sync Wreck Materials (or rebuild the wreck for mesh/scale failures).
    /// </summary>
    public sealed class WreckMaterialSyncTests
    {
        private const float TOLERANCE = 0.0001f;

        private static IEnumerable<string> WreckedViews() => ShipWreckBuilder.FindWreckedViewPaths();

        [Test]
        public void WreckedViews_Exist()
        {
            Assert.That(ShipWreckBuilder.FindWreckedViewPaths(), Is.Not.Empty);
        }

        [TestCaseSource(nameof(WreckedViews))]
        public void Wreck_MatchesViewScaleAndMeshes(string viewPath)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
            UnitWreckView wreck = AssetDatabase.LoadAssetAtPath<UnitWreckView>(ShipWreckBuilder.GetWreckPrefabPath(view));
            List<MeshRenderer> sources = ShipWreckBuilder.GetSourceRenderers(view);
            MeshRenderer[] copies = wreck.GetComponentsInChildren<MeshRenderer>(true);

            Assert.That(wreck.transform.localScale, Is.EqualTo(view.transform.localScale), "Root scale differs.");
            Assert.That(copies.Length, Is.EqualTo(sources.Count), "Renderer count differs.");
            for (int i = 0; i < sources.Count; i++)
            {
                Assert.That(copies[i].GetComponent<MeshFilter>().sharedMesh,
                    Is.SameAs(sources[i].GetComponent<MeshFilter>().sharedMesh), $"Mesh of {sources[i].name} differs.");
            }
        }

        [TestCaseSource(nameof(WreckedViews))]
        public void WreckMaterials_MatchSourceMaterials(string viewPath)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
            UnitWreckView wreck = AssetDatabase.LoadAssetAtPath<UnitWreckView>(ShipWreckBuilder.GetWreckPrefabPath(view));
            List<MeshRenderer> sources = ShipWreckBuilder.GetSourceRenderers(view);
            MeshRenderer[] copies = wreck.GetComponentsInChildren<MeshRenderer>(true);

            Assert.That(copies.Length, Is.EqualTo(sources.Count), "Renderer count differs; rebuild the wreck.");
            List<string> mismatches = new List<string>();
            for (int i = 0; i < sources.Count; i++)
            {
                Material[] sourceMaterials = sources[i].sharedMaterials;
                Material[] wreckMaterials = copies[i].sharedMaterials;
                Assert.That(wreckMaterials.Length, Is.EqualTo(sourceMaterials.Length),
                    $"Material count of {sources[i].name} differs; rebuild the wreck.");
                for (int slot = 0; slot < sourceMaterials.Length; slot++)
                {
                    CollectMismatches(sourceMaterials[slot], wreckMaterials[slot], mismatches);
                }
            }

            Assert.That(mismatches, Is.Empty,
                "Wreck materials are out of sync; run Tools/Empire At War/Rendering/Materials/Sync Wreck Materials.\n" +
                string.Join("\n", mismatches));
        }

        // Compares every property of the source's Ship Lit shader; the wreck shader declares the same names.
        private static void CollectMismatches(Material source, Material wreck, List<string> mismatches)
        {
            Shader shader = source.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string name = shader.GetPropertyName(i);
                if (!wreck.HasProperty(name)) continue;

                bool isEqual = shader.GetPropertyType(i) switch
                {
                    ShaderPropertyType.Texture => source.GetTexture(name) == wreck.GetTexture(name) &&
                                                  source.GetTextureScale(name) == wreck.GetTextureScale(name) &&
                                                  source.GetTextureOffset(name) == wreck.GetTextureOffset(name),
                    ShaderPropertyType.Color or ShaderPropertyType.Vector =>
                        (source.GetVector(name) - wreck.GetVector(name)).sqrMagnitude < TOLERANCE,
                    ShaderPropertyType.Int => source.GetInteger(name) == wreck.GetInteger(name),
                    _ => Mathf.Abs(source.GetFloat(name) - wreck.GetFloat(name)) < TOLERANCE,
                };

                if (!isEqual)
                {
                    mismatches.Add($"{source.name} -> {wreck.name}: {name}");
                }
            }
        }
    }
}
