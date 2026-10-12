using System.Collections.Generic;
using EmpireAtWar.ViewComponents.Squadrons;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
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
