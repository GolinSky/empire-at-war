using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FighterViewTests
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
        public void Injection_CapturesAuthoredRestBeforeAnInactiveSquadronPosesItsFighters()
        {
            Quaternion rest = Quaternion.Euler(10f, 90f, 0f);
            (FighterView view, Transform body) = InactiveFighter(rest);
            Quaternion registeredRest = default;
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<IFighterExplosionPool>(new SilentExplosions());
            builder.RegisterComponent(view);
            // Entity scopes dispatch Initialize from a later build callback, while the entity is still inactive.
            builder.RegisterBuildCallback(_ =>
            {
                view.ApplyPose(Vector3.zero, Vector3.forward, 30f);
                registeredRest = view.BodyRestRotation;
            });

            using (builder.Build())
            {
                Assert.That(view.gameObject.activeSelf, Is.False);
                AssertRotation(registeredRest, rest);
                AssertRotation(body.localRotation, Quaternion.AngleAxis(30f, Vector3.forward) * rest);

                view.gameObject.SetActive(true);
                int slot = _batch.Register(view.transform, view.Body, view.BodyRestRotation);
                _batch.Set(slot, Vector3.one, Vector3.right, -45f);
                _batch.LateTick();

                AssertRotation(view.BodyRestRotation, rest);
                AssertRotation(body.localRotation, Quaternion.AngleAxis(-45f, Vector3.forward) * rest);
            }
        }

        [Test]
        public void Restore_DeadFighterDisablesItsViewWithoutExplosion_AndNextDeathExplodes()
        {
            (FighterView view, Transform body) = InactiveFighter(Quaternion.identity);
            Collider hitCollider = view.gameObject.AddComponent<BoxCollider>();
            WeaponHardPoint gun = body.gameObject.AddComponent<WeaponHardPoint>();
            typeof(HardPoint).GetField("spawnDestroyedExplosion", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(gun, false);
            TrailRenderer trail = body.gameObject.AddComponent<TrailRenderer>();
            view.SetEditorReferences(1, body, hitCollider, gun, new[] { trail });
            SilentExplosions explosions = new SilentExplosions();
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<IFighterExplosionPool>(explosions);
            builder.RegisterComponent(view);
            HardPointModel model = new HardPointModel(HardPointType.Any, 1);
            model.SetHealth(100f, 1f);

            using (builder.Build())
            using (new HardPointAdapter(view, model, view.transform))
            {
                model.Restore(0f);

                Assert.That(view.IsDestroyed, Is.True);
                Assert.That(body.gameObject.activeSelf, Is.False);
                Assert.That(hitCollider.enabled, Is.False);
                Assert.That(gun.IsDestroyed, Is.True);
                Assert.That(trail.emitting, Is.False);
                Assert.That(explosions.PlayCount, Is.Zero);

                model.SetHealth(100f, 1f);

                Assert.That(body.gameObject.activeSelf, Is.True);
                Assert.That(hitCollider.enabled, Is.True);
                Assert.That(gun.IsDestroyed, Is.False);
                Assert.That(trail.emitting, Is.True);
                model.ApplyDamage(100f);
                Assert.That(explosions.PlayCount, Is.EqualTo(1));
            }
        }

        private (FighterView View, Transform Body) InactiveFighter(Quaternion rest)
        {
            GameObject fighter = new GameObject("Fighter");
            fighter.SetActive(false);
            _objects.Add(fighter);
            GameObject body = new GameObject("Body");
            body.transform.SetParent(fighter.transform, false);
            body.transform.localRotation = rest;
            FighterView view = fighter.AddComponent<FighterView>();
            view.SetEditorReferences(1, body.transform, null, null, System.Array.Empty<TrailRenderer>());
            return (view, body.transform);
        }

        private static void AssertRotation(Quaternion actual, Quaternion expected) =>
            Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(0.01f), $"{actual} != {expected}");

        private sealed class SilentExplosions : IFighterExplosionPool
        {
            public int PlayCount { get; private set; }
            public void Play(Vector3 position) => PlayCount++;
        }
    }
}
