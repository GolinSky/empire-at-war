using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class TargetOriginCaptureTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private TargetOriginCapture _capture;

        [SetUp]
        public void SetUp() => _capture = new TargetOriginCapture();

        [TearDown]
        public void TearDown()
        {
            _capture.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Read_UsesWorldPositionAndParentRotationWithRotatedMount(bool nonUniformScale)
        {
            Transform root = CreateObject("Root").transform;
            root.SetPositionAndRotation(new Vector3(11f, -7f, 23f), Quaternion.Euler(17f, 128f, -23f));
            root.localScale = nonUniformScale ? new Vector3(2f, 0.7f, 1.3f) : Vector3.one * 1.5f;
            Transform parent = CreateObject("Parent").transform;
            parent.SetParent(root, false);
            parent.localPosition = new Vector3(-3f, 5f, 2f);
            parent.localRotation = Quaternion.Euler(-31f, 42f, 9f);
            WeaponHardPoint mount = CreateMount("Mount", parent);
            mount.transform.localPosition = new Vector3(1f, 2f, -4f);
            mount.transform.localRotation = Quaternion.Euler(21f, -64f, 15f);
            _capture.Register(new Owner(), mount);

            ReadAndAssert(mount, parent.rotation);
            root.Rotate(new Vector3(-12f, 37f, 8f), Space.World);
            parent.localPosition += Vector3.right;
            mount.transform.localRotation = Quaternion.Euler(-13f, 89f, 42f);
            ReadAndAssert(mount, parent.rotation);
        }

        [Test]
        public void Read_UnparentedRotatedMountHasIdentityParent()
        {
            WeaponHardPoint mount = CreateMount("Unparented", null);
            mount.transform.SetPositionAndRotation(new Vector3(-9f, 6f, 12f), Quaternion.Euler(23f, 147f, -35f));
            _capture.Register(new Owner(), mount);

            ReadAndAssert(mount, Quaternion.identity);
        }

        [Test]
        public void Unregister_SwapsSlotsAndAllowsPooledMountsToRegisterAgain()
        {
            Owner released = new Owner();
            Owner remaining = new Owner();
            WeaponHardPoint first = CreateMount("First", null);
            WeaponHardPoint second = CreateMount("Second", null);
            WeaponHardPoint third = CreateMount("Third", null);
            first.transform.position = Vector3.right;
            second.transform.position = Vector3.up;
            third.transform.position = Vector3.forward;
            _capture.Register(released, first);
            _capture.Register(released, second);
            _capture.Register(remaining, third);
            ReadAndAssert(third, Quaternion.identity);

            _capture.Unregister(released);
            Assert.That(_capture.GetSlot(third), Is.Zero);
            Assert.Throws<KeyNotFoundException>(() => _capture.GetSlot(first));
            Assert.Throws<KeyNotFoundException>(() => _capture.GetSlot(second));
            ReadAndAssert(third, Quaternion.identity);

            first.gameObject.SetActive(false);
            first.gameObject.SetActive(true);
            first.transform.position = new Vector3(8f, -2f, 19f);
            _capture.Register(released, first);
            _capture.Register(released, first);
            _capture.Register(released, second);
            Assert.That(_capture.GetSlot(first), Is.EqualTo(1));
            Assert.That(_capture.GetSlot(second), Is.EqualTo(2), "Duplicate registration must not add another slot.");
            ReadAndAssert(first, Quaternion.identity);
            ReadAndAssert(second, Quaternion.identity);
            ReadAndAssert(third, Quaternion.identity);

            _capture.Unregister(remaining);
            _capture.Unregister(released);
            _capture.BeginRead();
            _capture.CompleteRead();
            _capture.Register(remaining, third);
            Assert.That(_capture.GetSlot(third), Is.Zero);
            ReadAndAssert(third, Quaternion.identity);
        }

        private void ReadAndAssert(WeaponHardPoint mount, Quaternion expectedParent)
        {
            _capture.BeginRead();
            _capture.CompleteRead();
            int slot = _capture.GetSlot(mount);
            Vector3 position = (Vector3)_capture.Positions[slot];
            quaternion rotation = _capture.ParentRotations[slot];
            Quaternion parent = new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
            Assert.That(Vector3.Distance(position, mount.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(parent, expectedParent), Is.LessThan(0.05f));
        }

        private WeaponHardPoint CreateMount(string name, Transform parent)
        {
            GameObject item = CreateObject(name);
            item.transform.SetParent(parent, false);
            return item.AddComponent<WeaponHardPoint>();
        }

        private GameObject CreateObject(string name)
        {
            GameObject item = new GameObject(name);
            _objects.Add(item);
            return item;
        }

        private sealed class Owner : IWeaponPresenter
        {
            public float GetHitChance(AttackData attackData, WeaponProfile profile) => throw new NotSupportedException();
            public void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponProfile profile,
                float attackDelay, float damageDuration, IncomingMissile missile) => throw new NotSupportedException();
            public bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel, float damage,
                DamageType damageType, int targetId) => throw new NotSupportedException();
        }
    }
}
