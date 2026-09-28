using System.Collections.Generic;
using EmpireAtWar.Models.Players;
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

        [Test]
        public void StructureClearance_RejectsFootprintOverlappingZone()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform, TestPlayers.Enemy,
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
                    root.transform, TestPlayers.Enemy, true, new Vector3(55f, 0f, -55f));
                ReinforcementZonesSystem system = CreateSystem(root, data,
                    CreateZone(root.transform, TestPlayers.Enemy, false, Vector3.zero),
                    captured,
                    CreateZone(root.transform, TestPlayers.Human, true, Vector3.left * 80f),
                    CreateZone(root.transform, PlayerId.None, true, Vector3.right * 80f));
                List<Vector3> centers = new List<Vector3> { Vector3.one };

                system.CopyOwnedCapturableZoneCenters(TestPlayers.Enemy, centers);

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
                    root.transform, TestPlayers.Enemy, false, new Vector3(160f, 0f, -170f));
                ReinforcementZoneView captured = CreateZone(
                    root.transform, TestPlayers.Enemy, true, Vector3.zero);
                SetField(captured, "_radius", capturedRadius);
                ReinforcementZonesSystem system = CreateSystem(root, data, home, captured);
                SetField(system, "_shipService", new ShipService());
                Dictionary<ShipType, float> radii = (Dictionary<ShipType, float>)
                    typeof(ReinforcementZonesSystem).GetField(
                        "_shipNavigationRadii", PRIVATE_INSTANCE).GetValue(system);
                radii.Add(ShipType.Arquitens, 5f);

                bool found = system.TryGetRandomSpawnPosition(
                    TestPlayers.Enemy, ShipType.Arquitens, out Vector3 position);

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
            SetField(system, "_shipNavigationService", new FakeShipNavigationService());
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

            public Vector3 GetStationPosition(PlayerId owner)
            {
                return owner == TestPlayers.Human ? _republicPosition : _separatistPosition;
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
