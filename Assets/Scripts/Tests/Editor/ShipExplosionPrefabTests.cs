using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class ShipExplosionPrefabTests
    {
        private static IEnumerable<string> ShipPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Models/Ships" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<ShipEntity>() != null)
                    yield return path;
            }
        }

        [TestCaseSource(nameof(ShipPrefabs))]
        public void ExplosionHullRenderers_AreAssignedAndBoundsAreReadable(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                using (var serialized = new SerializedObject(root.GetComponent<ShipEntity>()))
                {
                    var renderers = serialized.FindProperty("explosionHullRenderers");
                    Assert.That(renderers.arraySize, Is.GreaterThan(0), path);
                    for (int index = 0; index < renderers.arraySize; index++)
                    {
                        var renderer = renderers.GetArrayElementAtIndex(index).objectReferenceValue as Renderer;
                        Assert.That(renderer != null, Is.True, $"{path}: explosionHullRenderers[{index}] is missing.");
                        Assert.That(renderer.transform.IsChildOf(root.transform), Is.True, path);
                        Assert.DoesNotThrow(() => { var bounds = renderer.bounds; }, $"{path}: renderer {index}");
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
