using System;
using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using VContainer;
using VContainer.Internal;
using IPostTickable = EmpireAtWar.Lifecycle.IPostTickable;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PostTickLifecycleTests
    {
        [Test]
        public void PostTick_UsesAscendingOrderAndStableTies()
        {
            var calls = new List<string>();
            using var coordinator = CreateCoordinator(new IPostTickable[]
            {
                new LaterPostTick(calls, "later"), new PostTickProbe(calls, "first"),
                new EarlierPostTick(calls, "earlier"), new PostTickProbe(calls, "second")
            }, new LifecycleTickGate());

            coordinator.PostTick();

            Assert.That(calls, Is.EqualTo(new[] { "earlier", "first", "second", "later" }));
        }

        [Test]
        public void PostTick_RespectsPoolGateAndDoesNotRunAfterDisposal()
        {
            var calls = new List<string>();
            var gate = new LifecycleTickGate();
            using var coordinator = CreateCoordinator(new[] { new PostTickProbe(calls, "post") }, gate);
            gate.SetTicking(false);
            coordinator.PostTick();
            Assert.That(calls, Is.Empty);

            gate.SetTicking(true);
            coordinator.PostTick();
            coordinator.Dispose();
            coordinator.PostTick();
            Assert.That(calls, Is.EqualTo(new[] { "post" }));
        }

        [Test]
        public void ChildScope_PostTickUsesOnlyLocalCallbacksAndLocalGate()
        {
            var calls = new List<string>();
            var builder = new ContainerBuilder();
            builder.RegisterLifecycle();
            builder.RegisterInstance(new PostTickProbe(calls, "parent")).AsImplementedInterfaces();
            using IObjectResolver parent = builder.Build();
            using IScopedObjectResolver child = parent.CreateScope(childBuilder =>
            {
                childBuilder.RegisterLifecycle();
                childBuilder.RegisterInstance(new PostTickProbe(calls, "child")).AsImplementedInterfaces();
            });

            OrderedLifecycleCoordinator parentCoordinator = parent.Resolve<OrderedLifecycleCoordinator>();
            OrderedLifecycleCoordinator childCoordinator = child.Resolve<OrderedLifecycleCoordinator>();
            parentCoordinator.PostTick();
            childCoordinator.PostTick();
            Assert.That(calls, Is.EqualTo(new[] { "parent", "child" }));

            calls.Clear();
            child.Resolve<LifecycleTickGate>().SetTicking(false);
            parentCoordinator.PostTick();
            childCoordinator.PostTick();
            Assert.That(calls, Is.EqualTo(new[] { "parent" }));
            Assert.That(parent.Resolve<IReadOnlyList<VContainer.Unity.IPostTickable>>(),
                Is.EqualTo(new[] { parentCoordinator }));
        }

        [Test]
        public void InstalledPlayerLoop_PostTickFollowsAllUpdateAndPrecedesLateUpdate()
        {
            // Building registers the real dispatcher; inspect its installed phases without running them.
            var builder = new ContainerBuilder();
            builder.RegisterLifecycle();
            using IObjectResolver container = builder.Build();
            var phases = new List<Type>();
            CollectPhases(UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop(), phases);
            int tick = phases.IndexOf(typeof(VContainer.Unity.VContainerUpdate));
            int update = phases.IndexOf(typeof(Update.ScriptRunBehaviourUpdate));
            int postTick = phases.IndexOf(typeof(VContainer.Unity.VContainerPostUpdate));
            int lateTick = phases.IndexOf(typeof(VContainer.Unity.VContainerLateUpdate));
            int lateUpdate = phases.IndexOf(typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate));

            Assert.That(tick, Is.GreaterThanOrEqualTo(0));
            Assert.That(update, Is.GreaterThan(tick));
            Assert.That(postTick, Is.GreaterThan(update));
            Assert.That(lateTick, Is.GreaterThan(postTick));
            Assert.That(lateUpdate, Is.GreaterThan(lateTick));
        }

        [Test]
        public void ChildProducer_PostTickCompletesLatestPoseBeforeCombatDespiteChildGateClosing()
        {
            var fighterObject = new GameObject("PostTick fighter");
            var bodyObject = new GameObject("PostTick fighter body");
            bodyObject.transform.SetParent(fighterObject.transform, false);
            try
            {
                var batch = new FighterPoseBatch();
                int slot = batch.Register(fighterObject.transform, bodyObject.transform, Quaternion.identity);
                var combat = new CombatPoseProbe(fighterObject.transform);
                var builder = new ContainerBuilder();
                builder.RegisterLifecycle();
                builder.RegisterInstance(batch).AsImplementedInterfaces();
                builder.RegisterInstance(combat).AsImplementedInterfaces();
                using IObjectResolver parent = builder.Build();
                Vector3 expected = new Vector3(10f, 20f, 30f);
                using IScopedObjectResolver child = parent.CreateScope(childBuilder =>
                {
                    childBuilder.RegisterLifecycle();
                    childBuilder.RegisterInstance(new PoseProducer(batch, slot, expected)).AsImplementedInterfaces();
                });

                OrderedLifecycleCoordinator parentCoordinator = parent.Resolve<OrderedLifecycleCoordinator>();
                OrderedLifecycleCoordinator childCoordinator = child.Resolve<OrderedLifecycleCoordinator>();
                parentCoordinator.Tick();
                childCoordinator.Tick();
                parentCoordinator.PostTick();
                child.Resolve<LifecycleTickGate>().SetTicking(false);
                parentCoordinator.LateTick();
                childCoordinator.LateTick();

                Assert.That(combat.Position, Is.EqualTo(expected));
                Assert.That(parent.Resolve<LifecycleTickGate>().IsTicking, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fighterObject);
            }
        }
        private static void CollectPhases(PlayerLoopSystem phase, List<Type> types)
        {
            types.Add(phase.type);
            if (phase.subSystemList == null) return;
            foreach (PlayerLoopSystem child in phase.subSystemList) CollectPhases(child, types);
        }

        private static OrderedLifecycleCoordinator CreateCoordinator(
            IReadOnlyList<IPostTickable> callbacks, LifecycleTickGate gate) =>
            new OrderedLifecycleCoordinator(
                new ContainerLocal<IReadOnlyList<IInitializable>>(Array.Empty<IInitializable>()),
                new ContainerLocal<IReadOnlyList<ITickable>>(Array.Empty<ITickable>()),
                new ContainerLocal<IReadOnlyList<IPostTickable>>(callbacks),
                new ContainerLocal<IReadOnlyList<IFixedTickable>>(Array.Empty<IFixedTickable>()),
                new ContainerLocal<IReadOnlyList<ILateTickable>>(Array.Empty<ILateTickable>()),
                new ContainerLocal<IReadOnlyList<IOrderedDisposable>>(Array.Empty<IOrderedDisposable>()),
                new ContainerLocal<IReadOnlyList<ILateDisposable>>(Array.Empty<ILateDisposable>()), gate);

        private sealed class PoseProducer : ITickable
        {
            private readonly FighterPoseBatch _batch;
            private readonly int _slot;
            private readonly Vector3 _position;

            public PoseProducer(FighterPoseBatch batch, int slot, Vector3 position)
            {
                _batch = batch;
                _slot = slot;
                _position = position;
            }

            public void Tick() => _batch.Set(_slot, _position, Vector3.forward, 0f);
        }

        [LifecycleOrder(LateTick = LifecycleOrders.COMBAT_LATE_TICK)]
        private sealed class CombatPoseProbe : ILateTickable
        {
            private readonly Transform _fighter;
            public Vector3 Position { get; private set; }

            public CombatPoseProbe(Transform fighter) => _fighter = fighter;
            public void LateTick() => Position = _fighter.position;
        }
        private class PostTickProbe : IPostTickable
        {
            private readonly List<string> _calls;
            private readonly string _name;

            public PostTickProbe(List<string> calls, string name)
            {
                _calls = calls;
                _name = name;
            }

            public void PostTick() => _calls.Add(_name);
        }

        [LifecycleOrder(PostTick = -10)]
        private sealed class EarlierPostTick : PostTickProbe
        {
            public EarlierPostTick(List<string> calls, string name) : base(calls, name) { }
        }

        [LifecycleOrder(PostTick = 10)]
        private sealed class LaterPostTick : PostTickProbe
        {
            public LaterPostTick(List<string> calls, string name) : base(calls, name) { }
        }
    }
}
