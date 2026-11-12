using System.Collections.Generic;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class HardPointDestructionVfxPrefabTests
    {
        [Test]
        public void ModelPrefabs_EnabledDestructionEffectsHaveDedicatedAnchors()
        {
            int checkedHardPoints = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                HashSet<Transform> anchors = new HashSet<Transform>();
                foreach (HardPoint hardPoint in prefab.GetComponentsInChildren<HardPoint>(true))
                {
                    SerializedObject serialized = new SerializedObject(hardPoint);
                    if (!serialized.FindProperty("spawnDestroyedExplosion").boolValue) continue;
                    Transform anchor = (Transform)serialized.FindProperty("destroyedVfxAnchor").objectReferenceValue;
                    string context = path + "/" + hardPoint.name;
                    Assert.That(anchor, Is.Not.Null, context);
                    Assert.That(anchor, Is.Not.SameAs(hardPoint.transform), context);
                    Assert.That(anchor.root, Is.SameAs(hardPoint.transform.root), context);
                    Assert.That(anchors.Add(anchor), Is.True, context);
                    Assert.That(serialized.FindProperty("destroyedVfxScale").floatValue / prefab.transform.lossyScale.x,
                        Is.InRange(1f, 8f), context);
                    checkedHardPoints++;
                }
            }

            Assert.That(checkedHardPoints, Is.GreaterThan(1000));
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/View/RaiderCorvetteShipView.prefab", 1f, 1f)]
        [TestCase("Assets/Prefabs/Models/Ships/Empire/View/ISDIIShipView.prefab", 2f, 3f)]
        [TestCase("Assets/Prefabs/Models/Ships/Rebellion/View/HomeOneShipView.prefab", 3f, 4f)]
        [TestCase("Assets/Prefabs/Models/Ships/Empire/View/ExecutorShipView.prefab", 8f, 8f)]
        public void ShipPrefabs_DestructionEffectSizeMatchesUnit(string path, float minimum, float maximum)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (HardPoint hardPoint in prefab.GetComponentsInChildren<HardPoint>(true))
            {
                SerializedObject serialized = new SerializedObject(hardPoint);
                Assert.That(serialized.FindProperty("destroyedVfxScale").floatValue / prefab.transform.lossyScale.x,
                    Is.InRange(minimum, maximum), hardPoint.name);
            }
        }
    }
}
