using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class WeaponCandidateTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private CombatAttackCoordinator _coordinator;
        private WeaponComponent _weapon;

        [SetUp]
        public void SetUp()
        {
            _coordinator = new CombatAttackCoordinator();
            GameObject weaponObject = Track(new GameObject("Weapon"));
            _weapon = weaponObject.AddComponent<WeaponComponent>();
            typeof(WeaponComponent).GetField("_attackCoordinator", PRIVATE_INSTANCE).SetValue(_weapon, _coordinator);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _objects)
                if (created != null) Object.DestroyImmediate(created);
            _objects.Clear();
            _coordinator.Dispose();
        }

        [Test]
        public void AddBaseTarget_SplicesNewestContactAfterMainLikeARebuild()
        {
            AttackData first = Group(2);
            AttackData main = Group(2);
            AttackData newest = Group(3);
            _weapon.AddTarget(first, AttackType.Base);
            _weapon.AddTarget(main, AttackType.MainTarget);
            Invoke("EnsureCandidates");

            _weapon.AddTarget(newest, AttackType.Base);
            List<(AttackData, IHardPointModel)> spliced = Candidates();
            Invoke("RebuildCandidates");

            Assert.That(spliced, Is.EqualTo(Candidates()));
            Assert.That(spliced[2].Item1, Is.SameAs(newest));
        }

        [Test]
        public void TargetDestroyed_RebuildsOnlyOnceHalfTheGroupsAreDead()
        {
            HealthStub[] healths = new HealthStub[4];
            for (int i = 0; i < healths.Length; i++)
            {
                healths[i] = new HealthStub(Units(1));
                _weapon.AddTarget(new AttackData(healths[i], null, HardPointType.Any, null), AttackType.Base);
            }

            Assert.That(IsDirty(), Is.False);
            healths[0].Destroy();
            Assert.That(IsDirty(), Is.False);
            healths[1].Destroy();
            Assert.That(IsDirty(), Is.True);
        }

        private AttackData Group(int units) =>
            new AttackData(new HealthStub(Units(units)), null, HardPointType.Any, null);

        private IHardPointModel[] Units(int count)
        {
            IHardPointModel[] units = new IHardPointModel[count];
            for (int i = 0; i < count; i++)
                units[i] = new HardPointStub(Track(new GameObject("Unit")).transform);
            return units;
        }

        private List<(AttackData, IHardPointModel)> Candidates()
        {
            int count = (int)Field("_candidateCount");
            int[] slots = (int[])Field("_candidateSlots");
            int[] groupIndices = (int[])Field("_candidateGroupIndices");
            List<AttackData> groups = (List<AttackData>)Field("_candidateGroups");
            List<(AttackData, IHardPointModel)> candidates = new List<(AttackData, IHardPointModel)>();
            for (int i = 0; i < count; i++)
                candidates.Add((groups[groupIndices[i]], _coordinator.TargetUnits.GetUnit(slots[i])));
            return candidates;
        }

        private bool IsDirty() => (bool)Field("_candidatesDirty");

        private object Field(string name) => typeof(WeaponComponent).GetField(name, PRIVATE_INSTANCE).GetValue(_weapon);

        private void Invoke(string method) =>
            typeof(WeaponComponent).GetMethod(method, PRIVATE_INSTANCE).Invoke(_weapon, null);

        private GameObject Track(GameObject created)
        {
            _objects.Add(created);
            return created;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            private readonly IHardPointModel[] _units;

            public HealthStub(IHardPointModel[] units) => _units = units;

            public event Action OnDestroy;
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; private set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => _units;

            public void Destroy()
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }
        }

        private sealed class HardPointStub : IHardPointModel
        {
            public HardPointStub(Transform transform) => Transform = transform;

            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
            public HardPointType HardPointType => HardPointType.Weapon;
            public float HealthPercentage => 1f;
            public int Id => 0;
            public int Generation => 0;
            public bool IsDestroyed => false;
            public Vector3 Position => Transform.position;
            public Transform Transform { get; }
            public Transform Pivot => Transform;
        }
    }
}
