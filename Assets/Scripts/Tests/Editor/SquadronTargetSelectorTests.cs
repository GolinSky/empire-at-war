using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SquadronTargetSelectorTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<IEntity> _candidates = new List<IEntity>();
        private EntityLocator _locator;
        private VisionSpy _vision;

        [SetUp]
        public void SetUp()
        {
            _locator = new EntityLocator();
            _vision = new VisionSpy();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects) UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
            _candidates.Clear();
        }

        [TestCase(false, ShipClass.Fighter)]
        [TestCase(true, ShipClass.Fighter)]
        [TestCase(false, ShipClass.Bomber)]
        [TestCase(true, ShipClass.Bomber)]
        public void Selection_PreservesClassPreferenceAndFirstTie(bool seen, ShipClass squadronClass)
        {
            ShipClass preferred = squadronClass == ShipClass.Bomber ? ShipClass.Capital : ShipClass.Fighter;
            ShipClass other = squadronClass == ShipClass.Bomber ? ShipClass.Fighter : ShipClass.Capital;
            AddTarget(6f, other);
            IEntity expected = AddTarget(10f, preferred);
            AddTarget(10f, preferred);

            Assert.That(Select(CreateSelector(squadronClass), seen), Is.SameAs(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Selection_RejectsWorseAndDistantCandidatesBeforeCloakOrVision(bool seen)
        {
            IEntity expected = AddTarget(3f);
            IEntity worse = AddTarget(20f);
            IEntity distant = AddTarget(200f);
            IEntity dead = AddTarget(1f);
            ((HealthStub)dead.HealthModel).IsDestroyed = true;
            IEntity ally = AddTarget(1f, owner: TestPlayers.Human);

            Assert.That(Select(CreateSelector(), seen), Is.SameAs(expected));
            Assert.That(Combat(expected).Reads, Is.EqualTo(1));
            foreach (IEntity rejected in new[] { worse, distant, dead, ally })
                Assert.That(Combat(rejected).Reads, Is.Zero);
            Assert.That(_vision.Reads, Is.EqualTo(seen ? 1 : 0));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SelectSeen_RejectsCloakedTargetsAndAppliesFogOnlyForLocalSquadrons(bool local)
        {
            IEntity cloaked = AddTarget(1f);
            Combat(cloaked).Value.SetCloaked(true);
            IEntity hidden = AddTarget(2f);
            _vision.Hidden.Add(hidden.GetFacade<IEntityTransformFacade>().Transform.position);
            IEntity visible = AddTarget(3f);
            PlayerId side = local ? TestPlayers.Human : TestPlayers.Ally;

            Assert.That(CreateSelector(side: side).SelectSeen(Vector3.zero, 100f), Is.SameAs(local ? visible : hidden));
            Assert.That(_vision.Reads, Is.EqualTo(local ? 2 : 0));
        }

        [Test]
        public void SelectNear_RechecksCloakAndHealthOnNextSearch()
        {
            IEntity first = AddTarget(1f);
            IEntity second = AddTarget(2f);
            SquadronTargetSelector selector = CreateSelector();
            Assert.That(Select(selector, false), Is.SameAs(first));
            Combat(first).Value.SetCloaked(true);
            Assert.That(Select(selector, false), Is.SameAs(second));
            ((HealthStub)second.HealthModel).IsDestroyed = true;
            Assert.That(Select(selector, false), Is.Null);
            Combat(first).Value.SetCloaked(false);
            Assert.That(Select(selector, false), Is.SameAs(first));
        }

        private IEntity Select(SquadronTargetSelector selector, bool seen) => seen
            ? selector.SelectSeen(Vector3.zero, 100f)
            : selector.SelectNear(_candidates, Vector3.zero, 100f);

        private SquadronTargetSelector CreateSelector(ShipClass shipClass = ShipClass.Fighter, PlayerId? side = null)
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            return new SquadronTargetSelector(_locator, _vision, roster, TestPlayers.CreateLocalPlayer(roster),
                side ?? TestPlayers.Human, new SquadronHealthData { ShipClass = shipClass });
        }

        private IEntity AddTarget(float distance, ShipClass shipClass = ShipClass.Fighter, PlayerId? owner = null)
        {
            GameObject item = new GameObject("Target");
            _objects.Add(item);
            item.transform.position = Vector3.forward * distance;
            Entity entity = new Entity(new HealthStub { ShipClass = shipClass }, _locator,
                new IEntityFacade[] { new EntityTransformFacade(item.transform), new CombatSpy() },
                owner ?? TestPlayers.Enemy, _candidates.Count + 1);
            _locator.AddEntity(entity);
            _candidates.Add(entity);
            return entity;
        }

        private static CombatSpy Combat(IEntity entity) => (CombatSpy)entity.GetFacade<ICombatModifiersFacade>();

        private sealed class CombatSpy : ICombatModifiersFacade
        {
            public CombatModifiers Value { get; } = new CombatModifiers();
            public int Reads { get; private set; }
            public CombatModifiers Modifiers { get { Reads++; return Value; } }
        }

        private sealed class VisionSpy : IVisionService
        {
            public HashSet<Vector3> Hidden { get; } = new HashSet<Vector3>();
            public int Reads { get; private set; }
            public bool IsVisible(PlayerId viewer, Vector3 position) { Reads++; return !Hidden.Contains(position); }
            public IReadOnlyList<OwnedCircle> Sources => throw new NotSupportedException();
            public void Register(PlayerId owner, Transform source, float radius) => throw new NotSupportedException();
            public void Unregister(Transform source) => throw new NotSupportedException();
            public bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius) => throw new NotSupportedException();
        }

        private sealed class SquadronHealthData : ISquadronHealthData
        {
            public ShipClass ShipClass { get; set; }
            public float MemberHull => 1f;
            public float MemberShields => 0f;
            public float ShieldRegenerateValue => 0f;
            public float ShieldRegenerateDelay => 0f;
            public float LaserShieldDamageMultiplier => 1f;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass { get; set; }
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public bool HasLiveHardPoints => !IsDestroyed;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Enemy;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
