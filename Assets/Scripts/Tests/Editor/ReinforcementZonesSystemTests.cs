using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ReinforcementZonesSystemTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(FactionType.Republic, FactionType.Separatist)]
        [TestCase(FactionType.Separatist, FactionType.Republic)]
        public void Initialize_DifferentSelectedFactions_KeepsDefaultZonesClear(
            FactionType playerFaction, FactionType opponentFaction)
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();

            try
            {
                Vector3 republicStation = new Vector3(-180f, -40f, 170f);
                Vector3 separatistStation = new Vector3(160f, -40f, -170f);
                ReinforcementZoneView playerZone = CreateZone(
                    root.transform,
                    PlayerType.Player,
                    false,
                    new Vector3(-180f, 0f, 170f));
                ReinforcementZoneView opponentZone = CreateZone(
                    root.transform,
                    PlayerType.Opponent,
                    false,
                    new Vector3(160f, 0f, -170f));
                ReinforcementZoneView capturableZone = CreateZone(
                    root.transform,
                    PlayerType.None,
                    true,
                    new Vector3(25f, 0f, 35f));
                ReinforcementZoneView randomZone = CreateZone(
                    root.transform,
                    PlayerType.None,
                    true,
                    new Vector3(-65f, 0f, 45f));
                ReinforcementZonesSystem system = root.AddComponent<ReinforcementZonesSystem>();

                SetField(system, "_zoneViews", new[] { randomZone, playerZone, opponentZone, capturableZone });
                SetField(system, "_data", data);
                SetField(
                    system,
                    "_mapModel",
                    new FakeMapModel(republicStation, separatistStation));
                SetField(system, "_playerFactionType", playerFaction);
                SetField(system, "_opponentFactionType", opponentFaction);

                system.Initialize();

                foreach (ReinforcementZoneView zone in new[] { playerZone, opponentZone })
                {
                    FactionType faction = zone == playerZone ? playerFaction : opponentFaction;
                    Vector3 station = faction == FactionType.Republic ? republicStation : separatistStation;
                    station.y = zone.Center.y;
                    float stationRadius = faction == FactionType.Republic ? 135f : 90f;
                    Assert.That(Vector3.Distance(zone.Center, station),
                        Is.EqualTo(stationRadius + zone.Radius + 30f).Within(0.01f));
                    Assert.That(zone.Center.z, Is.EqualTo(station.z));
                    Assert.That(Mathf.Sign(zone.Center.x - station.x),
                        Is.EqualTo(station.x < 0f ? 1f : -1f));
                    Assert.That(Vector3.Distance(zone.Center, capturableZone.Center),
                        Is.GreaterThanOrEqualTo(zone.Radius + capturableZone.Radius + 30f));
                    Assert.That(Vector3.Distance(zone.Center, randomZone.Center),
                        Is.GreaterThanOrEqualTo(zone.Radius + randomZone.Radius + 30f));
                    Assert.That(Mathf.Abs(zone.Center.x) + zone.Radius, Is.LessThanOrEqualTo(250f));
                    Assert.That(Mathf.Abs(zone.Center.z) + zone.Radius, Is.LessThanOrEqualTo(250f));
                }
                Assert.That(Vector3.Distance(playerZone.Center, opponentZone.Center),
                    Is.GreaterThanOrEqualTo(playerZone.Radius + opponentZone.Radius + 30f));
                Assert.That(randomZone.Center, Is.EqualTo(Vector3.zero));
                Assert.That(Vector3.Distance(capturableZone.Center, randomZone.Center),
                    Is.EqualTo(150f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void StructureClearance_RejectsFootprintOverlappingZone()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform, PlayerType.Opponent,
                    true, Vector3.zero);
                ReinforcementZonesSystem system = CreateSystem(root, data, zone);

                Assert.That(system.IsPositionInAnyZone(new Vector3(50f, 0f, 0f)), Is.False);
                Assert.That(system.IsPositionInAnyZone(new Vector3(50f, 0f, 0f), 16f), Is.True);
                Assert.That(system.IsPositionInAnyZone(new Vector3(64f, 0f, 0f), 16f), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void StructureFallback_UsesOnlyOwnedCapturableZonesAndClearsPreviousCenters()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView captured = CreateZone(
                    root.transform, PlayerType.Opponent, true, new Vector3(55f, 0f, -55f));
                ReinforcementZonesSystem system = CreateSystem(root, data,
                    CreateZone(root.transform, PlayerType.Opponent, false, Vector3.zero),
                    captured,
                    CreateZone(root.transform, PlayerType.Player, true, Vector3.left * 80f),
                    CreateZone(root.transform, PlayerType.None, true, Vector3.right * 80f));
                List<Vector3> centers = new List<Vector3> { Vector3.one };

                system.CopyOwnedCapturableZoneCenters(PlayerType.Opponent, centers);

                Assert.That(centers, Is.EqualTo(new[] { captured.Center }));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [TestCase(40f, true)]
        [TestCase(1f, false)]
        public void EnemySpawn_PrefersCapturedZoneAndFallsBackWhenShipDoesNotFit(
            float capturedRadius,
            bool usesCapturedZone)
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView home = CreateZone(
                    root.transform, PlayerType.Opponent, false, new Vector3(160f, 0f, -170f));
                ReinforcementZoneView captured = CreateZone(
                    root.transform, PlayerType.Opponent, true, Vector3.zero);
                SetField(captured, "_radius", capturedRadius);
                ReinforcementZonesSystem system = CreateSystem(root, data, home, captured);
                SetField(system, "_shipService", new ShipService());
                Dictionary<ShipType, float> radii = (Dictionary<ShipType, float>)
                    typeof(ReinforcementZonesSystem).GetField(
                        "_shipNavigationRadii", PRIVATE_INSTANCE).GetValue(system);
                radii.Add(ShipType.Arquitens, 5f);

                bool found = system.TryGetRandomSpawnPosition(
                    PlayerType.Opponent, ShipType.Arquitens, out Vector3 position);

                Assert.That(found, Is.True);
                ReinforcementZoneView expectedZone = usesCapturedZone ? captured : home;
                Assert.That(Vector3.Distance(position, expectedZone.Center),
                    Is.LessThanOrEqualTo(expectedZone.Radius - 5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        private static ReinforcementZonesSystem CreateSystem(GameObject root,
            ReinforcementZoneData data, params ReinforcementZoneView[] zones)
        {
            ReinforcementZonesSystem system = root.AddComponent<ReinforcementZonesSystem>();
            SetField(system, "_zoneViews", zones);
            SetField(system, "_data", data);
            SetField(system, "_mapModel", new FakeMapModel(
                new Vector3(-180f, 0f, 170f), new Vector3(160f, 0f, -170f)));
            SetField(system, "_playerFactionType", FactionType.Republic);
            SetField(system, "_opponentFactionType", FactionType.Separatist);
            SetField(system, "_shipNavigationService", new FakeShipNavigationService());
            system.Initialize();
            return system;
        }

        private static ReinforcementZoneView CreateZone(
            Transform parent,
            PlayerType owner,
            bool isCapturable,
            Vector3 center)
        {
            GameObject gameObject = new GameObject($"{owner}Zone");
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = center;
            ReinforcementZoneView view = gameObject.AddComponent<ReinforcementZoneView>();
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

        private sealed class FakeMapModel : IMapModelObserver
        {
            private readonly Vector3 _republicPosition;
            private readonly Vector3 _separatistPosition;
            private readonly Vector2Range _sizeRange;

            public FakeMapModel(Vector3 republicPosition, Vector3 separatistPosition)
            {
                _republicPosition = republicPosition;
                _separatistPosition = separatistPosition;
                _sizeRange = new Vector2Range();
                SetRangeValue("<Min>k__BackingField", new Vector2(-250f, -250f));
                SetRangeValue("<Max>k__BackingField", new Vector2(250f, 250f));
            }

            public Vector2Range SizeRange => _sizeRange;

            public Vector3 GetStationPosition(FactionType factionType)
            {
                return factionType switch
                {
                    FactionType.Republic => _republicPosition,
                    FactionType.Separatist => _separatistPosition,
                    _ => throw new System.ArgumentOutOfRangeException(
                        nameof(factionType),
                        factionType,
                        null)
                };
            }

            private void SetRangeValue(string fieldName, Vector2 value)
            {
                FieldInfo field = _sizeRange.GetType().BaseType?.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(_sizeRange, value);
            }
        }

        private sealed class FakeShipNavigationService : IShipNavigationService
        {
            public string Id => nameof(FakeShipNavigationService);

            public void Register(
                IShipNavigationAgent agent,
                Vector3 initialFinalPosition)
            {
            }

            public void Unregister(IShipNavigationAgent agent)
            {
            }

            public void Stop(IShipNavigationAgent agent)
            {
            }

            public void CancelPendingDestination(IShipNavigationAgent agent)
            {
            }

            public bool IsPositionClear(Vector3 position, float navigationRadius)
            {
                return true;
            }

            public bool IsPositionClear(
                IShipNavigationAgent agent,
                Vector3 position,
                float navigationRadius)
            {
                return true;
            }

            public bool TryResolveInitialFinalPosition(
                IShipNavigationAgent agent,
                Vector3 requestedPosition,
                Vector2Range mapRange,
                float heightTolerance,
                out Vector3 resolvedPosition)
            {
                resolvedPosition = requestedPosition;
                return true;
            }

            public ShipNavigationPlan Plan(
                IShipNavigationAgent agent,
                Vector3 forward,
                Vector3 requestedDestination,
                IReadOnlyList<RadarContact> obstacleContacts,
                float heightTolerance,
                float clearance,
                Vector2Range mapRange,
                bool preserveCourse = false,
                bool reserveAsPending = false)
            {
                throw new System.NotSupportedException();
            }
        }
    }
}
