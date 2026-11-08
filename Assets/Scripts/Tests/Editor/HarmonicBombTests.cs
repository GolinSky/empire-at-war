using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
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
        public void OverlappingDropsSampleLiveMembersAtDetonationAndHitEachOnce()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            VisionService vision = new VisionService(roster, false);
            DetonationAudio audio = new DetonationAudio();
            _bombs = new HarmonicBombService(_entities, roster, vision,
                new HarmonicBombEffects(vision, TestPlayers.CreateLocalPlayer(roster), audio));
            ShipAbilityDefinition definition = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(
                "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.HarmonicBomb);
            var settings = (HarmonicBombSettings)definition.Settings;
            // Distances scale with the authored radius: inner band, middle band, edge, just outside, far outside.
            float radius = settings.Radius;
            Wing enemy = AddWing(TestPlayers.Enemy, 0f, radius * 12f / 35f, radius, radius * 35.1f / 35f,
                radius * 100f / 35f);
            Wing ally = AddWing(TestPlayers.Ally, 0f);
            Wing own = AddWing(TestPlayers.Human, 0f);
            Vector3 emptyDrop = new Vector3(500f, 4f, -250f);
            _bombs.Drop(TestPlayers.Enemy, emptyDrop, definition.Duration, settings.Radius,
                settings.CenterDamage, settings.ViewPrefab);
            for (int i = 0; i < 2; i++)
                _bombs.Drop(TestPlayers.Human, Vector3.zero, definition.Duration, settings.Radius,
                    settings.CenterDamage, settings.ViewPrefab);
            _poolRoot = GameObject.Find("Harmonic Bombs");

            _bombs.Advance(1f);
            Assert.That(enemy.Hits, Is.Empty);
            Assert.That(audio.Cues, Is.Empty);
            enemy.Units[4].Position = Vector3.up * 200f;
            _bombs.Advance(0.5f);
            _bombs.Advance(5f);
            float full = settings.CenterDamage;
            float middle = settings.CenterDamage * 0.6f;
            float edge = settings.CenterDamage * 0.25f;
            Assert.That(enemy.Hits, Is.EqualTo(new[] { (0, full), (1, middle), (2, edge), (4, full),
                (0, full), (1, middle), (2, edge), (4, full) }));
            Assert.That(ally.Hits, Is.Empty);
            Assert.That(own.Hits, Is.Empty);
            Assert.That(audio.Cues, Is.EquivalentTo(new[] { (TestPlayers.Enemy, emptyDrop),
                (TestPlayers.Human, Vector3.zero), (TestPlayers.Human, Vector3.zero) }));
        }

        [Test]
        public void AutomaticDropCountsOnlyVisibleLiveHostileMembersInsideTheRadius()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            VisionService vision = new VisionService(roster);
            _bombs = new HarmonicBombService(_entities, roster, vision,
                new HarmonicBombEffects(vision, TestPlayers.CreateLocalPlayer(roster), new DetonationAudio()));
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

        private Wing AddWing(PlayerId owner, params float[] distances)
        {
            Wing wing = new Wing(owner, distances);
            _entities.AddEntity(new Entity(wing, _entities, new IEntityFacade[] { wing }, owner,
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

        private sealed class Wing : IHealthModelObserver, ISquadronMembersFacade, IHealthFacade
        {
            public readonly List<(int, float)> Hits = new();
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

            public void ApplyDamage(float damage, DamageType type, int id)
            {
                Assert.That(type, Is.EqualTo(DamageType.HypervelocityGun));
                Hits.Add((id, damage));
            }

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
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public IHardPointModel[] GetShipUnits(HardPointType type) => Units;
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
        }
    }
}
