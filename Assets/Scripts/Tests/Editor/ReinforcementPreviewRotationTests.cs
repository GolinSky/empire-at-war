using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.StationFacing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
    public sealed class ReinforcementPreviewRotationTests
    {
        [TestCase(DefendPlatformType.GolanIII, "AotrGolanIII", 45f)]
        [TestCase(DefendPlatformType.GolanIII, "AotrGolanIII", 180f)]
        [TestCase(DefendPlatformType.Empress, "AotrEmpress", 45f)]
        [TestCase(DefendPlatformType.Empress, "AotrEmpress", 180f)]
        public void PlatformPreview_MatchesSpawnRotation(
            DefendPlatformType type, string assetPrefix, float stationYaw)
        {
            UnitSpawnView prefab = AssetDatabase.LoadAssetAtPath<UnitSpawnView>(
                $"Assets/Prefabs/Ui/Reinforcement/{assetPrefix}DefensePlatformReinforcementView.prefab");
            GameObject spawnedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Models/DefendStation/{assetPrefix}DefensePlatformView.prefab");
            var owner = new PlayerSlot(new PlayerId(0), default, default, default, default, 0);
            var factory = new ReinforcementPreviewFactory(new FakeStationFacing(stationYaw), owner);
            var placement = new DefendPlatformReinforcementPlacement(
                null, factory, null, null, prefab, new PlayerId(0), type);
            UnitSpawnView preview = placement.CreatePreview();
            try
            {
                Assert.That(Quaternion.Angle(preview.transform.rotation, spawnedPrefab.transform.rotation),
                    Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(preview.gameObject);
            }
        }

        [Test]
        public void DefaultPreview_KeepsStationFacing()
        {
            UnitSpawnView prefab = AssetDatabase.LoadAssetAtPath<UnitSpawnView>(
                "Assets/Prefabs/Ui/Reinforcement/ISDIReinforcementView.prefab");
            var owner = new PlayerSlot(new PlayerId(0), default, default, default, default, 0);
            var factory = new ReinforcementPreviewFactory(new FakeStationFacing(75f), owner);
            UnitSpawnView preview = factory.Create(prefab);
            try
            {
                Assert.That(Quaternion.Angle(preview.transform.rotation, Quaternion.Euler(0f, 75f, 0f)),
                    Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(preview.gameObject);
            }
        }

        private sealed class FakeStationFacing : IStationFacingService
        {
            private readonly Quaternion _rotation;
            public FakeStationFacing(float yaw) => _rotation = Quaternion.Euler(0f, yaw, 0f);
            public Quaternion GetRotation(PlayerId owner) => _rotation;
        }
    }
}
