using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Components.Hangar;
using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Weapon;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Vision;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    [Category("BattleInput")]
    public sealed class ShipAbilityServiceTests
    {
        private GameObject _targetView;

        [TestCase(true, 8f)]
        [TestCase(true, 20f)]
        [TestCase(false, 8f)]
        public void ConcentrateFire_AuthorityPreservesOrdersOnlyForAutomaticCasts(bool automatic, float orderedDistance)
        {
            _targetView = new GameObject("Ordered target");
            GameObject nearbyView = new GameObject("Nearby target");
            ShipAbilityCatalog catalog = ScriptableObject.CreateInstance<ShipAbilityCatalog>();
            try
            {
                _targetView.transform.position = Vector3.right * orderedDistance;
                nearbyView.transform.position = Vector3.right;
                var ordered = new FakeEntity(null, new FakeHealth(_targetView.transform), TestPlayers.Enemy, 2);
                var nearby = new FakeEntity(null, new FakeHealth(nearbyView.transform), TestPlayers.Enemy, 3);
                var settings = new ConcentrateFireSettings();
                var caster = CreateCaster(targeted: true);
                var ally = new FakeCommand(new ShipAbilityDefinition(), entityId: 4);
                var idle = new FakeCommand(new ShipAbilityDefinition(), entityId: 5);
                var definition = caster.Slots[0].Definition;
                SetField(definition, "settings", settings);
                caster.Attack(ordered, Vector3.zero, null);
                ally.Attack(automatic ? nearby : ordered, Vector3.zero, null);
                var entities = new EntityLocator();
                entities.AddEntity(caster.Entity);
                entities.AddEntity(ordered);
                entities.AddEntity(nearby);
                entities.AddEntity(ally.Entity);
                entities.AddEntity(idle.Entity);
                var roster = TestPlayers.CreateDuel();
                var service = new ShipAbilityService(new SingleAbilityFactory(new ConcentrateFireAbility(entities, settings)),
                    roster, new VisionService(roster, fogEnabled: false));
                var combat = new AutoPowerCombatSnapshot(entities, null, service, null, null, catalog);
                Assert.That(combat.FindTarget(caster, definition), Is.SameAs(orderedDistance <= 10f ? ordered : null));

                service.Execute(new[] { caster.Entity }, caster.Slots[0].Id, nearby, cancel: false, automatic: automatic);

                bool cast = !automatic || orderedDistance <= 10f;
                Assert.That(caster.Slots[0].State, Is.EqualTo(cast ? ShipAbilityState.Active : ShipAbilityState.Ready));
                Assert.That(caster.AttackTarget, Is.SameAs(automatic ? ordered : nearby));
                Assert.That(caster.AttackCount, Is.EqualTo(automatic ? 1 : 2));
                Assert.That(ally.AttackTarget, Is.SameAs(nearby));
                Assert.That(ally.AttackCount, Is.EqualTo(automatic ? 1 : 2));
                Assert.That(idle.AttackTarget, Is.SameAs(cast ? automatic ? ordered : nearby : null));
                service.Clear();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(nearbyView);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void HazardSuppression_StopsAbilityButCooldownStillProgresses()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            caster.Modifiers.AddAbilitySuppression();
            service.Advance(0.1f);
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));
            service.Advance(6f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.False);
            caster.Modifiers.RemoveAbilitySuppression();
            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);
            service.Clear();
        }

        [Test]
        public void EventCapture_AfterCasterLeftLocator_UsesStableCasterId()
        {
            FakeCommand caster = CreateCaster();
            caster.Despawned = true;
            ShipAbilityEventMessage message = default;
            Assert.DoesNotThrow(() => message = ShipAbilityEventMessage.Capture(caster.Slots[0]));
            Assert.That(message.CasterId, Is.EqualTo(caster.EntityId));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void HazardSuppression_UsesItsOwnSurvivalPolicy(bool survivesDeath, bool survivesSuppression)
        {
            var ability = new RecordingAdvancingAbility
            {
                SurvivesCasterDeath = survivesDeath,
                SurvivesHazardSuppression = survivesSuppression
            };
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new SingleAbilityFactory(ability),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            caster.Modifiers.AddAbilitySuppression();
            service.Advance(0.1f);
            Assert.That(ability.AdvanceCount, Is.EqualTo(survivesSuppression ? 1 : 0));
            Assert.That(caster.Slots[0].State,
                Is.EqualTo(survivesSuppression ? ShipAbilityState.Active : ShipAbilityState.Recovering));
            service.Clear();
        }

        [Test]
        public void TargetingFeedback_RejectsInvalidTargetAndOnlyReportsExplicitCancel()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster(targeted: true);
            int rejected = 0;
            int canceled = 0;
            service.TargetRejected += casters =>
            {
                Assert.That(casters, Is.EqualTo(new[] { caster.Entity }));
                rejected++;
            };
            service.TargetingCanceled += casters =>
            {
                Assert.That(casters, Is.EqualTo(new[] { caster.Entity }));
                canceled++;
            };
            service.Press(new[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
            service.SubmitTarget(null);
            Assert.That(rejected, Is.EqualTo(1));
            Assert.That(service.IsWaitingForTarget, Is.True);
            service.CancelTargeting();
            service.CancelTargeting();
            Assert.That(canceled, Is.EqualTo(1));
            service.Press(new[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
            service.Clear();
            Assert.That(canceled, Is.EqualTo(1), "Teardown must not play a player cancellation cue.");
            service.Press(new[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
            service.PruneTargeting(Array.Empty<IEntity>());
            Assert.That(canceled, Is.EqualTo(1), "Losing the selection must not play a cancellation cue.");
        }

        [TestCase(false, 0.2f)]
        [TestCase(true, 0.2f)]
        [TestCase(false, 0.05f)]
        [TestCase(true, 0.05f)]
        public void BeamDamage_UsesAttackerBonusesAndStopsWhenTargetCloaks(bool composite, float step)
        {
            WeaponsData weapons = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var vision = new VisionService(TestPlayers.CreateDuel(), fogEnabled: false);
            var session = new OfflineSession(TestPlayers.Human);
            SustainedBeamAbility beam = composite
                ? new CompositeBeamAbility(new CompositeBeamSettings(), null, weapons, vision, session)
                : new ProtonBeamAbility(new ProtonBeamSettings(), null, weapons, vision, session);
            FakeCommand caster = CreateCaster();
            FakeCommand target = CreateCaster();
            caster.Modifiers.Add(new CombatStatModifier(2f, 1f, 1f, 1f, 1f));
            caster.Modifiers.SetFleetCommand(true);
            caster.Modifiers.SetFocusFire(target.Entity, 1.5f);
            // Exercise the running ability without spawning its visual presentation.
            SetBeamField(beam, "_caster", caster);
            SetBeamField(beam, "_target", target.Entity);
            SetBeamField(beam, "_health", target.FakeHealth);
            SetBeamField(beam, "_damagePerSecond", 100f);
            SetBeamField(beam, "_timeLeft", 1f);

            for (int i = 0; i < Mathf.CeilToInt(1f / step) + 1; i++) beam.Advance(step);

            Assert.That(target.FakeHealth.DamageReceived, Is.EqualTo(330f).Within(0.001f));
            SetBeamField(beam, "_timeLeft", 1f);
            Assert.That(beam.IsComplete, Is.False);
            target.Modifiers.SetCloaked(true);
            Assert.That(beam.IsComplete, Is.True);
        }

        [TestCase(false, true)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(true, false)]
        public void BeamDamage_OnlyAuthorityRefreshesFiringRevealUntilFiveSecondsAfterLastTick(
            bool composite, bool authority)
        {
            WeaponsData weapons = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            float now = 0f;
            var vision = new VisionService(TestPlayers.CreateDuel(), true, () => now);
            var session = new OnlineSession(authority, TestPlayers.Human);
            SustainedBeamAbility beam = composite
                ? new CompositeBeamAbility(new CompositeBeamSettings(), null, weapons, vision, session)
                : new ProtonBeamAbility(new ProtonBeamSettings(), null, weapons, vision, session);
            FakeCommand caster = CreateCaster();
            FakeCommand target = CreateCaster();
            var enemy = new FakeEntity(target, target.FakeHealth, TestPlayers.Enemy, 2);
            _targetView = new GameObject("Hidden beam caster");
            var footprint = new ShipVisionFootprint(_targetView.transform, caster.Entity.Id,
                new[] { Vector2.zero, Vector2.right, Vector2.up });
            SetBeamField(beam, "_caster", caster);
            SetBeamField(beam, "_target", enemy);
            SetBeamField(beam, "_health", target.FakeHealth);
            SetBeamField(beam, "_damagePerSecond", 100f);
            SetBeamField(beam, "_timeLeft", 1f);

            beam.Advance(0.2f);
            Assert.That(vision.IsVisible(TestPlayers.Enemy, footprint), Is.EqualTo(authority));
            Assert.That(vision.IsVisible(TestPlayers.Human, footprint), Is.False);
            now = 4f;
            beam.Advance(0.2f);
            now = 5.1f;
            vision.Tick();
            Assert.That(vision.IsVisible(TestPlayers.Enemy, footprint), Is.EqualTo(authority));
            now = 9.1f;
            vision.Tick();
            Assert.That(vision.IsVisible(TestPlayers.Enemy, footprint), Is.False);
        }

        [Test]
        public void IonShot_NoLiveMemberEmits_NoFiringReveal()
        {
            var vision = new VisionService(TestPlayers.CreateDuel());
            int revealCount = 0;
            vision.RevealChanged += (viewer, entity, active) => revealCount++;
            var ability = new IonShotAbility(new IonShotSettings(), null, vision,
                new OfflineSession(TestPlayers.Human));
            FakeCommand caster = CreateCaster();
            FakeCommand target = CreateCaster();
            target.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub() };

            ability.Start(caster, new ShipAbilityDefinition(), target.Entity);

            Assert.That(revealCount, Is.Zero);
        }

        [TestCase(ShipClass.HeavyCruiser, true)]
        [TestCase(ShipClass.Capital, true)]
        [TestCase(ShipClass.BattleCruiser, true)]
        [TestCase(ShipClass.Dreadnought, true)]
        [TestCase(ShipClass.Structure, true)]
        [TestCase(ShipClass.Fighter, false)]
        [TestCase(ShipClass.Bomber, false)]
        [TestCase(ShipClass.Interceptor, false)]
        [TestCase(ShipClass.HeroStrikecraft, false)]
        [TestCase(ShipClass.Corvette, true)]
        [TestCase(ShipClass.Frigate, true)]
        [TestCase(ShipClass.Cruiser, true)]
        public void LuckyShot_TargetsShipsAndStructures(ShipClass targetClass, bool allowed)
        {
            var settings = new LuckyShotSettings();
            var caster = CreateCaster();
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub() };
            var target = CreateCaster();
            target.FakeHealth.ShipClass = targetClass;
            Assert.That(settings.CanTarget(caster, target.Entity), Is.EqualTo(allowed));
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub { Id = 1 } };
            Assert.That(settings.CanTarget(caster, target.Entity), Is.False, "Wingmen cannot fire Luke's ability.");
            caster.FakeHealth.Units = Array.Empty<IHardPointModel>();
            Assert.That(settings.CanTarget(caster, target.Entity), Is.False);
        }

        [TestCase(ShipClass.Capital, ShipClass.HeavyCruiser, true)]
        [TestCase(ShipClass.Capital, ShipClass.Corvette, true)]
        [TestCase(ShipClass.Capital, ShipClass.Capital, false)]
        [TestCase(ShipClass.HeavyCruiser, ShipClass.Capital, false)]
        [TestCase(ShipClass.Capital, ShipClass.Fighter, false)]
        [TestCase(ShipClass.Capital, ShipClass.Structure, false)]
        public void TractorBeam_TargetsOnlyLowerClassShips(ShipClass casterClass, ShipClass targetClass, bool allowed)
        {
            var settings = new TractorBeamSettings();
            FakeCommand caster = CreateCaster();
            caster.FakeHealth.ShipClass = casterClass;
            FakeCommand target = CreateCaster();
            target.FakeHealth.ShipClass = targetClass;
            Assert.That(settings.CanTarget(caster, target.Entity), Is.EqualTo(allowed));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LuckyShot_WindsUpThenOnlyAuthorityHitsOnceAndRecovers(bool authority)
        {
            var relations = TestPlayers.CreateDuel();
            var vision = new VisionService(relations, fogEnabled: false);
            var entities = new EntityLocator();
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(
                "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
            ShipAbilityDefinition definition = catalog.Get(ShipAbilityId.LuckyShot);
            var settings = (LuckyShotSettings)definition.Settings;
            var matrix = UnityEditor.AssetDatabase.LoadAssetAtPath<DamageMatrixData>(
                "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
            var ability = new LuckyShotAbility(settings, entities, null,
                new ImpactEffectPresenter(null, matrix), vision,
                new OnlineSession(authority, TestPlayers.Human));
            var service = new ShipAbilityService(new SingleAbilityFactory(ability), relations, vision);
            var muzzle = new GameObject("LukeMuzzle");
            _targetView = new GameObject("LuckyShotTarget");
            _targetView.transform.position = Vector3.forward * 100f;
            var targetHealth = new FakeHealth(_targetView.transform) { ShipClass = ShipClass.Corvette };
            var target = new FakeEntity(null, targetHealth, TestPlayers.Enemy, 2);
            entities.AddEntity(target);
            var caster = new FakeCommand(definition, muzzle.transform, ShipAbilityId.LuckyShot);
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub { Transform = muzzle.transform } };
            ShotEffect shot = null;
            try
            {
                Assert.That(service.TryActivate(caster, ShipAbilityId.LuckyShot, target), Is.True);
                Assert.That(caster.AttackTarget, Is.EqualTo(authority ? target : null));
                service.Advance(settings.Windup * 0.5f);
                Assert.That(targetHealth.DamageReceived, Is.Zero);
                Assert.That(ability.SurvivesCasterDeath, Is.False);
                AbilitySlotSave windingUp = AbilitySlotSave.From(caster.Slots[0]);
                Assert.That(windingUp.State, Is.EqualTo(ShipAbilityState.Recovering));
                Assert.That(windingUp.TimeLeft, Is.EqualTo(definition.RecoveryDelay + settings.Windup * 0.5f +
                    100f / settings.Projectile.ProjectileSpeed).Within(0.001f));
                service.Advance(settings.Windup * 0.5f);
                shot = (ShotEffect)typeof(LuckyShotAbility)
                    .GetField("_shot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ability);
                Assert.That(shot, Is.Not.Null);
                Assert.That(targetHealth.DamageReceived, Is.Zero, "Launch is not impact.");
                Assert.That(ability.SurvivesCasterDeath, Is.True);
                AbilitySlotSave inFlight = AbilitySlotSave.From(caster.Slots[0]);
                Assert.That(inFlight.TimeLeft, Is.EqualTo(definition.RecoveryDelay + 100f / settings.Projectile.ProjectileSpeed)
                    .Within(0.001f));
                var loaded = new FakeCommand(definition, muzzle.transform, ShipAbilityId.LuckyShot);
                var restoredService = new ShipAbilityService(new FakeFactory(), relations, vision);
                restoredService.Restore(loaded.Slots[0], inFlight.State, inFlight.TimeLeft);
                Assert.That(loaded.Slots[0].RunningAbility, Is.Null, "Loading must not replay the shot.");
                Assert.That(loaded.Slots[0].TimeLeft, Is.EqualTo(inFlight.TimeLeft));
                restoredService.Clear();
                service.Advance(100f / settings.Projectile.ProjectileSpeed + 0.01f);
                Assert.That(targetHealth.DamageReceived, Is.EqualTo(authority ? settings.Damage : 0f));
                if (authority) Assert.That(targetHealth.LastDamageType, Is.EqualTo(DamageType.PrecisionStrike));
                Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
                Assert.That(caster.Slots[0].TimeLeft, Is.EqualTo(definition.RecoveryDelay));
                service.Advance(definition.RecoveryDelay - 1f);
                Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
                service.Advance(1f);
                Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
                Assert.That(targetHealth.DamageReceived, Is.EqualTo(authority ? settings.Damage : 0f));
            }
            finally
            {
                service.Clear();
                if (shot != null) UnityEngine.Object.DestroyImmediate(shot.gameObject);
                UnityEngine.Object.DestroyImmediate(muzzle);
            }
        }

        [Test]
        public void LuckyShot_RemovedTargetDuringWindupCancelsWithoutDamage()
        {
            var entities = new EntityLocator();
            var ability = new LuckyShotAbility(new LuckyShotSettings(), entities, null, null,
                new VisionService(TestPlayers.CreateDuel(), fogEnabled: false),
                new OfflineSession(TestPlayers.Human));
            FakeCommand caster = CreateCaster();
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub() };
            FakeCommand target = CreateCaster();
            entities.AddEntity(target.Entity);
            ability.Start(caster, new ShipAbilityDefinition(), target.Entity);
            entities.RemoveEntity(target.Entity);
            ability.Advance(2f);
            Assert.That(ability.IsComplete, Is.True);
            Assert.That(target.FakeHealth.DamageReceived, Is.Zero);
            ability.Stop();
        }

        [TestCase(UnitOrderType.Move)]
        [TestCase(UnitOrderType.None)]
        public void LuckyShot_MoveOrStopDuringWindupCancelsWithoutLaunching(UnitOrderType order)
        {
            var entities = new EntityLocator();
            var ability = new LuckyShotAbility(new LuckyShotSettings(), entities, null, null,
                new VisionService(TestPlayers.CreateDuel(), fogEnabled: false),
                new OfflineSession(TestPlayers.Human));
            FakeCommand caster = CreateCaster();
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub() };
            FakeCommand target = CreateCaster();
            entities.AddEntity(target.Entity);
            ability.Start(caster, new ShipAbilityDefinition(), target.Entity);
            caster.CurrentOrder = order;
            ability.Advance(2f);
            Assert.That(ability.IsComplete, Is.True);
            Assert.That(ability.SurvivesCasterDeath, Is.False);
            Assert.That(target.FakeHealth.DamageReceived, Is.Zero);
            ability.Stop();
        }

        [Test]
        public void LuckyShot_ReplicaActivationAfterLeaderLossDoesNotFireFromAWingman()
        {
            var ability = new LuckyShotAbility(new LuckyShotSettings(), new EntityLocator(), null, null,
                new VisionService(TestPlayers.CreateDuel(), fogEnabled: false),
                new OnlineSession(false, TestPlayers.Human));
            FakeCommand caster = CreateCaster();
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub { Id = 1 } };
            FakeCommand target = CreateCaster();
            ability.Start(caster, new ShipAbilityDefinition(), target.Entity);
            ability.Advance(2f);
            Assert.That(ability.IsComplete, Is.True);
            Assert.That(ability.SurvivesCasterDeath, Is.False);
            Assert.That(target.FakeHealth.DamageReceived, Is.Zero);
            ability.Stop();
        }

        private static void SetBeamField(SustainedBeamAbility beam, string name, object value) =>
            typeof(SustainedBeamAbility).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(beam, value);

        [TearDown]
        public void TearDown()
        {
            // A fired wave's view is a scene root; Release uses Destroy, which edit mode refuses.
            foreach (IonPulseView view in UnityEngine.Object.FindObjectsByType<IonPulseView>())
                UnityEngine.Object.DestroyImmediate(view.gameObject);
            if (_targetView != null) UnityEngine.Object.DestroyImmediate(_targetView);
        }

        [Test]
        public void IonPulse_AuthorityFires_ServiceRaisesFiredOnceWithWaveGeometry()
        {
            FakeCommand caster = CreateIonPulseCaster(out FakeCommand target);
            IonPulseSettings settings = CreateIonPulseSettings();
            // The service checks the slot's settings (required ion cannon) before it starts the ability.
            SetField(caster.Slots[0].Definition, "settings", settings);
            var relations = TestPlayers.CreateDuel();
            var ability = new IonPulseAbility(settings, new EntityLocator(), relations, new OfflineSession(TestPlayers.Human));
            var service = new ShipAbilityService(new SingleAbilityFactory(ability), relations,
                new VisionService(relations, fogEnabled: false));
            var fires = new List<(IShipAbilityFacade Caster, ShipAbilityId Id, ShipAbilityFire Fire)>();
            service.Fired += (firedCaster, id, fire) => fires.Add((firedCaster, id, fire));
            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, target.Entity), Is.True);

            service.Advance(0.1f);
            service.Advance(0.1f);

            Assert.That(fires.Count, Is.EqualTo(1));
            Assert.That(fires[0].Caster, Is.SameAs(caster));
            Assert.That(fires[0].Id, Is.EqualTo(ShipAbilityId.BoostEnginePower));
            // The starboard cannon sits at +X; the target lies dead to starboard on the same plane.
            Vector3 cannon = Vector3.right;
            Vector3 toTarget = target.FakeHealth.Transform.position - cannon;
            Assert.That(fires[0].Fire.Origin, Is.EqualTo(cannon));
            Assert.That(fires[0].Fire.Direction, Is.EqualTo(toTarget.normalized));
            Assert.That(fires[0].Fire.Rise, Is.Zero);
            Assert.That(fires[0].Fire.Range,
                Is.EqualTo(Mathf.Max(caster.Slots[0].Definition.Range, toTarget.magnitude + settings.WaveThickness)));
            Assert.That(caster.IsAbilityFacing, Is.False);
        }

        [Test]
        public void IonPulse_ReplicaAlignedToTarget_WaitsForHostFire()
        {
            FakeCommand caster = CreateIonPulseCaster(out FakeCommand target);
            var ability = new IonPulseAbility(CreateIonPulseSettings(), new EntityLocator(), TestPlayers.CreateDuel(),
                new OnlineSession(false, TestPlayers.Human));
            int fired = 0;
            ability.Fired += fire => fired++;

            ability.Start(caster, caster.Slots[0].Definition, target.Entity);
            ability.Advance(0.1f);
            ability.Advance(0.1f);

            Assert.That(fired, Is.Zero);
            Assert.That(ability.SurvivesCasterDeath, Is.False);
            Assert.That(ability.IsComplete, Is.False);
            Assert.That(caster.IsAbilityFacing, Is.False);
            Assert.That(FindFiredIonPulseView(), Is.Null);
        }

        [Test]
        public void IonPulse_ReplicaFireReplica_PlaysHostGeometryOnlyWhileActive()
        {
            FakeCommand caster = CreateIonPulseCaster(out FakeCommand target);
            IonPulseSettings settings = CreateIonPulseSettings();
            var relations = TestPlayers.CreateDuel();
            var ability = new IonPulseAbility(settings, new EntityLocator(), relations,
                new OnlineSession(false, TestPlayers.Human));
            var service = new ShipAbilityService(new SingleAbilityFactory(ability), relations,
                new VisionService(relations, fogEnabled: false));
            int fired = 0;
            ability.Fired += fire => fired++;
            // A host shot that points away from where this client would aim on its own.
            var hostFire = new ShipAbilityFire(new Vector3(0f, 0f, 5f), Vector3.forward, 0f, 500f);

            service.FireReplica(caster, ShipAbilityId.BoostEnginePower, hostFire);
            Assert.That(FindFiredIonPulseView(), Is.Null);

            service.ActivateReplica(caster, ShipAbilityId.BoostEnginePower, target.Entity);
            service.FireReplica(caster, ShipAbilityId.BoostEnginePower, hostFire);
            service.Advance(0.1f);

            Assert.That(ability.SurvivesCasterDeath, Is.True);
            Assert.That(fired, Is.Zero);
            IonPulseView view = FindFiredIonPulseView();
            Assert.That(view, Is.Not.Null);
            Vector3 expected = hostFire.Origin + hostFire.Direction * Mathf.Min(hostFire.Range, settings.WaveSpeed * 0.1f);
            Assert.That(Vector3.Distance(view.transform.position, expected), Is.LessThan(0.001f));
        }

        private FakeCommand CreateIonPulseCaster(out FakeCommand target)
        {
            _targetView = new GameObject("Ion pulse participants");
            Transform targetTransform = new GameObject("Ion pulse target").transform;
            targetTransform.SetParent(_targetView.transform);
            // Dead to starboard: the starboard cannon is already aligned and fires on the first advance.
            targetTransform.position = Vector3.right * 100f;
            var caster = new FakeCommand(new ShipAbilityDefinition(), _targetView.transform);
            target = new FakeCommand(new ShipAbilityDefinition(), targetTransform, entityId: 2);
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub() };
            return caster;
        }

        // A working view prefab, parented under the participants root so only fired copies are scene roots.
        private IonPulseSettings CreateIonPulseSettings()
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            GameObject prefab = new GameObject("Ion pulse view prefab");
            prefab.transform.SetParent(_targetView.transform);
            IonPulseView view = prefab.AddComponent<IonPulseView>();
            GameObject stun = new GameObject("Ion pulse stun field");
            stun.transform.SetParent(prefab.transform);
            IonStunView stunView = stun.AddComponent<IonStunView>();
            typeof(IonStunView).GetField("shimmer", PRIVATE_INSTANCE).SetValue(stunView, stun.AddComponent<MeshRenderer>());
            typeof(IonStunView).GetField("arcs", PRIVATE_INSTANCE).SetValue(stunView, new LineRenderer[0]);
            typeof(IonPulseView).GetField("wave", PRIVATE_INSTANCE).SetValue(view, prefab.AddComponent<LineRenderer>());
            typeof(IonPulseView).GetField("ionStunField", PRIVATE_INSTANCE).SetValue(view, stunView);
            var settings = new IonPulseSettings();
            typeof(IonPulseSettings).GetField("viewPrefab", PRIVATE_INSTANCE).SetValue(settings, view);
            return settings;
        }

        private static IonPulseView FindFiredIonPulseView()
        {
            foreach (IonPulseView view in UnityEngine.Object.FindObjectsByType<IonPulseView>())
                if (view.transform.parent == null) return view;
            return null;
        }

        [Test]
        public void PhasedAbility_IonDisabledBeforeTick_StopsWithoutAdvancing()
        {
            RecordingAdvancingAbility ability = new RecordingAdvancingAbility();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new SingleAbilityFactory(ability),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(0.1f);
            caster.Modifiers.SetIonDisabled(true);

            service.Advance(0.2f);

            Assert.That(ability.AdvanceCount, Is.EqualTo(1));
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(caster.Slots[0].TimeLeft, Is.EqualTo(6f));
            service.Clear();
            Assert.That(ability.StopCount, Is.EqualTo(1));
        }

        [Test]
        public void PhasedAbility_ContinuesDuringIonDisable_WhenItsEffectPersists()
        {
            RecordingAdvancingAbility ability = new RecordingAdvancingAbility { CancelOnIonDisable = false };
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new SingleAbilityFactory(ability),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            caster.Modifiers.SetIonDisabled(true);

            service.Advance(0.2f);

            Assert.That(ability.AdvanceCount, Is.EqualTo(1));
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.Clear();
        }

        [Test]
        public void CompletableAbility_StopsWhenCompleteWithoutTickWork()
        {
            var ability = new RecordingCompletableAbility();
            var relations = TestPlayers.CreateDuel();
            var service = new ShipAbilityService(new SingleAbilityFactory(ability),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);

            service.Advance(0.1f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));

            ability.IsComplete = true;
            service.Advance(0.1f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(ability.StopCount, Is.EqualTo(1));
        }

        [Test]
        public void PhasedAbility_CompletesDuringTick_StopsInSameTick()
        {
            RecordingAdvancingAbility ability = new RecordingAdvancingAbility { CompleteOnAdvance = true };
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new SingleAbilityFactory(ability),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);

            service.Advance(0.2f);

            Assert.That(ability.AdvanceCount, Is.EqualTo(1));
            Assert.That(ability.StopCount, Is.EqualTo(1));
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
        }

        [Test]
        public void IonPulse_TargetCloaksWhileTurning_KeepsAimingAtLastPosition()
        {
            _targetView = new GameObject("Ion pulse participants");
            Transform targetTransform = new GameObject("Ion pulse target").transform;
            targetTransform.SetParent(_targetView.transform);
            // Ahead of the bow: the starboard ion cannon still has to turn toward it.
            targetTransform.position = Vector3.forward * 100f;
            FakeCommand caster = new FakeCommand(new ShipAbilityDefinition(), _targetView.transform);
            FakeCommand target = new FakeCommand(new ShipAbilityDefinition(), targetTransform);
            caster.FakeHealth.Units = new IHardPointModel[] { new IonCannonStub() };
            IonPulseAbility ability = new IonPulseAbility(new IonPulseSettings(), new EntityLocator(), TestPlayers.CreateDuel(),
                new OfflineSession(TestPlayers.Human));
            Assert.That(new IonPulseSettings().CanTarget(caster, target.Entity), Is.True);
            ability.Start(caster, caster.Slots[0].Definition, target.Entity);
            ability.Advance(0.1f);
            Assert.That(ability.IsComplete, Is.False);
            target.Modifiers.SetCloaked(true);

            ability.Advance(0.1f);

            Assert.That(ability.IsComplete, Is.False);
            Assert.That(caster.IsAbilityFacing, Is.True);
            Assert.That(ability.SurvivesCasterDeath, Is.False);
            ability.Stop();
            Assert.That(caster.IsAbilityFacing, Is.False);
        }

        [Test]
        public void Activation_ProgressesThroughDurationAndRecovery()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();

            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.Advance(4f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.Advance(1f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));
            service.Advance(6f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));

            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);
            Assert.That(factory.Created.Count, Is.EqualTo(2));
            Assert.That(factory.Created[1], Is.Not.SameAs(factory.Created[0]));
            service.LateDispose();
            Assert.That(factory.Created[1].StopCount, Is.EqualTo(1));
        }

        [Test]
        public void ZeroRecovery_ReactivatedAbility_ElapsesOncePerAdvance()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster(recoveryDelay: 0f);

            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(5f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));

            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(1f);
            Assert.That(caster.Slots[0].TimeLeft, Is.EqualTo(4f));
            service.LateDispose();
            Assert.That(factory.Created[1].StopCount, Is.EqualTo(1));
        }

        [Test]
        public void Cancel_StopsOnceAndEntersFullRecovery_OnlyForToggles()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster(isToggle: true);
            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower);

            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(caster.Slots[0].TimeLeft, Is.EqualTo(6f));
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));
            service.Advance(6f);
            service.LateDispose();
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));

            // A cancellable timed ability runs its full time: players and auto power may end only toggles.
            FakeCommand timed = CreateCaster();
            ShipAbilityService timedService = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            timedService.Press(new IEntity[] { timed.Entity }, ShipAbilityId.BoostEnginePower);
            timedService.Press(new IEntity[] { timed.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(timed.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(timed.Slots[0].CanCancel, Is.False);
            Assert.That(timedService.TryRequestCancel(timed, ShipAbilityId.BoostEnginePower), Is.False);
            Assert.That(timedService.TryOwnerCancel(timed, ShipAbilityId.BoostEnginePower), Is.False);
            timedService.Execute(new[] { timed.Entity }, ShipAbilityId.BoostEnginePower, null, cancel: true);
            Assert.That(timed.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            // Game rules (Cloak on attack, Interdict on Retreat) still end it.
            Assert.That(timedService.TryCancel(timed, ShipAbilityId.BoostEnginePower), Is.True);
            Assert.That(timed.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            timedService.LateDispose();

            FakeCommand fixedDuration = CreateCaster(canCancel: false);
            ShipAbilityService fixedService = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            fixedService.Press(new IEntity[] { fixedDuration.Entity }, ShipAbilityId.BoostEnginePower);
            fixedService.Press(new IEntity[] { fixedDuration.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(fixedDuration.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            fixedService.LateDispose();
        }

        [Test]
        public void Cancel_InterdictGravityWell_OwnerMayEndIt()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            SetField(caster.Slots[0].Definition, "settings", new GravityWellSettings());
            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);

            Assert.That(caster.Slots[0].CanCancel, Is.True);
            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower);

            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            service.LateDispose();
        }

        [Test]
        public void CasterDeath_StopsActiveAbilityOnce()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            caster.FakeHealth.IsDestroyedValue = true;

            service.Advance(1f);
            service.LateDispose();
            Assert.That(factory.Created[0].StopCount, Is.EqualTo(1));
        }

        [TestCase(1059.4355f, 50f, 1200f, false)]
        [TestCase(1059.4355f, 50f, 1300f, true)]
        [TestCase(100f, 50f, 499f, false)]
        [TestCase(100f, 50f, 501f, true)]
        public void TractorBeam_EndsOnlyOutsideTheActivationRange(float casterRadius, float targetRadius,
            float distance, bool expectedComplete)
        {
            FakeCommand caster = CreateCaster();
            caster.FakeHealth.NavigationRadius = casterRadius;
            _targetView = new GameObject("Tractor target");
            _targetView.transform.position = Vector3.right * distance;
            var targetHealth = new FakeHealth(_targetView.transform) { NavigationRadius = targetRadius };
            var target = new FakeEntity(command: null, owner: TestPlayers.Enemy, health: targetHealth);
            var ability = new TractorBeamAbility(new TractorBeamSettings());
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(TractorBeamAbility).GetField("_caster", FLAGS).SetValue(ability, caster);
            typeof(TractorBeamAbility).GetField("_target", FLAGS).SetValue(ability, target);
            typeof(TractorBeamAbility).GetField("_targetTransform", FLAGS).SetValue(ability, _targetView.transform);
            typeof(TractorBeamAbility).GetField("_hardPoint", FLAGS).SetValue(ability, new IonCannonStub());
            typeof(TractorBeamAbility).GetField("_range", FLAGS).SetValue(ability, 500f);

            Assert.That(ShipAbilityRange.IsInRange(caster, target, 500f), Is.EqualTo(!expectedComplete));
            Assert.That(ability.IsComplete, Is.EqualTo(expectedComplete));
        }

        [Test]
        public void Targeting_RejectsOutOfRangeAndActivatesInRange()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            int cancellations = 0;
            service.TargetingCanceled += _ => cancellations++;
            FakeCommand caster = CreateCaster(targeted: true);
            _targetView = new GameObject("Target");
            FakeHealth targetHealth = new FakeHealth(_targetView.transform);
            FakeEntity target = new FakeEntity(command: null, owner: TestPlayers.Enemy, health: targetHealth);

            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
            Assert.That(service.IsWaitingForTarget, Is.True);
            _targetView.transform.position = Vector3.right * 11f;
            Assert.That(service.IsValidTarget(target), Is.False);
            Assert.That(factory.Created, Is.Empty, "Hovering a target must not construct an ability.");
            service.SubmitTarget(target);
            Assert.That(service.IsWaitingForTarget, Is.True);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));

            _targetView.transform.position = Vector3.right * 9f;
            Assert.That(service.IsValidTarget(target), Is.True);
            service.SubmitTarget(target);
            Assert.That(service.IsWaitingForTarget, Is.False);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(cancellations, Is.Zero, "An accepted target must not play the targeting-canceled cue.");
            service.LateDispose();
        }

        [Test]
        public void AuthorityRejectsKnownTargetsHiddenFromTheCasterTeam()
        {
            var relations = TestPlayers.CreateDuel();
            var vision = new VisionService(relations);
            var factory = new FakeFactory();
            var service = new ShipAbilityService(factory, relations, vision);
            var caster = CreateCaster(targeted: true);
            _targetView = new GameObject("Target");
            _targetView.transform.position = Vector3.right * 5f;
            var target = new FakeEntity(null, new FakeHealth(_targetView.transform), TestPlayers.Enemy, id: 2);
            var entities = new EntityLocator();
            entities.AddEntity(caster.Entity);
            entities.AddEntity(target);
            var handlers = new PlayerCommandHandlers();
            handlers.RegisterAbilities(service);
            var sink = new AuthorityCommandSink(entities, null, handlers, null);
            ShipAbilityCommand command = ShipAbilityCommand.Create(TestPlayers.Human, caster.Slots[0].Id,
                new[] { caster.Entity }, target, cancel: false);

            vision.Register(TestPlayers.Enemy, _targetView.transform, 10f);
            sink.Submit(command);
            Assert.That(factory.Created, Is.Empty);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));

            vision.Register(TestPlayers.Human, _targetView.transform, 10f);
            sink.Submit(command);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.LateDispose();
        }

        [Test]
        public void AuthorityAcceptsHullOverlapAndFiringRevealWithoutCenterVision()
        {
            var relations = TestPlayers.CreateTeamGame();
            var vision = new VisionService(relations);
            var service = new ShipAbilityService(new FakeFactory(), relations, vision);
            var caster = CreateCaster(targeted: true);
            _targetView = new GameObject("Target");
            _targetView.transform.position = Vector3.right * 300f;
            var target = new FakeEntity(null, new FakeHealth(_targetView.transform), TestPlayers.Enemy, id: 2)
            {
                Footprint = new ShipVisionFootprint(_targetView.transform, 2, new[]
                {
                    new Vector2(-250f, 0f), new Vector2(50f, -50f), new Vector2(50f, 50f)
                })
            };
            Transform source = new GameObject("Vision Source").transform;
            source.SetParent(_targetView.transform);
            vision.Register(TestPlayers.Human, source, 100f);

            Assert.That(vision.IsVisible(TestPlayers.Human, _targetView.transform.position), Is.False);
            Assert.That(service.CanTarget(caster, caster.Slots[0].Definition, target), Is.True);
            vision.Unregister(source);
            Assert.That(service.CanTarget(caster, caster.Slots[0].Definition, target), Is.False);
            vision.Reveal(TestPlayers.Ally, target.Id, 5f);
            Assert.That(service.CanTarget(caster, caster.Slots[0].Definition, target), Is.True);
            service.LateDispose();
        }

        [Test]
        public void MissingSettings_FailsAvailabilityCheck()
        {
            FakeCommand caster = CreateCaster();
            SetField(caster.Slots[0].Definition, "settings", null);

            Assert.Throws<NullReferenceException>(() => _ = caster.Slots[0].IsAvailable);
        }

        [Test]
        public void CloakedCaster_CannotArmAnUnavailableTargetedAbility()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster(targeted: true);
            ShipAbilitySlot slot = caster.Slots[0];
            SetField(slot.Definition, "settings", new BoostEnginePowerSettings());
            caster.Modifiers.SetCloaked(true);

            Assert.That(slot.CanPress, Is.False);
            service.Press(new[] { caster.Entity }, slot.Id);
            Assert.That(service.IsWaitingForTarget, Is.False);
        }

        [Test]
        public void FighterReinforcements_RequiresAnAvailableReplacementForReadinessAndActivation()
        {
            var definition = new ShipAbilityDefinition();
            SetField(definition, "settings", new FighterReinforcementsSettings());
            SetField(definition, "recoveryDelay", 30f);
            var caster = new FakeCommand(definition, id: ShipAbilityId.FighterReinforcements);
            var relations = TestPlayers.CreateDuel();
            var service = new ShipAbilityService(new ShipAbilityFactory(null),
                relations, new VisionService(relations, fogEnabled: false));
            int rejections = 0;
            service.TargetRejected += _ => rejections++;
            ShipAbilitySlot slot = caster.Slots[0];

            Assert.That(slot.CanPress, Is.False);
            Assert.That(slot.CanActivate, Is.False);
            service.Press(new[] { caster.Entity }, slot.Id);
            Assert.That(rejections, Is.EqualTo(1));
            Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Ready));

            caster.CanLaunchReplacement = true;
            Assert.That(slot.CanPress, Is.True);
            service.Press(new[] { caster.Entity }, slot.Id);
            Assert.That(caster.CanLaunchReplacement, Is.False);
            service.Advance(0.1f);
            Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(rejections, Is.EqualTo(1));
            service.Clear();
        }

        [Test]
        public void GroupPress_PreservesActiveAbilityAndActivatesReadyShip()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand first = CreateCaster();
            FakeCommand second = CreateCaster();
            service.TryActivate(first, ShipAbilityId.BoostEnginePower, null);
            service.Advance(1f);

            service.Press(new IEntity[] { first.Entity, second.Entity },
                ShipAbilityId.BoostEnginePower);

            Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(first.Slots[0].TimeLeft, Is.EqualTo(4f));
            Assert.That(factory.Created[0].StopCount, Is.Zero);
            Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(factory.Created.Count, Is.EqualTo(2));
            service.LateDispose();
        }

        [Test]
        public void GroupPress_WhenAllAbilitiesActive_PreservesThem()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand first = CreateCaster();
            FakeCommand second = CreateCaster();
            IEntity[] casters = { first.Entity, second.Entity };
            service.Press(casters, ShipAbilityId.BoostEnginePower);
            service.Advance(1f);

            service.Press(casters, ShipAbilityId.BoostEnginePower);

            Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(first.Slots[0].TimeLeft, Is.EqualTo(4f));
            Assert.That(second.Slots[0].TimeLeft, Is.EqualTo(4f));
            Assert.That(factory.Created.Count, Is.EqualTo(2));
            Assert.That(factory.Created[0].StopCount, Is.Zero);
            Assert.That(factory.Created[1].StopCount, Is.Zero);
            service.LateDispose();
        }

        [Test]
        public void GroupPress_SingleShipGroup_PreservesActiveAbility()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(1f);

            service.Press(new IEntity[] { caster.Entity }, ShipAbilityId.BoostEnginePower,
                allowCancel: false);

            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(caster.Slots[0].TimeLeft, Is.EqualTo(4f));
            Assert.That(factory.Created.Count, Is.EqualTo(1));
            Assert.That(factory.Created[0].StopCount, Is.Zero);
            service.LateDispose();
        }

        [Test]
        public void GroupPress_ActivatesOnlyReadySlots()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand first = CreateCaster();
            FakeCommand second = CreateCaster();
            service.TryActivate(first, ShipAbilityId.BoostEnginePower, null);
            service.Advance(5f);

            service.Press(new IEntity[] { first.Entity, second.Entity },
                ShipAbilityId.BoostEnginePower);

            Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.LateDispose();
        }

        [Test]
        public void TargetingPruning_PreservesSurvivingCastersAndClearsLastCaster()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand first = CreateCaster(targeted: true);
            FakeCommand second = CreateCaster(targeted: true);
            service.Press(new IEntity[] { first.Entity, second.Entity }, ShipAbilityId.BoostEnginePower);
            first.FakeHealth.IsDestroyedValue = true;
            service.PruneTargeting(new IEntity[] { second.Entity });
            Assert.That(service.IsWaitingForTarget, Is.True);
            Assert.That(service.PendingCasters, Is.EqualTo(new[] { second.Entity }));
            service.PruneTargeting(Array.Empty<IEntity>());
            Assert.That(service.IsWaitingForTarget, Is.False);
            Assert.That(service.PendingCasters, Is.Empty);
        }

        [Test]
        public void AbilityButtonSelectingAlreadyActiveCaster_ShowsDurationImmediatelyWithReadyGroupMember()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand active = CreateCaster();
            FakeCommand ready = CreateCaster();
            service.TryActivate(active, ShipAbilityId.BoostEnginePower, null);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/Ship/ShipAbilityButtonUi.prefab");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            ShipAbilityButtonUi view = instance.GetComponent<ShipAbilityButtonUi>();
            try
            {
                view.Configure(ShipAbilityId.BoostEnginePower,
                    new[] { active.Slots[0], ready.Slots[0] }, null, _ => { });
                UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(view);
                GameObject highlight = (GameObject)serialized.FindProperty("activeHighlight").objectReferenceValue;
                TMPro.TMP_Text timer = (TMPro.TMP_Text)serialized.FindProperty("cooldownText").objectReferenceValue;
                Assert.That(highlight.activeSelf, Is.True);
                UnityEngine.UI.Image progress = (UnityEngine.UI.Image)serialized.FindProperty("cooldownFill").objectReferenceValue;
                Assert.That(progress.fillAmount, Is.EqualTo(active.Slots[0].Progress01));
                Assert.That(timer.text, Is.EqualTo(UnityEngine.Mathf.CeilToInt(active.Slots[0].TimeLeft) + "s"));
            }
            finally
            {
                InvokeButtonLifecycle(view, "OnDestroy");
                UnityEngine.Object.DestroyImmediate(instance);
                service.LateDispose();
            }
        }

        [Test]
        public void AbilityButtonAutomaticStart_RendersBeforeAnotherUiUpdateAndReleasesPooledSlots()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand first = CreateCaster();
            FakeCommand second = CreateCaster();
            List<ShipAbilitySlot> pooledSlots = new List<ShipAbilitySlot> { first.Slots[0] };
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/Ship/ShipAbilityButtonUi.prefab");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            ShipAbilityButtonUi view = instance.GetComponent<ShipAbilityButtonUi>();
            try
            {
                view.Configure(ShipAbilityId.BoostEnginePower, pooledSlots, null, _ => { });
                UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(view);
                GameObject highlight = (GameObject)serialized.FindProperty("activeHighlight").objectReferenceValue;
                TMPro.TMP_Text timer = (TMPro.TMP_Text)serialized.FindProperty("cooldownText").objectReferenceValue;
                Assert.That(highlight.activeSelf, Is.False);
                Assert.That(service.TryActivate(first, ShipAbilityId.BoostEnginePower, null), Is.True);
                Assert.That(highlight.activeSelf, Is.True);
                Assert.That(timer.text, Is.EqualTo(Mathf.CeilToInt(first.Slots[0].TimeLeft) + "s"));

                // Ability bars reuse their group lists before reconfiguring their pooled button.
                pooledSlots.Clear();
                pooledSlots.Add(second.Slots[0]);
                view.Configure(ShipAbilityId.BoostEnginePower, pooledSlots, null, _ => { });
                Assert.That(HasButtonSubscriber(first.Slots[0], view), Is.False);
                Assert.That(highlight.activeSelf, Is.False);
                Assert.That(service.TryActivate(second, ShipAbilityId.BoostEnginePower, null), Is.True);
                Assert.That(highlight.activeSelf, Is.True);

                // EditMode tests invoke lifecycle directly because SetActive/DestroyImmediate
                // do not consistently invoke non-ExecuteAlways MonoBehaviour callbacks.
                InvokeButtonLifecycle(view, "OnDisable");
                Assert.That(HasButtonSubscriber(second.Slots[0], view), Is.False);
                InvokeButtonLifecycle(view, "OnEnable");
                Assert.That(HasButtonSubscriber(second.Slots[0], view), Is.True);
                InvokeButtonLifecycle(view, "OnDestroy");
                Assert.That(HasButtonSubscriber(second.Slots[0], view), Is.False);
            }
            finally
            {
                InvokeButtonLifecycle(view, "OnDestroy");
                UnityEngine.Object.DestroyImmediate(instance);
                service.LateDispose();
            }
        }

        private static void InvokeButtonLifecycle(ShipAbilityButtonUi view, string method) =>
            typeof(ShipAbilityButtonUi).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null);

        private static bool HasButtonSubscriber(ShipAbilitySlot slot, ShipAbilityButtonUi view)
        {
            Action changed = (Action)typeof(ShipAbilitySlot).GetField("StateChanged",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(slot);
            if (changed == null) return false;
            foreach (Delegate subscriber in changed.GetInvocationList())
                if (ReferenceEquals(subscriber.Target, view)) return true;
            return false;
        }

        [Test]
        public void ShipUiEscape_CancelsArmedAbilityBeforePauseMenuFallback()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster(targeted: true);
            AbilityCancelInput input = new AbilityCancelInput();
            UiCancelRouter router = new UiCancelRouter(input);
            ShipUiModel model = new ShipUiModel();
            ShipUiController controller = new ShipUiController(uiService: null, cancelRouter: router,
                selectionService: new AbilitySelection(), routeNavigation: new AbilityRoutes(),
                cameraService: null, tooltipService: null, bindings: null, model: model,
                icons: null, shipAbilityService: service, tooltipIcons: null, factions: null,
                settings: new SettingsService(null,
                    Array.Empty<ISettingsApplier>()),
                localPlayer: TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), destroyFlow: null);
            int unhandledCount = 0;
            router.CancelUnhandled += () => unhandledCount++;
            router.Initialize();
            controller.Initialize();
            try
            {
                service.Press(new[] { caster.Entity }, ShipAbilityId.BoostEnginePower);
                Assert.That(controller.IsFocused, Is.True);
                Assert.That(model.PendingAbilityId, Is.EqualTo(ShipAbilityId.BoostEnginePower));
                input.Press();
                Assert.That(service.IsWaitingForTarget, Is.False);
                Assert.That(model.PendingAbilityId, Is.Null);
                Assert.That(controller.IsFocused, Is.False);
                Assert.That(unhandledCount, Is.Zero);
                input.Press();
                Assert.That(unhandledCount, Is.EqualTo(1));
            }
            finally
            {
                controller.LateDispose();
                router.Dispose();
                service.LateDispose();
            }
        }

        [Test]
        public void TargetingModel_OnlyMarksCastersInArmedGroup()
        {
            FakeCommand first = CreateCaster(targeted: true);
            FakeCommand second = CreateCaster(targeted: true);
            ShipUiModel model = new ShipUiModel();
            model.SetPendingAbility(ShipAbilityId.BoostEnginePower, new[] { first.Entity });
            Assert.That(model.IsPendingCaster(first.Entity), Is.True);
            Assert.That(model.IsPendingCaster(second.Entity), Is.False);
            model.SetPendingAbility(null, Array.Empty<IEntity>());
            Assert.That(model.IsPendingCaster(first.Entity), Is.False);
        }

        [Test]
        public void UnknownAbilityId_TryActivateReturnsFalse()
        {
            FakeFactory factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();

            Assert.That(service.TryActivate(caster, ShipAbilityId.ProtonBeam, null), Is.False);
            Assert.That(factory.Created, Is.Empty);
        }

        [Test]
        public void Save_ActiveAbility_BecomesRecoveringForRemainingPlusFullRecovery()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
            service.Advance(2f);

            AbilitySlotSave save = AbilitySlotSave.From(caster.Slots[0]);

            Assert.That(save.State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(save.TimeLeft, Is.EqualTo(caster.Slots[0].TimeLeft + caster.Slots[0].Definition.RecoveryDelay));
            service.LateDispose();
        }

        [Test]
        public void SlotStateChanged_ReportsLifecycleTransitionsWithoutPerFrameElapsedNotifications()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            ShipAbilitySlot slot = caster.Slots[0];
            int stateChanges = 0;
            int allChanges = 0;
            slot.StateChanged += () => stateChanges++;
            slot.Changed += () => allChanges++;
            try
            {
                service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null);
                Assert.That(stateChanges, Is.EqualTo(1));
                int beforeElapsed = allChanges;
                service.Advance(0.1f);
                Assert.That(allChanges, Is.GreaterThan(beforeElapsed));
                Assert.That(stateChanges, Is.EqualTo(1));
                service.TryCancel(caster, ShipAbilityId.BoostEnginePower);
                Assert.That(stateChanges, Is.EqualTo(2));
                beforeElapsed = allChanges;
                service.Advance(0.1f);
                Assert.That(allChanges, Is.GreaterThan(beforeElapsed));
                Assert.That(stateChanges, Is.EqualTo(2));
                service.Advance(slot.Definition.RecoveryDelay);
                Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Ready));
                Assert.That(stateChanges, Is.EqualTo(3));
                service.Restore(slot, ShipAbilityState.Recovering, 2f);
                Assert.That(stateChanges, Is.EqualTo(4));
            }
            finally
            {
                service.LateDispose();
            }
        }

        [Test]
        public void Restore_LongCooldown_BarStartsFullAndCountsDownToReady()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            ShipAbilitySlot slot = caster.Slots[0];
            float cooldown = slot.Definition.RecoveryDelay * 2f;

            service.Restore(slot, ShipAbilityState.Recovering, cooldown);

            Assert.That(slot.Progress01, Is.EqualTo(1f));
            service.Advance(cooldown / 2f);
            Assert.That(slot.Progress01, Is.EqualTo(0.5f).Within(0.0001f));
            service.Advance(cooldown / 2f);
            Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Ready));
            Assert.That(service.TryActivate(caster, ShipAbilityId.BoostEnginePower, null), Is.True);
        }

        [Test]
        public void Restore_ActiveState_IsRejected()
        {
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();

            Assert.Throws<ArgumentException>(() => service.Restore(caster.Slots[0], ShipAbilityState.Active, 1f));
        }

        [Test]
        public void ReplicaSnapshot_KeepsTransientActiveWithoutReplayingItsAction()
        {
            var factory = new FakeFactory();
            var relations = TestPlayers.CreateDuel();
            var service = new ShipAbilityService(factory,
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            ShipAbilitySlot slot = caster.Slots[0];
            int activationNotifications = 0;
            slot.StateChanged += () => activationNotifications++;

            service.RestoreReplicaState(caster, slot.Id, ShipAbilityState.Active, 2f, null);
            service.Advance(10f);

            Assert.That(factory.Created[0].StartCount, Is.Zero);
            Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(slot.CanActivate, Is.False);
            Assert.That(activationNotifications, Is.Zero);
            service.RestoreReplicaState(caster, slot.Id, ShipAbilityState.Recovering, 3f, null);
            service.Advance(10f);
            Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Recovering));
            service.RestoreReplicaState(caster, slot.Id, ShipAbilityState.Ready, 0f, null);
            Assert.That(slot.CanActivate, Is.True);
            Assert.That(service.TryActivate(caster, slot.Id, null), Is.True);
            Assert.That(factory.Created[1].StartCount, Is.EqualTo(1), "A subsequent live action must still execute.");
            service.Clear();
        }

        [Test]
        public void ReplicaSnapshot_RestoresPersistentEffectOnceAndStopsAtHostRecovery()
        {
            var relations = TestPlayers.CreateDuel();
            var service = new ShipAbilityService(new CloakFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            ShipAbilitySlot slot = caster.Slots[0];
            service.RestoreReplicaState(caster, slot.Id, ShipAbilityState.Active, 2f, null);
            IShipAbility running = slot.RunningAbility;
            service.RestoreReplicaState(caster, slot.Id, ShipAbilityState.Active, 1f, null);

            Assert.That(caster.Modifiers.IsCloaked, Is.True);
            Assert.That(slot.RunningAbility, Is.SameAs(running));
            Assert.That(slot.TimeLeft, Is.EqualTo(1f));
            ShipAbilityEventMessage snapshot = ShipAbilityEventMessage.Capture(slot);
            Assert.That(snapshot.RestoreState, Is.True);
            Assert.That(snapshot.State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(snapshot.TimeLeft, Is.EqualTo(1f));
            service.RestoreReplicaState(caster, slot.Id, ShipAbilityState.Recovering, 6f, null);
            Assert.That(caster.Modifiers.IsCloaked, Is.False);
            Assert.That(slot.TimeLeft, Is.EqualTo(6f));
            service.Clear();
        }

        [Test]
        public void AuthorityAbility_ReportsAutomaticRecoveryAndReadyForClientSnapshots()
        {
            var relations = TestPlayers.CreateDuel();
            var service = new ShipAbilityService(new FakeFactory(),
                relations, new VisionService(relations, fogEnabled: false));
            FakeCommand caster = CreateCaster();
            var states = new List<ShipAbilityState>();
            service.StateChanged += slot => states.Add(slot.State);
            service.TryActivate(caster, caster.Slots[0].Id, null);
            service.Advance(5f);
            service.Advance(6f);

            Assert.That(states, Is.EqualTo(new[]
                { ShipAbilityState.Active, ShipAbilityState.Recovering, ShipAbilityState.Ready }));
            service.Clear();
        }

        private static FakeCommand CreateCaster(bool canCancel = true, bool targeted = false,
            float recoveryDelay = 6f, bool isToggle = false)
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "duration", 5f);
            SetField(definition, "recoveryDelay", recoveryDelay);
            SetField(definition, "canCancel", canCancel);
            SetField(definition, "isToggle", isToggle);
            SetField(definition, "requiresEnemyTarget", targeted);
            SetField(definition, "range", 10f);
            SetField(definition, "settings", new BoostEnginePowerSettings());
            return new FakeCommand(definition);
        }

        private static void SetField(object instance, string name, object value)
        {
            typeof(ShipAbilityDefinition).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
        }

        private sealed class AbilityCancelInput : ICancelInput
        {
            public event Action CancelPressed;
            public void Press() => CancelPressed?.Invoke();
        }

        private sealed class AbilitySelection : ISelectionService
        {
            public string Id => nameof(AbilitySelection);
            public ISelectionContext PlayerSelectionContext => null;
            public ISelectionContext OtherSelectionContext => null;
            public void RemoveSelectable(ISelectionContext selectionContext) { }
            public void SelectEntity(IEntity entity) => throw new NotSupportedException();
            public void SelectEntities(IReadOnlyList<IEntity> entities) => throw new System.NotSupportedException();
            public void SelectCurrentUnitsByType(UnitTypeId unitTypeId) { }
            public void AddObserver(IObserver<ISelectionSubject> observer) { }
            public void RemoveObserver(IObserver<ISelectionSubject> observer) { }
        }

        private sealed class AbilityRoutes : ISkirmishRouteNavigation
        {
            public void RegisterRoute(SkirmishUiRoutePosition position, ISkirmishUiRoute route) { }
            public void UnregisterRoute(SkirmishUiRoutePosition position, ISkirmishUiRoute route) { }
            public void SetRouteActive(SkirmishUiRoutePosition position, bool isActive) { }
        }

        private sealed class FakeFactory : IShipAbilityFactory
        {
            public readonly List<RecordingAbility> Created = new List<RecordingAbility>();

            public IShipAbility Create(ShipAbilityDefinition definition)
            {
                RecordingAbility ability = new RecordingAbility();
                Created.Add(ability);
                return ability;
            }
        }

        private sealed class CloakFactory : IShipAbilityFactory
        {
            public IShipAbility Create(ShipAbilityDefinition definition) => new CloakAbility();
        }

        private sealed class RecordingAbility : IStoppableShipAbility
        {
            public int StopCount { get; private set; }
            public int StartCount { get; private set; }

            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition,
                IEntity target) => StartCount++;

            public void Stop() => StopCount++;
        }

        private sealed class SingleAbilityFactory : IShipAbilityFactory
        {
            private readonly IShipAbility _ability;

            public SingleAbilityFactory(IShipAbility ability) { _ability = ability; }

            public IShipAbility Create(ShipAbilityDefinition definition) => _ability;
        }

        private sealed class RecordingAdvancingAbility : IAdvancingShipAbility, IShipAbilityInterruptionPolicy
        {
            private IShipAbilityFacade _caster;
            private bool _complete;

            public int AdvanceCount { get; private set; }
            public int StopCount { get; private set; }
            public bool CompleteOnAdvance { get; set; }
            public bool CancelOnIonDisable { get; set; } = true;
            public bool IsComplete => _complete || CancelOnIonDisable && _caster.Modifiers.IsIonDisabled;
            public bool SurvivesCasterDeath { get; set; }
            public bool SurvivesHazardSuppression { get; set; }

            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) => _caster = caster;
            public void Advance(float deltaTime)
            {
                AdvanceCount++;
                _complete = CompleteOnAdvance;
            }
            public void Stop() => StopCount++;
        }

        private sealed class RecordingCompletableAbility : ICompletableShipAbility
        {
            public bool IsComplete { get; set; }
            public int StopCount { get; private set; }

            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) => IsComplete = false;

            public void Stop() => StopCount++;
        }

        private sealed class IonCannonStub : IHardPointModel, IReadOnlyHardPointModel
        {
            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
            public event Action OnDestroyedByDamage { add { } remove { } }
            public bool IsInstalled => true;
            public float HullDamageMultiplier => 1f;
            public HardPointType HardPointType => HardPointType.IonPulseCannon;
            public float HealthPercentage => 1f;
            public int Id { get; set; }
            public int Generation => 0;
            public bool IsDestroyed => false;
            public Vector3 Position => Transform == null ? Vector3.right : Transform.position;
            public Transform Transform { get; set; }
            public Transform Pivot => null;
        }

        private sealed class FakeCommand : IShipAbilityFacade, ICombatModifiersFacade, IAbilityFacingFacade, IHangarCommand,
            IAttackFacade, IUnitOrderObserverFacade
        {
            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            private readonly IEntity _entity;
            public bool Despawned { get; set; }
            public IEntity Entity => Despawned ? throw new InvalidOperationException("Not entity found") : _entity;
            public long EntityId => _entity.Id;
            public FakeHealth FakeHealth { get; }
            public IHealthModelObserver Health => FakeHealth;
            public float RadarRange => 20f;
            public float NavigationRadius => 0f;
            public float PaceSpeed => 30f;
            public UnitOrderType CurrentOrder { get; set; }
            public IEntity AttackTarget { get; private set; }
            public bool IsEngagingEnRoute => false;
            public int AttackCount { get; private set; }
            public void Attack(IEntity target, Vector3 formationOffset, PaceCluster pace = null)
            {
                AttackCount++;
                AttackTarget = target;
                CurrentOrder = UnitOrderType.Attack;
            }

            public bool IsAbilityFacing { get; private set; }
            public void BeginAbilityFacing() => IsAbilityFacing = true;
            public void FaceAbility(Vector3 direction) { }
            public void EndAbilityFacing() => IsAbilityFacing = false;

            public bool CanLaunchReplacement { get; set; }
            public bool CanRequestReplacement => CanLaunchReplacement;
            public bool TryLaunchReplacement()
            {
                if (!CanLaunchReplacement) return false;
                CanLaunchReplacement = false;
                return true;
            }
            public ISquadron Launch(SquadronType type) => throw new NotSupportedException();
            public bool CanLaunchImmediately => throw new NotSupportedException();
            public bool TryLaunchImmediately() => throw new NotSupportedException();

            public FakeCommand(ShipAbilityDefinition definition, Transform transform = null,
                ShipAbilityId id = ShipAbilityId.BoostEnginePower, long entityId = 1)
            {
                FakeHealth = new FakeHealth(transform);
                _entity = new FakeEntity(command: this, owner: TestPlayers.Human, health: FakeHealth, id: entityId);
                Slots = new[] { new ShipAbilitySlot(id: id,
                    definition: definition, owner: this) };
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IShipAbilityFacade _command;
            public IVisionFootprintFacade Footprint { get; set; }

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public FakeEntity(IShipAbilityFacade command, IHealthModelObserver health,
                PlayerId owner, long id = 1)
            {
                Id = id;
                _command = command;
                Owner = owner;
                HealthModel = health;
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityFacade
            { if (HealthModel is TCommand transformFacade) { entityCommand = transformFacade; return true; }
                if (Footprint is TCommand footprint) { entityCommand = footprint; return true; }
                if (_command is TCommand matching)
                {
                    entityCommand = matching;
                    return true;
                }
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade, IHealthFacade,
            INavigationFootprintFacade
        {
            public float NavigationRadius { get; set; }
            public float DamageReceived { get; private set; }
            public DamageType LastDamageType { get; private set; }
            public void ApplyDamage(float damage, DamageType damageType, int id)
            {
                DamageReceived += damage;
                LastDamageType = damageType;
            }

            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            // Units that are also health hardpoints count for an ability's required subsystem (Ion Pulse cannon).
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OfType<IReadOnlyHardPointModel>(Units));
            public float Hull => 1f;
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => IsDestroyedValue;
            public bool IsDestroyedValue { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;
            public IHardPointModel[] Units { get; set; } = Array.Empty<IHardPointModel>();

            public FakeHealth(Transform transform) { Transform = transform; }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Units;
        }
    }
}
