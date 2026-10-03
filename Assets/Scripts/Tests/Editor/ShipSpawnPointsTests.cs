using System.Reflection;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipSpawnPointsTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;
        private const float HULL_RADIUS = 5f;

        [TestCase(40f, true)]
        [TestCase(1f, false)]
        public void EnemySpawn_PrefersCapturedZoneAndFallsBackWhenShipDoesNotFit(
            float capturedRadius,
            bool usesCapturedZone)
        {
            GameObject root = new GameObject(nameof(ShipSpawnPointsTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView home = CreateZone(
                    root.transform, TestPlayers.Enemy, false, new Vector3(160f, 0f, -170f));
                ReinforcementZoneView captured = CreateZone(
                    root.transform, TestPlayers.Enemy, true, Vector3.zero);
                SetField(captured, "_radius", capturedRadius);
                PlayerRoster roster = TestPlayers.CreateTeamGame();
                ShipSpawnPoints spawnPoints = new ShipSpawnPoints(
                    CreateZoneSource(root, data, roster, home, captured), new OpenClearance(), roster);

                bool found = spawnPoints.TryGetRandomSpawnPosition(
                    TestPlayers.Enemy, ShipType.Arquitens, out Vector3 position);

                Assert.That(found, Is.True);
                ReinforcementZoneView expectedZone = usesCapturedZone ? captured : home;
                Assert.That(Vector3.Distance(position, expectedZone.Center),
                    Is.LessThanOrEqualTo(expectedZone.Radius - HULL_RADIUS));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Spawn_AllowsAlliedZonesAndRejectsHostileZones()
        {
            GameObject root = new GameObject(nameof(ShipSpawnPointsTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView allied = CreateZone(
                    root.transform, TestPlayers.Ally, false, Vector3.zero);
                PlayerRoster roster = TestPlayers.CreateTeamGame();
                ShipSpawnPoints spawnPoints = new ShipSpawnPoints(
                    CreateZoneSource(root, data, roster, allied), new OpenClearance(), roster);

                Assert.That(spawnPoints.TryGetRandomSpawnPosition(
                    TestPlayers.Human, ShipType.Arquitens, out Vector3 position), Is.True);
                Assert.That(Vector3.Distance(position, allied.Center),
                    Is.LessThanOrEqualTo(allied.Radius));
                Assert.That(spawnPoints.TryGetRandomSpawnPosition(
                    TestPlayers.Enemy, ShipType.Arquitens, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        private static ReinforcementZonesSystem CreateZoneSource(GameObject root,
            ReinforcementZoneData data, PlayerRoster roster, params ReinforcementZoneView[] zones)
        {
            ReinforcementZonesSystem system = root.AddComponent<ReinforcementZonesSystem>();
            SetField(system, "_zoneViews", zones);
            SetField(system, "_data", data);
            SetField(system, "_roster", roster);
            SetField(system, "_localPlayer", TestPlayers.CreateLocalPlayer(roster));
            system.Initialize();
            return system;
        }

        private static ReinforcementZoneView CreateZone(
            Transform parent,
            PlayerId owner,
            bool isCapturable,
            Vector3 center)
        {
            GameObject gameObject = new GameObject($"{owner}Zone");
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = center;
            ReinforcementZoneView view = gameObject.AddComponent<ReinforcementZoneView>();
            SetField(view, "_sphereRenderer", gameObject.AddComponent<MeshRenderer>());
            GameObject captureUi = new GameObject("CaptureUi", typeof(RectTransform));
            captureUi.transform.SetParent(gameObject.transform);
            SetField(view, "_captureCanvas", captureUi.AddComponent<Canvas>());
            SetField(view, "_startingOwner", owner);
            SetField(view, "_isCapturable", isCapturable);
            return view;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private sealed class OpenClearance : IShipSpawnClearance
        {
            public float GetPlanarRadius(ShipType shipType) => HULL_RADIUS;

            public bool IsClear(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public void ReserveLanding(object ship, PlayerId owner, ShipType shipType, Vector3 position)
            {
            }

            public void ReleaseLanding(object ship)
            {
            }
        }
    }
}
