using System.Collections.Generic;
using EmpireAtWar.Editor;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Repository.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class AssetMappingContextMenuTests
    {
        private const string RESOLUTE_GUID = "d9d6eb346b0497649911724f20048daf";
        private const string REMOVED_GUID = "00000000000000000000000000000001";

        [Test]
        public void BuildMappings_KeepsAliasesWhoseTargetIsStillAnEntry()
        {
            List<(string, string)> existing = new()
            {
                ("ResoluteShipView", RESOLUTE_GUID),
                ("VenatorIIShipView", RESOLUTE_GUID),
                ("RemovedShipView", REMOVED_GUID),
            };
            List<(string, string)> entries = new() { ("ResoluteShipView", RESOLUTE_GUID) };

            List<(string Key, string Guid)> mappings = AssetMappingContextMenu.BuildMappings(existing, entries);

            Assert.That(mappings, Is.EquivalentTo(new[]
            {
                ("ResoluteShipView", RESOLUTE_GUID),
                ("VenatorIIShipView", RESOLUTE_GUID),
            }));
        }

        [Test]
        public void GetMappingKey_SameTypeAssetsUseTheirDistinctAddresses()
        {
            MiningFacilityData baseData = ScriptableObject.CreateInstance<MiningFacilityData>();
            MiningFacilityData asteroidData = ScriptableObject.CreateInstance<MiningFacilityData>();
            try
            {
                Assert.That(AssetMappingContextMenu.GetMappingKey(baseData, "MiningFacilityData"),
                    Is.EqualTo("MiningFacilityData"));
                Assert.That(AssetMappingContextMenu.GetMappingKey(asteroidData, "AsteroidMiningFacilityData"),
                    Is.EqualTo("AsteroidMiningFacilityData"));
                Assert.That(AssetMappingContextMenu.GetMappingKey(asteroidData, null),
                    Is.EqualTo(nameof(MiningFacilityData)));
            }
            finally
            {
                Object.DestroyImmediate(baseData);
                Object.DestroyImmediate(asteroidData);
            }
        }

        [Test]
        public void SavedMapping_KeepsVenatorIIAliasAndBaseMiningFacilityKey()
        {
            AssetMappingData mapping = AssetDatabase.LoadAssetAtPath<AssetMappingData>("Assets/Settings/AssetMappingData.asset");
            SerializedProperty mappings = new SerializedObject(mapping).FindProperty("assetMappings.keyValue");

            Assert.That(AssetMappingContextMenu.FindMappedGuid(mappings, "VenatorIIShipView"), Is.EqualTo(RESOLUTE_GUID));
            Assert.That(AssetDatabase.GUIDToAssetPath(AssetMappingContextMenu.FindMappedGuid(mappings, "MiningFacilityData")),
                Does.Not.Contain("Asteroid"));
        }
    }
}
