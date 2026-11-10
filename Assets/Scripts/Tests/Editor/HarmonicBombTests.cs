using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class HarmonicBombTests
    {
        private readonly EntityLocator _entities = new EntityLocator();
        private HarmonicBombService _bombs;
        private GameObject _poolRoot;

        [Test]
        public void DetonationKillsEveryHostileNonHeroSquadronWithAMemberInsideTheRadius()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            VisionService vision = new VisionService(roster, false);
            DetonationAudio audio = new DetonationAudio();
            _bombs = CreateService(roster, vision, audio);
            ShipAbilityDefinition definition = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(
                "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.HarmonicBomb);
            var settings = (HarmonicBombSettings)definition.Settings;
            float radius = settings.Radius;
            Wing center = AddWing(TestPlayers.Enemy, 0f);
            Wing edge = AddWing(TestPlayers.Enemy, radius * 1.5f, radius);
            Wing outside = AddWing(TestPlayers.Enemy, radius * 1.01f, radius * 3f);
            Wing movedIn = AddWing(TestPlayers.Enemy, radius * 3f);
            Wing hero = AddWing(TestPlayers.Enemy, SquadronType.BobaFettSlaveI, 0f);
            Wing ally = AddWing(TestPlayers.Ally, 0f);
            Wing own = AddWing(TestPlayers.Human, 0f);
            Vector3 emptyDrop = new Vector3(500f, 4f, -250f);
            _bombs.Drop(TestPlayers.Enemy, emptyDrop, definition.Duration, radius, settings.ViewPrefab);
            for (int i = 0; i < 2; i++)
                _bombs.Drop(TestPlayers.Human, Vector3.zero, definition.Duration, radius, settings.ViewPrefab);
            _poolRoot = GameObject.Find("Harmonic Bombs");

            _bombs.Advance(1f);
            Assert.That(center.Kills, Is.Zero);
            Assert.That(audio.Cues, Is.Empty);
            movedIn.Units[0].Position = Vector3.zero;
            _bombs.Advance(0.5f);
            _bombs.Advance(5f);
            Assert.That(center.Kills, Is.EqualTo(2));
            Assert.That(edge.Kills, Is.EqualTo(2));
            Assert.That(movedIn.Kills, Is.EqualTo(2));
            Assert.That(outside.Kills, Is.Zero);
            Assert.That(hero.Kills, Is.Zero);
            Assert.That(ally.Kills, Is.Zero);
            Assert.That(own.Kills, Is.Zero);
            Assert.That(audio.Cues, Is.EquivalentTo(new[] { (TestPlayers.Enemy, emptyDrop),
                (TestPlayers.Human, Vector3.zero), (TestPlayers.Human, Vector3.zero) }));
        }

        [Test]
        public void AutomaticDropCountsOnlyVisibleLiveHostileMembersInsideTheRadius()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            VisionService vision = new VisionService(roster);
            _bombs = CreateService(roster, vision, new DetonationAudio());
            Wing enemy = AddWing(TestPlayers.Enemy, 1f, 15f, 35f, 36f);
            AddWing(TestPlayers.Ally, 0f, 0f, 0f);
            GameObject radar = new GameObject("Vision source");
            try
            {
                Assert.That(Enough(), Is.False);
                vision.Register(TestPlayers.Human, radar.transform, 40f);
                Assert.That(Enough(), Is.True);
                enemy.Units[0].IsDestroyed = true;
                Assert.That(Enough(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(radar);
            }
        }

        private bool Enough() => _bombs.HasEnoughTargets(TestPlayers.Human, Vector3.zero, 35f, 3);

        private HarmonicBombService CreateService(PlayerRoster roster, VisionService vision, DetonationAudio audio) =>
            new HarmonicBombService(_entities, roster, vision,
                new HarmonicBombEffects(vision, TestPlayers.CreateLocalPlayer(roster), audio),
                AssetDatabase.LoadAssetAtPath<FactionCatalog>("Assets/Settings/Data/Factions/Shared/FactionCatalog.asset"));

        private sealed class DetonationAudio : IWorldSfxService
        {
            public readonly List<(PlayerId, Vector3)> Cues = new();

            public void StopOneShots(SfxProfile sound) { }

            public bool TryPlayOneShot(PlayerId owner, SfxProfile sound, Vector3 position)
            {
                Assert.That(sound.GetClip().name, Is.EqualTo("EXP_SIZEMIC_CHARGE_2"));
                Cues.Add((owner, position));
                return true;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_poolRoot != null) UnityEngine.Object.DestroyImmediate(_poolRoot);
            foreach (IEntity entity in new List<IEntity>(_entities.Entities)) _entities.RemoveEntity(entity);
        }

        private Wing AddWing(PlayerId owner, params float[] distances) =>
            AddWing(owner, SquadronType.TIEFighter, distances);

        private Wing AddWing(PlayerId owner, SquadronType type, params float[] distances)
        {
            Wing wing = new Wing(owner, distances);
            _entities.AddEntity(new Entity(wing, _entities,
                new IEntityFacade[] { wing, new UnitTypeFacade(UnitTypeId.Squadron(type)) }, owner,
                _entities.Entities.Count + 1, Array.Empty<IPooledEntityEvents>(),
                Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>()));
            return wing;
        }

        private sealed class Member : IHardPointModel
        {
            public int Id { get; set; }
            public Vector3 Position { get; set; }
            public bool IsDestroyed { get; set; }
            public int Generation => 0;
            public HardPointType HardPointType => HardPointType.Any;
            public float HealthPercentage => IsDestroyed ? 0f : 1f;
            public Transform Transform => null;
            public Transform Pivot => null;
            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
        }

        private sealed class Wing : IHealthModelObserver, ISquadronMembersFacade
        {
            public int Kills { get; private set; }
            public Member[] Units { get; }
            public IReadOnlyList<IHardPointModel> Members => Units;
            public PlayerId Owner { get; }

            public Wing(PlayerId owner, float[] distances)
            {
                Owner = owner;
                Units = new Member[distances.Length];
                for (int i = 0; i < Units.Length; i++)
                    Units[i] = new Member { Id = i, Position = Vector3.right * distances[i] };
            }

            public void Kill() => Kills++;

            public ShipClass ShipClass => ShipClass.Fighter;
            public bool IsDestroyed => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public bool IsLostShieldGenerator => false;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public IHardPointModel[] GetShipUnits(HardPointType type) => Units;
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
        }
    }
}
