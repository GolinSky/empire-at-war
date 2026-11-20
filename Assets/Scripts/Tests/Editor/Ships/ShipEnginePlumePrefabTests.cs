using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Editor.Ship;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipEnginePlumePrefabTests
    {
        private static readonly string[] ALLOWED_OVERRIDE_PREFIXES =
        {
            "m_LocalPosition", "m_LocalRotation", "m_LocalScale", "m_LocalEulerAnglesHint", "m_Name", "m_Layer",
            "m_Parameters.widthMultiplier"
        };

        private static IEnumerable<string> ShipViewPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Models/Ships" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ShipMoveComponent>() != null)
                    yield return path;
            }
        }

        [Test]
        public void SharedPlume_IsAnIdleTrail()
        {
            var plume = AssetDatabase.LoadAssetAtPath<GameObject>(ShipEnginePlumeSetup.PLUME_PREFAB_PATH)
                .GetComponent<TrailRenderer>();
            Assert.That(plume, Is.Not.Null);
            Assert.That(plume.emitting, Is.False, "Ships start emitting after hyperspace arrival.");
            Assert.That(plume.autodestruct, Is.False, "Pooled ships reuse their trails.");
        }

        [TestCaseSource(nameof(ShipViewPrefabs))]
        public void EnginePlumes_AreSharedInstancesListedForMovementAndFog(string path)
        {
            var plumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShipEnginePlumeSetup.PLUME_PREFAB_PATH);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                using var movement = new SerializedObject(root.GetComponent<ShipMoveComponent>());
                using var fog = new SerializedObject(root.GetComponent<FogVisibilityComponent>());
                SerializedProperty plumes = movement.FindProperty("enginePlumes");
                SerializedProperty fogRenderers = fog.FindProperty("renderers");
                var fogList = Enumerable.Range(0, fogRenderers.arraySize)
                    .Select(index => fogRenderers.GetArrayElementAtIndex(index).objectReferenceValue).ToList();
                Assert.That(plumes.arraySize, Is.GreaterThan(0), $"{path}: every ship needs at least one engine plume.");
                Assert.That(root.GetComponentsInChildren<TrailRenderer>(true).Length, Is.EqualTo(plumes.arraySize),
                    $"{path}: every trail must be a listed engine plume.");
                int instances = root.GetComponentsInChildren<TrailRenderer>(true)
                    .Count(line => PrefabUtility.GetCorrespondingObjectFromOriginalSource(line.gameObject) == plumePrefab);
                Assert.That(instances, Is.EqualTo(plumes.arraySize), $"{path}: every plume instance must be listed in enginePlumes.");

                for (int index = 0; index < plumes.arraySize; index++)
                {
                    var plume = plumes.GetArrayElementAtIndex(index).objectReferenceValue as TrailRenderer;
                    Assert.That(plume != null, Is.True, $"{path}: enginePlumes[{index}] is missing.");
                    Assert.That(PrefabUtility.GetCorrespondingObjectFromOriginalSource(plume.gameObject), Is.SameAs(plumePrefab),
                        $"{path}: enginePlumes[{index}] must be an instance of the shared plume.");
                    Assert.That(fogList, Does.Contain(plume), $"{path}: enginePlumes[{index}] must be hidden by fog.");
                    string[] foreign = PrefabUtility.GetPropertyModifications(plume.gameObject)
                        .Select(modification => modification.propertyPath)
                        .Where(propertyPath => !ALLOWED_OVERRIDE_PREFIXES.Any(propertyPath.StartsWith))
                        .ToArray();
                    Assert.That(foreign, Is.Empty, $"{path}: enginePlumes[{index}] overrides more than placement and width.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
