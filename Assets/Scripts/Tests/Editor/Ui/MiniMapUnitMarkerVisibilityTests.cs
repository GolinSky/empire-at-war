using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class MiniMapUnitMarkerVisibilityTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void MarkerUsesHullOrPointAndFiringRevealButStillRejectsCloak(bool hasFootprint)
        {
            GameObject target = new GameObject("Target");
            GameObject source = new GameObject("Source");
            MiniMapData data = ScriptableObject.CreateInstance<MiniMapData>();
            MiniMapUnitMarkerPresenter presenter = null;
            try
            {
                target.transform.position = Vector3.right * 300f;
                var relations = TestPlayers.CreateTeamGame();
                float now = 0f;
                var vision = new VisionService(relations, true, () => now);
                var modifiers = new CombatModifiers();
                var footprint = new ShipVisionFootprint(target.transform, 7, new[]
                {
                    new Vector2(-250f, 0f), new Vector2(50f, -50f), new Vector2(50f, 50f)
                });
                presenter = new MiniMapUnitMarkerPresenter(new Health(), vision,
                    TestPlayers.CreateLocalPlayer(relations), data, target.transform, TestPlayers.Enemy,
                    SelectionType.Ship, modifiers, new SlowTicks(), null,
                    hasFootprint ? new IVisionFootprintFacade[] { footprint } : Array.Empty<IVisionFootprintFacade>(), 7);
                presenter.Initialize();
                MiniMapMarker marker = data.Markers[0];
                vision.Register(TestPlayers.Human, source.transform, 100f);

                Assert.That(vision.IsVisible(TestPlayers.Human, target.transform.position), Is.False);
                presenter.SlowTick();
                Assert.That(marker.Visible, Is.EqualTo(hasFootprint));
                vision.Unregister(source.transform);
                presenter.SlowTick();
                Assert.That(marker.Visible, Is.False);

                vision.Reveal(TestPlayers.Enemy, 7, 5f);
                vision.Reveal(TestPlayers.Ally, 8, 5f);
                Assert.That(marker.Visible, Is.False, "Other viewers and entity IDs do not reveal this marker.");
                vision.Reveal(TestPlayers.Ally, 7, 5f);
                Assert.That(marker.Visible, Is.True);
                now = 5f;
                vision.Tick();
                Assert.That(marker.Visible, Is.False);
                vision.Reveal(TestPlayers.Ally, 7, 5f);
                Assert.That(marker.Visible, Is.True);
                modifiers.SetCloaked(true);
                presenter.SlowTick();
                Assert.That(marker.Visible, Is.False);

                presenter.Despawn();
                Assert.That(data.Markers, Is.Empty);
                now = 10f;
                Assert.DoesNotThrow(vision.Tick);
            }
            finally
            {
                if (presenter != null) presenter.LateDispose();
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private sealed class SlowTicks : ISlowTickService
        {
            public void Add(ISlowTickable tickable) { }
            public void Remove(ISlowTickable tickable) { }
        }

        private sealed class Health : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Enemy;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
