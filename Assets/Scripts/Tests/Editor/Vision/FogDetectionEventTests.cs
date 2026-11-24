using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Vision")]
    public sealed class FogDetectionEventTests
    {
        [Test]
        public void VisibilityEventsExcludeInitializationAndRespawnButTrackLiveChanges()
        {
            GameObject owner = new GameObject("FogDetectionTest");
            FogVisibilityComponent fog = owner.AddComponent<FogVisibilityComponent>();
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            Vision vision = new Vision();
            try
            {
                typeof(FogVisibilityComponent).GetField("renderers", PRIVATE_INSTANCE)
                    .SetValue(fog, Array.Empty<Renderer>());
                typeof(FogVisibilityComponent).GetMethod("Construct", PRIVATE_INSTANCE).Invoke(fog, new object[]
                {
                    vision, TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()),
                    new CombatModifiers(), new SlowTicks(),
                    Array.Empty<IVisionFootprintFacade>(), 42L
                });
                List<bool> visible = new List<bool>();
                fog.VisibilityChanged += visible.Add;

                fog.Initialize();
                fog.SlowTick();
                Assert.That(visible, Is.Empty);
                vision.Visible = true;
                fog.SlowTick();
                fog.SlowTick();
                vision.Visible = false;
                fog.SlowTick();
                Assert.That(visible, Is.EqualTo(new[] { true, false }));

                fog.Despawn();
                vision.Visible = true;
                fog.Respawn();
                fog.SlowTick();
                Assert.That(visible, Has.Count.EqualTo(2));
            }
            finally
            {
                fog.LateDispose();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void CloakUpdatesHullAndVisibilityListenersBeforeNextSlowTick()
        {
            GameObject owner = new GameObject("CloakDetectionTest");
            FogVisibilityComponent fog = owner.AddComponent<FogVisibilityComponent>();
            MeshRenderer hull = owner.AddComponent<MeshRenderer>();
            var modifiers = new CombatModifiers();
            var vision = new Vision { Visible = true };
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(FogVisibilityComponent).GetField("renderers", PRIVATE_INSTANCE)
                    .SetValue(fog, new Renderer[] { hull });
                typeof(FogVisibilityComponent).GetMethod("Construct", PRIVATE_INSTANCE).Invoke(fog, new object[]
                {
                    vision, TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()),
                    modifiers, new SlowTicks(),
                    Array.Empty<IVisionFootprintFacade>(), 42L
                });
                var visibility = new List<bool>();
                fog.VisibilityChanged += visible =>
                {
                    Assert.That(hull.forceRenderingOff, Is.EqualTo(!visible));
                    visibility.Add(visible);
                };

                fog.Initialize();
                modifiers.SetCloaked(true);
                modifiers.SetCloaked(true);
                Assert.That(fog.IsHiddenByFog, Is.True);
                Assert.That(visibility, Is.EqualTo(new[] { false }));

                modifiers.SetCloaked(false);
                Assert.That(fog.IsHiddenByFog, Is.False);
                Assert.That(visibility, Is.EqualTo(new[] { false, true }));

                modifiers.SetCloaked(true);
                vision.Visible = false;
                modifiers.SetCloaked(false);
                Assert.That(fog.IsHiddenByFog, Is.True);
                Assert.That(visibility, Is.EqualTo(new[] { false, true, false }));

                fog.Despawn();
                vision.Visible = true;
                modifiers.SetCloaked(true);
                modifiers.SetCloaked(false);
                Assert.That(visibility, Has.Count.EqualTo(3));

                fog.Respawn();
                modifiers.SetCloaked(true);
                Assert.That(visibility, Is.EqualTo(new[] { false, true, false, false }));
            }
            finally
            {
                fog.LateDispose();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FiringReveal_RefreshesHullImmediately_AndCloakStillHidesIt(bool hasFootprint)
        {
            var owner = new GameObject("Firing fog");
            FogVisibilityComponent fog = owner.AddComponent<FogVisibilityComponent>();
            MeshRenderer hull = owner.AddComponent<MeshRenderer>();
            var modifiers = new CombatModifiers();
            float now = 0f;
            var vision = new VisionService(TestPlayers.CreateTeamGame(), true, () => now);
            var footprint = new ShipVisionFootprint(owner.transform, 42, new[]
            {
                new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(0f, 1f)
            });
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(FogVisibilityComponent).GetField("renderers", PRIVATE_INSTANCE)
                    .SetValue(fog, new Renderer[] { hull });
                typeof(FogVisibilityComponent).GetMethod("Construct", PRIVATE_INSTANCE).Invoke(fog, new object[]
                {
                    vision, TestPlayers.CreateLocalPlayer(TestPlayers.CreateTeamGame()),
                    modifiers, new SlowTicks(),
                    hasFootprint ? new IVisionFootprintFacade[] { footprint } : Array.Empty<IVisionFootprintFacade>(), 42L
                });
                fog.Initialize();
                Assert.That(hull.forceRenderingOff, Is.True);
                vision.Reveal(TestPlayers.Enemy, 42, 5f);
                Assert.That(hull.forceRenderingOff, Is.True);
                vision.Reveal(TestPlayers.Ally, 99, 5f);
                Assert.That(hull.forceRenderingOff, Is.True);
                vision.Reveal(TestPlayers.Ally, 42, 5f);
                Assert.That(hull.forceRenderingOff, Is.False);
                modifiers.SetCloaked(true);
                Assert.That(hull.forceRenderingOff, Is.True);
                modifiers.SetCloaked(false);
                Assert.That(hull.forceRenderingOff, Is.False);
                now = 5f;
                vision.Tick();
                Assert.That(hull.forceRenderingOff, Is.True);
            }
            finally
            {
                fog.LateDispose();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ShipCadence_StaggersChecksSkipsCatchupAndResetsAfterRespawn()
        {
            var firstOwner = new GameObject("First fog ship");
            var secondOwner = new GameObject("Second fog ship");
            var pointOwner = new GameObject("Fog structure");
            var vision = new Vision();
            var slowTicks = new SlowTicks();
            FogVisibilityComponent first = Configure(firstOwner, vision, slowTicks, 40, true);
            FogVisibilityComponent second = Configure(secondOwner, vision, slowTicks, 45, true);
            FogVisibilityComponent point = Configure(pointOwner, vision, slowTicks, 46, false);
            try
            {
                first.Initialize();
                second.Initialize();
                point.Initialize();
                Assert.That(slowTicks.Registered, Is.EqualTo(new[] { point }));
                vision.HullQueries = 0;
                vision.Visible = true;
                first.Advance(0.06f);
                second.Advance(0.06f);
                point.Advance(0.06f);
                Assert.That(first.IsHiddenByFog, Is.True);
                Assert.That(second.IsHiddenByFog, Is.False);
                Assert.That(point.IsHiddenByFog, Is.True);
                first.Advance(0.05f);
                second.Advance(0.05f);
                Assert.That(first.IsHiddenByFog, Is.False);
                Assert.That(vision.HullQueries, Is.EqualTo(2));
                point.SlowTick();
                Assert.That(point.IsHiddenByFog, Is.False);

                first.Advance(5f);
                Assert.That(vision.HullQueries, Is.EqualTo(3), "One query, not a backlog replay.");
                first.Despawn();
                first.Advance(1f);
                Assert.That(vision.HullQueries, Is.EqualTo(3));
                vision.Visible = false;
                first.Respawn();
                vision.Visible = true;
                first.Advance(0.06f);
                Assert.That(first.IsHiddenByFog, Is.True);
                first.Advance(0.05f);
                Assert.That(first.IsHiddenByFog, Is.False);
                Assert.That(slowTicks.Registered, Is.EqualTo(new[] { point }));
            }
            finally
            {
                first.LateDispose();
                second.LateDispose();
                point.LateDispose();
                UnityEngine.Object.DestroyImmediate(firstOwner);
                UnityEngine.Object.DestroyImmediate(secondOwner);
                UnityEngine.Object.DestroyImmediate(pointOwner);
            }
        }

        private static FogVisibilityComponent Configure(GameObject owner, Vision vision, SlowTicks slowTicks,
            long entityId, bool hasFootprint)
        {
            FogVisibilityComponent fog = owner.AddComponent<FogVisibilityComponent>();
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FogVisibilityComponent).GetField("renderers", PRIVATE_INSTANCE)
                .SetValue(fog, Array.Empty<Renderer>());
            var footprint = new ShipVisionFootprint(owner.transform, entityId, new[]
            {
                new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(0f, 1f)
            });
            typeof(FogVisibilityComponent).GetMethod("Construct", PRIVATE_INSTANCE).Invoke(fog, new object[]
            {
                vision, TestPlayers.CreateLocalPlayer(TestPlayers.CreateTeamGame()),
                new CombatModifiers(), slowTicks,
                hasFootprint ? new IVisionFootprintFacade[] { footprint } : Array.Empty<IVisionFootprintFacade>(), entityId
            });
            return fog;
        }

        private sealed class Vision : IVisionService
        {
            public event Action<PlayerId, long, bool> RevealChanged { add { } remove { } }
            public bool Visible;
            public bool FogEnabled => true;
            public bool IsFogLifted(PlayerId viewer) => false;
            public float FiringRevealSeconds => 5f;
            public bool IsRevealed(PlayerId viewer, long entityId) => false;
            public IReadOnlyList<OwnedCircle> Sources => Array.Empty<OwnedCircle>();
            public void Register(PlayerId owner, Transform source, float radius) { }
            public void Unregister(Transform source) { }
            public bool IsVisible(PlayerId viewer, Vector3 position) => Visible;
            public int HullQueries;
            public bool IsVisible(PlayerId viewer, IVisionFootprintFacade footprint)
            {
                HullQueries++;
                return Visible;
            }
            public void Reveal(PlayerId viewer, long entityId, float duration) { }
            public bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius) => Visible;
        }

        private sealed class SlowTicks : ISlowTickService
        {
            public readonly List<ISlowTickable> Registered = new List<ISlowTickable>();
            public void Add(ISlowTickable tickable) => Registered.Add(tickable);
            public void Remove(ISlowTickable tickable) => Registered.Remove(tickable);
        }
    }
}
