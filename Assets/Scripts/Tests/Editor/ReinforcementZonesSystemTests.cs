using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using System.Reflection;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ReinforcementZones;
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
        public void StructureFallback_UsesOnlyOwnedCapturableZonesAndClearsPreviousBounds()
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
                List<Bounds> bounds = new List<Bounds> { new Bounds(Vector3.one, Vector3.one) };

                system.CopyOwnedCapturableZoneBounds(TestPlayers.Enemy, bounds);

                Assert.That(bounds, Has.Count.EqualTo(1));
                Assert.That(bounds[0].center, Is.EqualTo(captured.Center));
                Assert.That(bounds[0].extents.x, Is.EqualTo(captured.Radius));
                Assert.That(bounds[0].extents.z, Is.EqualTo(captured.Radius));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void AlliedZone_AcceptsAlliesAndRejectsHostiles()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView allied = CreateZone(
                    root.transform, TestPlayers.Ally, false, Vector3.zero);
                ReinforcementZonesSystem system = CreateSystem(root, data, allied);

                Assert.That(system.IsPositionInAlliedZone(TestPlayers.Human, allied.Center), Is.True);
                Assert.That(system.IsPositionInAlliedZone(TestPlayers.Enemy, allied.Center), Is.False);
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
            PlayerRoster roster = TestPlayers.CreateTeamGame();
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

        private sealed class FakeMapModel : IMapModelObserver
        {
            private readonly Vector2Range _sizeRange;

            private readonly Vector3 _republicPosition;
            private readonly Vector3 _separatistPosition;

            public Vector2Range SizeRange => _sizeRange;

            public FakeMapModel(Vector3 republicPosition, Vector3 separatistPosition)
            {
                _republicPosition = republicPosition;
                _separatistPosition = separatistPosition;
                _sizeRange = new Vector2Range();
                SetRangeValue("<Min>k__BackingField", new Vector2(-250f, -250f));
                SetRangeValue("<Max>k__BackingField", new Vector2(250f, 250f));
            }

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
    }
}
