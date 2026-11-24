using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.UnitExplosion;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class UnitMuzzleBlastsTests
    {
        private FireEvents _events;
        private Pool _pool;
        private Fog _fog;
        private UnitMuzzleBlasts _blasts;
        private Transform _muzzle;

        [SetUp]
        public void SetUp()
        {
            _events = new FireEvents();
            _pool = new Pool();
            _fog = new Fog();
            _blasts = new UnitMuzzleBlasts(_events, _pool, _fog);
            _muzzle = new GameObject("Muzzle").transform;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_muzzle.gameObject);

        [Test]
        public void MainBatterySalvo_PlaysOneBlastAtTheMuzzle()
        {
            _blasts.Initialize();

            _events.RaiseSalvoStarted(Profile(new Vector3(2f, 1f, 1f)), _muzzle);

            Assert.That(_pool.Played, Has.Count.EqualTo(1));
            Assert.That(_pool.Played[0].Muzzle, Is.SameAs(_muzzle));
            Assert.That(_pool.Played[0].Fog, Is.SameAs(_fog));
        }

        [Test]
        public void Offset_IsAboveTheMountAndTowardsTheAim()
        {
            _muzzle.rotation = Quaternion.Euler(0f, 0f, 90f);
            Vector3 aimDirection = Vector3.forward;
            _blasts.Initialize();

            _events.RaiseSalvoStarted(Profile(Vector3.one), _muzzle, _muzzle.position + aimDirection * 100f);

            Vector3 offset = _pool.Played[0].Offset;
            float diameter = _pool.Played[0].Diameter;
            Assert.That(Vector3.Dot(offset, _muzzle.up), Is.GreaterThan(0f), "Above the mount, along its up axis.");
            Assert.That(Vector3.Dot(offset, aimDirection), Is.GreaterThan(0f), "Pushed towards the aim point.");
            Assert.That(offset.magnitude, Is.LessThan(diameter), "A small offset, inside the blast.");
        }

        [Test]
        public void BlastSize_FollowsShotSize()
        {
            _blasts.Initialize();

            _events.RaiseSalvoStarted(Profile(new Vector3(1f, 1f, 1f)), _muzzle);
            _events.RaiseSalvoStarted(Profile(new Vector3(3f, 1f, 1f)), _muzzle);

            Assert.That(_pool.Played[0].Diameter, Is.GreaterThan(0f));
            Assert.That(_pool.Played[1].Diameter, Is.EqualTo(_pool.Played[0].Diameter * 3f).Within(1e-4f));
        }

        [Test]
        public void HiddenByFog_PlaysNoBlast()
        {
            _fog.IsHiddenByFog = true;
            _blasts.Initialize();

            _events.RaiseSalvoStarted(Profile(Vector3.one), _muzzle);

            Assert.That(_pool.Played, Is.Empty);
        }

        [Test]
        public void LateDispose_StopsListening()
        {
            _blasts.Initialize();
            _blasts.LateDispose();

            _events.RaiseSalvoStarted(Profile(Vector3.one), _muzzle);

            Assert.That(_pool.Played, Is.Empty);
        }

        private static WeaponProfile Profile(Vector3 size) => new WeaponProfile(WeaponType.Laser, "Test",
            WeaponFamily.Laser, DamageType.Laser, 1f, 1, 1f, 1f, 100f, false, false, null, 100f, Color.white, size,
            false, false, default, default, 0f);

        private sealed class FireEvents : IWeaponFireEvents
        {
            public event Action<WeaponProfile, Transform, float> ShotEmitted { add { } remove { } }

            public event Action<WeaponProfile, Transform, Vector3> MainBatterySalvoStarted;

            public void RaiseSalvoStarted(WeaponProfile profile, Transform muzzle) =>
                RaiseSalvoStarted(profile, muzzle, muzzle.position + Vector3.forward);

            public void RaiseSalvoStarted(WeaponProfile profile, Transform muzzle, Vector3 aimPoint) =>
                MainBatterySalvoStarted?.Invoke(profile, muzzle, aimPoint);
        }

        private sealed class Pool : IMuzzleBlastPool
        {
            public List<(Transform Muzzle, Vector3 Offset, IFogVisibilityFacade Fog, float Diameter)> Played { get; } =
                new List<(Transform, Vector3, IFogVisibilityFacade, float)>();

            public void Play(Transform muzzle, Vector3 worldOffset, IFogVisibilityFacade fogVisibility, float diameter) =>
                Played.Add((muzzle, worldOffset, fogVisibility, diameter));
        }

        private sealed class Fog : IFogVisibilityFacade
        {
            public event Action<bool> VisibilityChanged { add { } remove { } }

            public bool IsHiddenByFog { get; set; }
        }
    }
}
