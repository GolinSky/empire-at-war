using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
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
        public void Initialize_DifferentSelectedFactions_AnchorsDefaultZonesToOwningStations()
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
                ReinforcementZonesSystem system = root.AddComponent<ReinforcementZonesSystem>();

                SetField(system, "_zoneViews", new[] { playerZone, opponentZone, capturableZone });
                SetField(system, "_data", data);
                SetField(
                    system,
                    "_mapModel",
                    new FakeMapModel(republicStation, separatistStation));
                SetField(system, "_playerFactionType", FactionType.Separatist);
                SetField(system, "_opponentFactionType", FactionType.Republic);

                system.Initialize();

                Assert.That(playerZone.Center, Is.EqualTo(new Vector3(160f, 0f, -170f)));
                Assert.That(opponentZone.Center, Is.EqualTo(new Vector3(-180f, 0f, 170f)));
                Assert.That(capturableZone.Center, Is.EqualTo(new Vector3(25f, 0f, 35f)));
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
                Vector3 capturedPosition = new Vector3(55f, 0f, -55f);
                ReinforcementZonesSystem system = CreateSystem(root, data,
                    CreateZone(root.transform, PlayerType.Opponent, false, Vector3.zero),
                    CreateZone(root.transform, PlayerType.Opponent, true, capturedPosition),
                    CreateZone(root.transform, PlayerType.Player, true, Vector3.left * 80f),
                    CreateZone(root.transform, PlayerType.None, true, Vector3.right * 80f));
                List<Vector3> centers = new List<Vector3> { Vector3.one };

                system.CopyOwnedCapturableZoneCenters(PlayerType.Opponent, centers);

                Assert.That(centers, Is.EqualTo(new[] { capturedPosition }));
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

            public FakeMapModel(Vector3 republicPosition, Vector3 separatistPosition)
            {
                _republicPosition = republicPosition;
                _separatistPosition = separatistPosition;
            }

            public Vector2Range SizeRange => default;

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
        }
    }
}
