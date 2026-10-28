using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.FogOfWar;
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
                    Array.Empty<IIonStunViewSource>(), new CombatModifiers(), new SlowTicks()
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

        private sealed class Vision : IVisionService
        {
            public bool Visible;
            public IReadOnlyList<OwnedCircle> Sources => Array.Empty<OwnedCircle>();
            public void Register(PlayerId owner, Transform source, float radius) { }
            public void Unregister(Transform source) { }
            public bool IsVisible(PlayerId viewer, Vector3 position) => Visible;
            public bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius) => Visible;
        }

        private sealed class SlowTicks : ISlowTickService
        {
            public void Add(ISlowTickable tickable) { }
            public void Remove(ISlowTickable tickable) { }
        }
    }
}
