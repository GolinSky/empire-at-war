using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.ViewComponents.Squadrons;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class FighterPoseBatchTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private FighterPoseBatch _batch;

        [SetUp]
        public void SetUp() => _batch = new FighterPoseBatch();

        [TearDown]
        public void TearDown()
        {
            _batch.Dispose();
            foreach (GameObject created in _objects) Object.DestroyImmediate(created);
            _objects.Clear();
        }

        [Test]
        public void LateTick_PlacesFighterAndRollsItsBody()
        {
            (Transform fighter, Transform body) = Fighter();
            Quaternion rest = Quaternion.Euler(0f, 90f, 0f);
            body.localRotation = rest;
            int slot = _batch.Register(fighter, body, rest);

            _batch.Set(slot, new Vector3(1f, 2f, 3f), Vector3.right, 30f);
            _batch.LateTick();

            Assert.That(Vector3.Distance(fighter.position, new Vector3(1f, 2f, 3f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Angle(fighter.forward, Vector3.right), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(body.localRotation, Quaternion.AngleAxis(30f, Vector3.forward) * rest),
                Is.LessThan(0.01f));
        }

        [Test]
        public void Unregister_LeavesFighterWhereItIsAndReusesTheSlot()
        {
            (Transform fighter, Transform body) = Fighter();
            int slot = _batch.Register(fighter, body, Quaternion.identity);
            _batch.Set(slot, Vector3.forward * 5f, Vector3.forward, 0f);
            _batch.Unregister(slot);

            _batch.LateTick();

            Assert.That(fighter.position, Is.EqualTo(Vector3.zero));
            (Transform next, Transform nextBody) = Fighter();
            Assert.That(_batch.Register(next, nextBody, Quaternion.identity), Is.EqualTo(slot));
        }

        [Test]
        public void PostTickThenLateTick_PublishesCurrentPoseAndBank()
        {
            (Transform fighter, Transform body) = Fighter();
            int slot = _batch.Register(fighter, body, Quaternion.identity);
            _batch.Set(slot, Vector3.one, Vector3.right, 25f);
            _batch.PostTick();
            _batch.LateTick();
            Assert.That(fighter.position, Is.EqualTo(Vector3.one));
            Assert.That(Vector3.Angle(fighter.forward, Vector3.right), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(body.localRotation, Quaternion.AngleAxis(25f, Vector3.forward)), Is.LessThan(0.01f));
        }

        [Test]
        public void SetAfterPostTick_PublishesNewestPoseAtLateTick()
        {
            (Transform fighter, Transform body) = Fighter();
            int slot = _batch.Register(fighter, body, Quaternion.identity);
            _batch.Set(slot, Vector3.one, Vector3.forward, 10f);
            _batch.PostTick();
            _batch.Set(slot, Vector3.right * 12f, Vector3.left, -30f);
            _batch.LateTick();
            Assert.That(fighter.position, Is.EqualTo(Vector3.right * 12f));
            Assert.That(Vector3.Angle(fighter.forward, Vector3.left), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(body.localRotation, Quaternion.AngleAxis(-30f, Vector3.forward)), Is.LessThan(0.01f));
        }

        [Test]
        public void UnregisterAndReuseAfterPostTick_DoesNotWriteOldFighterAgain()
        {
            (Transform oldFighter, Transform oldBody) = Fighter();
            int slot = _batch.Register(oldFighter, oldBody, Quaternion.identity);
            _batch.Set(slot, Vector3.one, Vector3.forward, 0f);
            _batch.PostTick();
            _batch.Unregister(slot);
            oldFighter.position = Vector3.up * 42f;
            (Transform fighter, Transform body) = Fighter();
            Assert.That(_batch.Register(fighter, body, Quaternion.identity), Is.EqualTo(slot));
            _batch.Set(slot, Vector3.right * 7f, Vector3.forward, 0f);
            _batch.LateTick();
            Assert.That(oldFighter.position, Is.EqualTo(Vector3.up * 42f));
            Assert.That(fighter.position, Is.EqualTo(Vector3.right * 7f));
        }

        [Test]
        public void RegisterAfterPostTick_GrowsStorageWithoutLosingQueuedPose()
        {
            (Transform first, Transform firstBody) = Fighter();
            int firstSlot = _batch.Register(first, firstBody, Quaternion.identity);
            _batch.Set(firstSlot, Vector3.one, Vector3.forward, 0f);
            _batch.PostTick();
            Transform last = null;
            for (int i = 0; i < 256; i++)
            {
                (Transform fighter, Transform body) = Fighter();
                int slot = _batch.Register(fighter, body, Quaternion.identity);
                _batch.Set(slot, Vector3.right * (i + 2), Vector3.forward, 0f);
                last = fighter;
            }
            _batch.LateTick();
            Assert.That(first.position, Is.EqualTo(Vector3.one));
            Assert.That(last.position, Is.EqualTo(Vector3.right * 257f));
        }

        [Test]
        public void DisposeWhilePending_AllowsLaterEntityUnregisterAndRepeatedDisposal()
        {
            (Transform fighter, Transform body) = Fighter();
            int slot = _batch.Register(fighter, body, Quaternion.identity);
            _batch.Set(slot, Vector3.one, Vector3.forward, 0f);
            _batch.PostTick();
            _batch.Dispose();
            Assert.That(fighter.position, Is.EqualTo(Vector3.one));
            Assert.DoesNotThrow(() => _batch.Unregister(slot));
            Assert.DoesNotThrow(() => _batch.Dispose());
            Assert.Throws<System.ObjectDisposedException>(() => _batch.Register(fighter, body, Quaternion.identity));
        }

        [Test]
        public void Lifecycle_CompletesBeforeCombatAndBeforeEntityTeardown()
        {
            LifecycleOrderAttribute order = typeof(FighterPoseBatch).GetCustomAttribute<LifecycleOrderAttribute>();
            Assert.That(order.LateTick, Is.LessThan(LifecycleOrders.COMBAT_LATE_TICK));
            Assert.That(order.Dispose, Is.GreaterThan(LifecycleOrders.ENTITY_SCOPES_DISPOSE));
            Assert.That(_batch, Is.InstanceOf<IPostTickable>());
        }

        private (Transform Fighter, Transform Body) Fighter()
        {
            GameObject fighter = new GameObject("Fighter");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(fighter.transform, false);
            _objects.Add(fighter);
            return (fighter.transform, body.transform);
        }
    }
}
