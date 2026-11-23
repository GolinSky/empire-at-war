using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Ship;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Tests.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class ShipPursuitTests
    {
        private GameObject _targetObject;
        private Target _target;
        private Movement _movement;
        private Weapon _weapon;
        private UnitOrderSettings _orderSettings;

        [SetUp]
        public void SetUp()
        {
            _targetObject = new GameObject("Pursuit target");
            _targetObject.transform.position = new Vector3(500f, 0f, 0f);
            _target = new Target(_targetObject.transform);
            _movement = new Movement();
            _weapon = new Weapon();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_targetObject);
            if (_orderSettings != null) Object.DestroyImmediate(_orderSettings);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_CommitsLegDespiteTargetMotionAndOwnDetour(bool formation)
        {
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target, formation ? Vector3.forward * 20f : Vector3.zero,
                UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            Vector3 committed = _movement.Destination;

            _movement.CurrentPosition = new Vector3(100f, 0f, 200f);
            state.Tick(0.1f);
            _target.Transform.position = new Vector3(900f, 0f, 0f);
            for (int i = 0; i < 20; i++) state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            Assert.That(_movement.Destination, Is.EqualTo(committed));
            _movement.FinishLeg();
            state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));
            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position),
                Is.EqualTo(65f).Within(0.001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_FormationReclosesFromCurrentSideAfterHolding(bool artillery)
        {
            _target.Transform.position = Vector3.right * (artillery ? 180f : 70f);
            if (!artillery)
                Assert.That(ShipEngagement.IsInRange(_movement, _weapon, _target, _target.Transform.position),
                    Is.True, "The normal weapon must start inside its 80 m engagement range.");
            var state = new AttackTargetState(null, _weapon, _movement,
                artillery ? new ArtilleryCapability(200f) : null);
            state.SetData(_target, Vector3.right * 20f, UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            Assert.That(_movement.HeldTarget, Is.SameAs(_target));
            Assert.That(_movement.MoveCount, Is.Zero);

            _target.Transform.position = Vector3.right * 500f;
            state.Tick(0.1f);

            Vector3 destination = Vector3.right * (artillery ? 320f : 435f);
            Assert.That(_movement.Destination, Is.EqualTo(destination));
            Assert.That(_movement.LastPreserveCourse, Is.True);
            _movement.CurrentPosition = Vector3.forward * 100f;
            state.Tick(0.1f);
            Assert.That(_movement.Destination, Is.EqualTo(destination));
            Assert.That(_movement.MoveCount, Is.EqualTo(1));

            state.SetData(new Target(_target.Transform) { Id = 2 }, Vector3.right * 20f,
                UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            Assert.That(_movement.Destination.x, Is.GreaterThan(_target.Transform.position.x),
                "A new target must retain the initial assigned formation approach.");
        }

        [Test]
        public void Runner_SameTargetReorderedKeepsHoldAndOnlyUpdatesSlot()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            try
            {
                _target.Transform.position = Vector3.right * 95f;
                UnitOrderModel orders = new UnitOrderModel();
                ShipStateMachine machine = new ShipStateMachine();
                Radar radar = new Radar();
                AttackTargetState attack = new AttackTargetState(null, _weapon, _movement);
                ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                    new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats()), null, attack, null, null, null, null, abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
                runner.Start();
                runner.Attack(_target, Vector3.forward * 20f);
                runner.Tick(0.1f);
                Assert.That(_movement.HeldTarget, Is.SameAs(_target));

                runner.Attack(_target, Vector3.back * 20f);
                runner.Tick(0.1f);

                Assert.That(_movement.HeldTarget, Is.SameAs(_target), "The re-issued order must not cancel the hold.");
                Assert.That(_movement.MoveCount, Is.Zero);
                Assert.That(machine.CurrentState, Is.SameAs(attack));
                Assert.That(orders.Offset.Z, Is.EqualTo(-20f));
            }
            finally
            {
                abilities.Clear();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Runner_GroupAttackApproachesAtSlowestPaceUntilInRangeOrNewOrder(bool reachRange)
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            try
            {
                Radar radar = new Radar();
                AttackTargetState attack = new AttackTargetState(null, _weapon, _movement);
                ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, new UnitOrderModel(),
                    new ShipStateMachine(), new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats()), null, attack, null, null, null,
                    null, abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
                PaceCluster pace = new PaceCluster();
                float slower = _movement.PaceSpeed * 0.8f;
                pace.Join(slower);
                runner.Start();

                runner.Attack(_target, Vector3.zero, pace);
                runner.Tick(0.1f);

                Assert.That(_movement.IsMoving, Is.True);
                Assert.That(_movement.SpeedCap, Is.EqualTo(slower));

                if (reachRange)
                {
                    _movement.FinishLeg();
                    _movement.CurrentPosition = _target.Transform.position + Vector3.left * 50f;
                    runner.Tick(0.1f);
                    Assert.That(_movement.HeldTarget, Is.SameAs(_target));
                }
                else runner.Stop();

                Assert.That(_movement.SpeedCap, Is.EqualTo(float.PositiveInfinity));
                Assert.That(pace.Pace, Is.EqualTo(float.PositiveInfinity), "The ship must have left the group.");
            }
            finally
            {
                abilities.Clear();
            }
        }

        [Test]
        public void MovePace_LagCorrectionStuckReleaseAndDoubleClickDrop()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            try
            {
                Radar radar = new Radar();
                NavigateState navigate = new NavigateState(_movement);
                ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, new UnitOrderModel(),
                    new ShipStateMachine(), new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats()), navigate, null, null, null, null,
                    null, abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
                PaceClusterTuning tuning = PaceClusterTuning.Default;
                PaceCluster cluster = new PaceCluster(orderId: 1, issuePace: 20f, tuning: tuning);
                _movement.PaceSpeed = 30f;
                _movement.CurrentSpeed = 20f;
                Vector3 destination = new Vector3(1000f, 0f, 0f);
                // Partner is far behind so this ship leads by more than LeadTolerance.
                int partner = cluster.Join(20f, remaining: 1000f);
                _movement.CurrentPosition = new Vector3(500f, 0f, 0f);
                runner.Start();

                runner.MoveTo(destination, cluster);
                runner.Tick(0.1f);
                Assert.That(_movement.SpeedCap, Is.LessThan(20f), "Ship leading the cluster should slow down.");

                // Partner moves far ahead so this ship is behind and runs uncapped to catch up.
                cluster.Report(partner, 20f, remaining: 50f);
                runner.Tick(0.1f);
                Assert.That(_movement.SpeedCap, Is.EqualTo(float.PositiveInfinity), "Ship lagging behind should run uncapped.");

                // Re-issuing MoveTo the same destination with null pace (double right-click) drops pace immediately.
                cluster.Report(partner, 20f, remaining: 500f);
                runner.Tick(0.1f);
                Assert.That(_movement.SpeedCap, Is.EqualTo(20f));
                runner.MoveTo(destination, null);
                Assert.That(_movement.SpeedCap, Is.EqualTo(float.PositiveInfinity));
                Assert.That(cluster.MemberCount, Is.EqualTo(1));

                // Rejoin slow (turning/accelerating at the start of the order): not stuck, keeps the cluster pace.
                runner.MoveTo(destination, cluster);
                _movement.CurrentSpeed = 0f;
                runner.Tick(tuning.StuckDetachTime + 0.1f);
                Assert.That(_movement.SpeedCap, Is.EqualTo(20f), "A ship that has not started moving is not stuck.");
                Assert.That(cluster.MemberCount, Is.EqualTo(2));

                // Once moving, stopping again is stuck: releases cap immediately and detaches after StuckDetachTime.
                _movement.CurrentSpeed = 20f;
                runner.Tick(0.1f);
                _movement.CurrentSpeed = 0f;
                runner.Tick(1f);
                Assert.That(_movement.SpeedCap, Is.EqualTo(float.PositiveInfinity));
                Assert.That(cluster.MemberCount, Is.EqualTo(2));
                runner.Tick(tuning.StuckDetachTime + 0.1f);
                Assert.That(cluster.MemberCount, Is.EqualTo(1));
            }
            finally
            {
                abilities.Clear();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_TargetKilled_PicksInReachContactAndHoldsWhenItLeaves(bool formation)
        {
            GameObject deadObject = new GameObject("Dead enemy");
            GameObject fighterObject = new GameObject("Near fighter");
            GameObject capitalObject = new GameObject("Far capital");
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            try
            {
                deadObject.transform.position = new Vector3(0f, 0f, 150f);
                fighterObject.transform.position = new Vector3(0f, 0f, 80f);
                capitalObject.transform.position = new Vector3(0f, 0f, 900f);
                Target dead = new Target(deadObject.transform) { Id = 2 };
                dead.Health.IsDestroyed = true;
                Target fighter = new Target(fighterObject.transform) { Id = 3 };
                fighter.Health.ShipClass = ShipClass.Fighter;
                Target capital = new Target(capitalObject.transform) { Id = 4 };
                Radar radar = new Radar();
                radar.Enemies.Add(dead);
                radar.Enemies.Add(fighter);
                radar.Enemies.Add(capital);
                UnitOrderModel orders = new UnitOrderModel();
                ShipStateMachine machine = new ShipStateMachine();
                AttackTargetState attack = new AttackTargetState(null, _weapon, _movement);
                ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                    new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats()), null, attack, null, null, null, null, abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
                runner.Start();
                Vector3 offset = formation ? Vector3.forward * 20f : Vector3.zero;
                runner.Attack(_target, offset);

                _target.Health.IsDestroyed = true;
                runner.Tick(0.1f);

                Assert.That(orders.Current, Is.EqualTo(UnitOrderType.Attack));
                Assert.That(orders.Target, Is.SameAs(fighter));
                Assert.That(machine.CurrentState, Is.SameAs(attack));
                runner.Tick(0.1f);
                Assert.That(_movement.IsMoving, Is.False);

                fighterObject.transform.position = Vector3.forward * 300f;
                runner.Tick(0.1f);
                Assert.That(orders.Current, Is.EqualTo(UnitOrderType.None));
                Assert.That(_movement.IsMoving, Is.False);
            }
            finally
            {
                abilities.Clear();
                Object.DestroyImmediate(deadObject);
                Object.DestroyImmediate(fighterObject);
                Object.DestroyImmediate(capitalObject);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Attack_StalledApproachRetriesAlternateBearingsWithoutPreservingCourse(bool formation, bool failsImmediately)
        {
            _movement.FailMoves = failsImmediately;
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target, formation ? Vector3.forward * 20f : Vector3.zero,
                UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            _movement.Stop();
            _movement.FailMoves = true;
            state.Tick(0.25f);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));

            for (int i = 0; i < 4; i++)
            {
                state.Tick(i == 0 ? 0.25f : 0.5f);
                float angle = 20f * (i / 2 + 1) * (i % 2 == 0 ? 1f : -1f);
                Vector3 bearing = formation ? Vector3.forward : Vector3.left;
                Vector3 expected = _target.Transform.position + Quaternion.AngleAxis(angle, Vector3.up) * bearing * 65f;
                Assert.That(Vector3.Distance(_movement.Destination, expected), Is.LessThan(0.001f));
                Assert.That(_movement.LastPreserveCourse, Is.False);
                Assert.That(_movement.MoveCount, Is.EqualTo(i + 2));
                Assert.That(state.IsComplete, Is.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_ProgressResetsStalledBearingAttempts(bool formation)
        {
            _movement.FailMoves = true;
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target, formation ? Vector3.forward * 20f : Vector3.zero,
                UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            state.Tick(0.5f);
            state.Tick(0.5f);
            _movement.FailMoves = false;
            state.Tick(0.5f);
            _movement.CurrentPosition = new Vector3(100f, 0f, 0f);
            state.Tick(0.1f);
            _movement.Stop();
            _movement.FailMoves = true;
            state.Tick(0.1f);
            state.Tick(0.5f);

            Vector3 bearing = formation ? Vector3.forward : Vector3.left;
            Vector3 expected = _target.Transform.position + Quaternion.AngleAxis(20f, Vector3.up) * bearing * 65f;
            Assert.That(Vector3.Distance(_movement.Destination, expected), Is.LessThan(0.001f));
            Assert.That(_movement.LastPreserveCourse, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_NewTargetOrExitResetsStalledBearingAttempts(bool exit)
        {
            _movement.FailMoves = true;
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target);
            state.Tick(0.1f);
            state.Tick(0.5f);
            state.Tick(0.5f);

            if (exit) state.Exit();
            else state.SetData(new Target(_target.Transform) { Id = 2 });
            state.Tick(0.1f);
            state.Tick(0.5f);

            Vector3 expected = _target.Transform.position + Quaternion.AngleAxis(20f, Vector3.up) * Vector3.left * 65f;
            Assert.That(Vector3.Distance(_movement.Destination, expected), Is.LessThan(0.001f));
            Assert.That(_movement.LastPreserveCourse, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_StalledShipInRangeDoesNotRetry(bool formation)
        {
            _movement.FailMoves = true;
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target, formation ? Vector3.forward * 20f : Vector3.zero,
                UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            _target.Transform.position = new Vector3(70f, 0f, 0f);
            state.Tick(0.5f);
            state.Tick(0.5f);

            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            Assert.That(_movement.IsMoving, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_ClosesInsideOversizedHullUntilAWeaponReaches(bool formation)
        {
            _target.Health.ShipClass = ShipClass.BattleCruiser;
            _target.HasHealthFacade = true;
            _target.NavigationRadius = 550f;
            _weapon.HardPointApproach = Vector3.right * 200f;
            AttackTargetState state = new AttackTargetState(new AttackDataFactory(), _weapon, _movement,
                new ArtilleryCapability(100f));
            state.SetData(_target, formation ? Vector3.forward * 20f : Vector3.zero,
                UnitOrderModel.NO_HARD_POINT);

            state.Enter();

            Assert.That(_movement.IsMoving, Is.True);
            Assert.That(_movement.Destination, Is.EqualTo(Vector3.right * 200f));
            _movement.CurrentPosition = Vector3.right * 180f;
            _weapon.HardPointApproach = Vector3.zero;
            state.Tick(0.1f);
            Assert.That(_movement.IsMoving, Is.False);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));

            _weapon.HardPointApproach = Vector3.right * 50f;
            state.Tick(0.1f);
            Assert.That(_movement.Destination, Is.EqualTo(Vector3.right * 230f));
        }

        [Test]
        public void Attack_HardPointScanIsBoundedWhileMovingAndStillDetectsRangeEntry()
        {
            AttackTargetState state = StartOversizedHullApproach();
            _weapon.HardPointApproach = Vector3.zero;

            for (int i = 0; i < 4; i++) state.Tick(0.02f);

            Assert.That(_weapon.HardPointApproachCalls, Is.EqualTo(1));
            Assert.That(_movement.IsMoving, Is.True);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            state.Tick(0.021f);
            Assert.That(_weapon.HardPointApproachCalls, Is.EqualTo(2));
            Assert.That(_movement.IsMoving, Is.False);
        }

        [Test]
        public void Attack_HardPointApproachArrivalBypassesScanInterval()
        {
            AttackTargetState state = StartOversizedHullApproach();
            _movement.FinishLeg();
            _weapon.HardPointApproach = Vector3.zero;

            state.Tick(0.001f);

            Assert.That(_weapon.HardPointApproachCalls, Is.EqualTo(2));
            Assert.That(_movement.IsMoving, Is.False);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_HardPointScanStaysBoundedWhileTargetMoves(bool rotation)
        {
            AttackTargetState state = StartOversizedHullApproach();
            _weapon.HardPointApproach = Vector3.zero;

            for (int i = 0; i < 4; i++)
            {
                if (rotation) _target.Transform.rotation *= Quaternion.Euler(0f, 5f, 0f);
                else _target.Transform.position += Vector3.right;
                state.Tick(0.02f);
            }

            Assert.That(_weapon.HardPointApproachCalls, Is.EqualTo(1),
                "A moving target must not force a full mount scan on every frame of a committed leg.");
            Assert.That(_movement.IsMoving, Is.True);
            state.Tick(0.021f);
            Assert.That(_weapon.HardPointApproachCalls, Is.EqualTo(2));
            Assert.That(_movement.IsMoving, Is.False, "Mount-range entry is still detected within one interval.");
        }

        [Test]
        public void Attack_NewHardPointTargetBypassesScanInterval()
        {
            AttackTargetState state = StartOversizedHullApproach();
            var next = new Target(_target.Transform) { Id = 2, HasHealthFacade = true, NavigationRadius = 550f };
            next.Health.ShipClass = ShipClass.BattleCruiser;
            _weapon.HardPointApproach = Vector3.zero;

            state.SetData(next);
            state.Enter();

            Assert.That(_weapon.HardPointApproachCalls, Is.EqualTo(2));
            Assert.That(_movement.IsMoving, Is.False);
        }

        private AttackTargetState StartOversizedHullApproach()
        {
            _target.Health.ShipClass = ShipClass.BattleCruiser;
            _target.HasHealthFacade = true;
            _target.NavigationRadius = 550f;
            _weapon.HardPointApproach = Vector3.right * 200f;
            var state = new AttackTargetState(new AttackDataFactory(), _weapon, _movement);
            state.SetData(_target);
            state.Enter();
            return state;
        }

        [Test]
        public void Attack_TargetKilled_NoRadarContactStaysInPlace()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            Radar radar = new Radar();
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            IdleState idle = new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats());
            AttackMoveState attackMove = new AttackMoveState(_movement, _weapon, radar, null, new FakeShipThreats());
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                idle, null, new AttackTargetState(null, _weapon, _movement), attackMove, null, null, null, abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
            runner.Start();
            runner.Attack(_target, Vector3.zero);

            _target.Health.IsDestroyed = true;
            runner.Tick(0.1f);

            Assert.That(orders.Current, Is.EqualTo(UnitOrderType.None));
            Assert.That(machine.CurrentState, Is.SameAs(idle));
            Assert.That(_movement.IsMoving, Is.False);
            abilities.Clear();
        }

        [Test]
        public void FindNearestEnemy_PrefersThePreviousTargetClassOverANearerContact()
        {
            GameObject nearObject = new GameObject("Near frigate");
            GameObject farObject = new GameObject("Far corvette");
            try
            {
                nearObject.transform.position = new Vector3(0f, 0f, 200f);
                farObject.transform.position = new Vector3(0f, 0f, 800f);
                Target near = new Target(nearObject.transform) { Id = 2 };
                near.Health.ShipClass = ShipClass.Frigate;
                Target far = new Target(farObject.transform) { Id = 3 };
                far.Health.ShipClass = ShipClass.Corvette;
                IEntity[] candidates = { near, far };

                Assert.That(ShipEngagement.FindNearestEnemy(candidates, _movement, _weapon), Is.SameAs(near));
                Assert.That(ShipEngagement.FindNearestEnemy(candidates, _movement, _weapon, ShipClass.Corvette),
                    Is.SameAs(far));
            }
            finally
            {
                Object.DestroyImmediate(nearObject);
                Object.DestroyImmediate(farObject);
            }
        }

        [Test]
        public void Attack_TargetClosesRange_StopsWithoutAnotherPlan()
        {
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target);
            state.Tick(0.1f);
            _target.Transform.position = new Vector3(95f, 0f, 0f);
            state.Tick(0.1f);

            Assert.That(_movement.IsMoving, Is.False);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            Assert.That(_movement.LookTarget, Is.SameAs(_target));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_ArtilleryUsesLauncherRingOnlyForApproach(bool formation)
        {
            ArtilleryCapability artillery = new ArtilleryCapability(200f);
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement, artillery);
            Vector3 offset = formation ? Vector3.forward * 20f : Vector3.zero;
            state.SetData(_target, offset, UnitOrderModel.NO_HARD_POINT);

            state.Tick(0.1f);

            Vector3 bearing = formation ? Vector3.forward : Vector3.left;
            Assert.That(_movement.Destination, Is.EqualTo(_target.Transform.position + bearing * 180f));
            _movement.CurrentPosition = _movement.Destination;
            _target.Transform.position -= bearing * 18f;
            for (int i = 0; i < 10; i++) state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            Assert.That(_movement.IsMoving, Is.False);

            _target.Transform.position += bearing * 40f;
            state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));

            _target.Transform.position -= bearing * 100f;
            state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(2));
            Assert.That(_movement.Destination, Is.EqualTo(_target.Transform.position + bearing * 180f));
        }

        [Test]
        public void Attack_HoldsAnywhereInReachAndResumesOnlyBeyondIt()
        {
            _target.Transform.position = Vector3.right * 95f;
            Assert.That(ShipEngagement.IsInRange(_movement, _weapon, _target, _target.Transform.position), Is.False,
                "95 m is outside the 80 m pursuit stop distance but inside the 100 m reach.");
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target);

            state.Tick(0.1f);

            Assert.That(_movement.HeldTarget, Is.SameAs(_target));
            Assert.That(_movement.MoveCount, Is.Zero);

            _target.Transform.position = Vector3.right * 150f;
            state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(1));
        }

        [Test]
        public void Attack_SameTargetNewSlotKeepsHold()
        {
            _target.Transform.position = Vector3.right * 95f;
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target, Vector3.forward * 20f, UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);

            state.SetData(_target, Vector3.back * 20f, UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.Zero);
            Assert.That(_movement.HeldTarget, Is.SameAs(_target));
        }

        [Test]
        public void Attack_ArtilleryRangeRevisionReplacesCommittedLegAndLastLauncherLossRestoresOrdinaryAttack()
        {
            ArtilleryCapability artillery = new ArtilleryCapability(200f);
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement, artillery);
            state.SetData(_target);
            state.Tick(0.1f);

            artillery.SetReach(60f);
            state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(2));
            Assert.That(_movement.Destination.x, Is.EqualTo(446f).Within(0.001f));

            artillery.SetReach(0f);
            state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(3));
            Assert.That(_movement.Destination.x, Is.EqualTo(435f).Within(0.001f));
        }

        [Test]
        public void Attack_ArtilleryBlockedApproachRetriesAtIntervalsAndWaitsForTargetToMove()
        {
            ArtilleryCapability artillery = new ArtilleryCapability(200f);
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement, artillery);
            _movement.FailMoves = true;
            state.SetData(_target);
            state.Tick(0.1f);
            Vector3 firstDestination = _movement.Destination;

            state.Tick(0.25f);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            state.Tick(0.25f);
            Assert.That(_movement.Destination, Is.Not.EqualTo(firstDestination));
            Assert.That(_movement.LastPreserveCourse, Is.False);
            for (int i = 0; i < 20; i++) state.Tick(0.5f);
            Assert.That(_movement.MoveCount, Is.EqualTo(7));

            _target.Transform.position += Vector3.right * 25f;
            state.Tick(0.5f);

            Assert.That(_movement.MoveCount, Is.EqualTo(8));
            Assert.That(_movement.Destination.x, Is.EqualTo(345f).Within(0.001f));
        }

        [TestCase(100f, 150f, 200f)]
        [TestCase(199f, 0f, 200f)]
        [TestCase(95f, 0f, 60f)]
        public void Attack_ArtilleryHoldsAtAnyFiringReach(float distance, float hullRadius, float launcherRange)
        {
            ArtilleryCapability artillery = new ArtilleryCapability(launcherRange);
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement, artillery);
            _target.Transform.position = Vector3.right * distance;
            _target.HullRadius = hullRadius;
            state.SetData(_target);

            state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(0));
            Assert.That(_movement.LookTarget, Is.SameAs(_target));
        }

        [TestCase(185f, 190f)]
        [TestCase(205f, 210f)]
        public void Attack_ArtilleryUsesContactFloorAndOrdinaryEngagementWhenLauncherCannotReach(
            float targetHullRadius, float expectedDistance)
        {
            ArtilleryCapability artillery = new ArtilleryCapability(200f);
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement, artillery);
            // This target models a mobile ship here, so the navigation radius is its live footprint.
            _target.NavigationRadius = targetHullRadius;
            _target.HasStationHull = false;
            state.SetData(_target);

            state.Tick(0.1f);
            _movement.FinishLeg();
            state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(_movement.CurrentPosition, _target.Transform.position),
                Is.EqualTo(expectedDistance).Within(0.001f));
        }

        [Test]
        public void SharedPursuit_NewTargetReplacesCourse_ThenWaitsForArrival()
        {
            _movement.MoveToPosition(Vector3.back * 100f);
            ShipEngagement.Pursue(_movement, _weapon, _target, newTarget: true);
            Assert.That(_movement.Destination.x, Is.EqualTo(435f));
            _target.Transform.position = new Vector3(900f, 0f, 0f);
            for (int i = 0; i < 20; i++) ShipEngagement.Pursue(_movement, _weapon, _target);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));

            _movement.FinishLeg();
            ShipEngagement.Pursue(_movement, _weapon, _target);
            Assert.That(_movement.MoveCount, Is.EqualTo(3));
            Assert.That(_movement.Destination.x, Is.EqualTo(835f));
        }

        [Test]
        public void SharedPursuit_StandoffRespectsTargetHull()
        {
            _target.NavigationRadius = 100f;
            ShipEngagement.Pursue(_movement, _weapon, _target);
            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position),
                Is.EqualTo(105f));
        }

        [TestCase(0f, 40f, 65f)]
        [TestCase(100f, 40f, 140f)]
        [TestCase(500f, 5f, 505f)]
        public void Attack_FormationSlotUsesBearingAndStopsAtStandoff(
            float targetRadius, float shipRadius, float expectedDistance)
        {
            Vector3 slot = new Vector3(20f, 0f, 40f);
            _target.Transform.position = new Vector3(5000f, 0f, 0f);
            _target.NavigationRadius = targetRadius;
            var movement = new Movement { NavigationRadius = shipRadius };
            var state = new AttackTargetState(null, _weapon, movement);
            state.SetData(_target, slot, UnitOrderModel.NO_HARD_POINT);

            state.Tick(0.1f);

            Vector3 expected = _target.Transform.position + slot.normalized * expectedDistance;
            Assert.That(Vector3.Distance(movement.Destination, expected), Is.LessThan(0.001f));
        }

        [Test]
        public void Attack_FormationSlotUsesLiveContactClearanceAfterTargetReplacement()
        {
            var state = new AttackTargetState(null, _weapon, _movement);
            Vector3 offset = Vector3.forward * 20f;
            state.SetData(_target, offset, UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            _movement.Stop();
            var replacement = new Target(_target.Transform) { Id = 2, NavigationRadius = 200f };
            state.SetData(replacement, offset, UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            Assert.That(_movement.Destination, Is.EqualTo(replacement.Transform.position + Vector3.forward * 205f));
        }

        [Test]
        public void SharedPursuit_CoincidentCentersUseStableNonzeroStandoff()
        {
            _movement.CurrentPosition = _target.Transform.position;
            Vector3 first = ShipEngagement.GetStandoffPosition(_movement, _weapon, _target);
            Vector3 second = ShipEngagement.GetStandoffPosition(_movement, _weapon, _target);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(Vector3.Distance(first, _target.Transform.position), Is.EqualTo(65f).Within(0.001f));
        }

        [Test]
        public void SharedPursuit_FormationBearingSurvivesOwnDetour()
        {
            Vector3 offset = Vector3.forward * 20f;
            Vector3 first = ShipEngagement.GetStandoffPosition(_movement, _weapon, _target, offset);
            _movement.CurrentPosition = new Vector3(900f, 0f, 100f);
            Assert.That(ShipEngagement.GetStandoffPosition(_movement, _weapon, _target, offset), Is.EqualTo(first));
            Assert.That(first, Is.EqualTo(_target.Transform.position + Vector3.forward * 65f));
        }

        [TestCase(true, 321f)]
        [TestCase(false, 500f)]
        public void SharedPursuit_ContactLimitedSiegeUsesLiveHullAndTighterGate(bool siegeApproach, float expected)
        {
            _weapon.UseSiegeApproach = siegeApproach;
            _movement.NavigationRadius = 300f;
            _target.NavigationRadius = 200f;
            _target.HullRadius = 20f;

            Assert.That(ShipEngagement.CanEngage(_target, _movement, _weapon), Is.EqualTo(!siegeApproach));
            ShipEngagement.Pursue(_movement, _weapon, _target, newTarget: true);
            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position),
                Is.EqualTo(expected).Within(0.001f));

            _movement.FinishLeg();
            Assert.That(ShipEngagement.CanEngage(_target, _movement, _weapon), Is.True);
        }

        [Test]
        public void SharedPursuit_SiegeWithNominalReachPreservesNormalStandoff()
        {
            _weapon.UseSiegeApproach = true;
            _target.NavigationRadius = 200f;
            _target.HullRadius = 20f;

            ShipEngagement.Pursue(_movement, _weapon, _target, newTarget: true);

            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position),
                Is.EqualTo(205f).Within(0.001f));
        }

        [Test]
        public void Guard_MovingFriendlyDoesNotReplaceFollowLeg()
        {
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            try
            {
                GuardState state = new GuardState(_movement, _weapon, new Radar(), null, settings, new FakeShipThreats());
                state.SetData(_target, Vector3.zero);
                state.Enter();
                _target.Transform.position = new Vector3(900f, 0f, 0f);
                for (int i = 0; i < 20; i++) state.Tick(0.1f);
                Assert.That(_movement.MoveCount, Is.EqualTo(1));

                _movement.FinishLeg();
                state.Tick(0.1f);
                Assert.That(_movement.MoveCount, Is.EqualTo(2));
                Assert.That(_movement.Destination, Is.EqualTo(_target.Transform.position));
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void AbilityApproach_MovingTargetDoesNotReplaceLeg()
        {
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            _target.Transform.position = new Vector3(900f, 0f, 0f);
            for (int i = 0; i < 20; i++) state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));

            _movement.FinishLeg();
            state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));
        }

        [Test]
        public void AbilityApproach_UsesRangeStandoff()
        {
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            Assert.That(_movement.Destination.x, Is.EqualTo(410f));
            _movement.FinishLeg();
            Assert.That(state.IsInRange, Is.True);
            Assert.That(state.IsComplete, Is.True);
        }

        [Test]
        public void AbilityApproach_FailedPathRetriesThenCompletesWithoutCasting()
        {
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            for (int i = 0; i < 3; i++)
            {
                _movement.Stop();
                state.Tick(0.5f);
            }
            Assert.That(_movement.MoveCount, Is.EqualTo(3));
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.IsInRange, Is.False);
        }

        [Test]
        public void AbilityApproach_HullContactCountsAsInRangeBeyondCastDistance()
        {
            _target.NavigationRadius = 120f;
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            Assert.That(state.IsInRange, Is.False);

            _movement.FinishLeg();
            state.Tick(0.5f);

            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.IsInRange, Is.True);
            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position), Is.EqualTo(125f));
        }

        [Test]
        public void AbilityApproach_FailureResumesRemainingWaypoints()
        {
            UnitOrderModel orders = new UnitOrderModel();
            var first = new FormationPoint(10f, 20f);
            var second = new FormationPoint(30f, 40f);
            orders.Replace(UnitOrderType.WaypointMove, first, waypoints: new[] { first, second });
            orders.AdvanceWaypoint(out _);
            ShipStateMachine machine = new ShipStateMachine();
            IdleState idle = new IdleState(_movement, _weapon, new Radar(), new AttackDataFactory(), new FakeShipThreats());
            machine.SetState(idle);
            NavigateState navigate = new NavigateState(_movement);
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                idle, navigate, null, null, null, null, new AbilityApproachState(_movement), null,
                new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower), new Radar(), null, new NoTeamContacts(), TestPlayers.Human);

            runner.CastAbility(ShipAbilityId.TractorBeam, _target, 100f);
            Assert.That(runner.CurrentOrder, Is.EqualTo(UnitOrderType.UseAbility));
            for (int i = 0; i < 3; i++)
            {
                _movement.Stop();
                runner.Tick(0.5f);
            }

            Assert.That(runner.CurrentOrder, Is.EqualTo(UnitOrderType.WaypointMove));
            Assert.That(orders.WaypointIndex, Is.EqualTo(1));
            Assert.That(machine.CurrentState, Is.SameAs(navigate));
            Assert.That(_movement.Destination, Is.EqualTo(new Vector3(30f, 0f, 40f)));
        }

        [Test]
        public void AttackMove_ArrivedShip_DoesNotFinishBeforeItsFirstTargetSearch()
        {
            // Seeded so the random first delay is above zero; edit-mode time does not advance between ticks.
            UnityEngine.Random.InitState(7);
            AttackMoveState state = new AttackMoveState(_movement, _weapon, new Radar(), null, new FakeShipThreats());
            state.SetData(Vector3.zero, new AttackMoveEngagement());
            state.Enter();
            _movement.FinishLeg();

            state.Tick(0.1f);

            Assert.That(state.IsComplete, Is.False);
            state.Exit();
        }

        [Test]
        public void AttackMove_EnRouteShipIgnoresContactsUntilItArrives()
        {
            Radar radar = new Radar();
            radar.Enemies.Add(_target);
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            AttackMoveState state = new AttackMoveState(_movement, _weapon, radar, null, new FakeShipThreats());
            state.SetData(Vector3.forward * 100f, engagement);
            state.Enter();

            state.Tick(0.1f);
            Assert.That(engagement.Contains(_target), Is.False);
            Assert.That(_movement.Destination, Is.EqualTo(Vector3.forward * 100f));

            _movement.FinishLeg();
            state.Tick(0.1f);
            Assert.That(engagement.Contains(_target), Is.True);
            state.Exit();
        }

        [Test]
        public void AttackMove_ArrivedShipDoesNotChaseEnemiesBeyondReachOfItsPoint()
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            _target.Transform.position = Vector3.right * 300f;
            Radar radar = new Radar();
            radar.Enemies.Add(_target);
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            AttackMoveState state = new AttackMoveState(_movement, _weapon, radar, null, new FakeShipThreats());
            state.SetData(Vector3.zero, engagement);
            state.Enter();
            _movement.FinishLeg();
            int moves = _movement.MoveCount;
            typeof(AttackMoveState).GetField("_nextSearchTime", PRIVATE_INSTANCE)
                .SetValue(state, float.NegativeInfinity);

            state.Tick(0.1f);

            Assert.That(engagement.Contains(_target), Is.True);
            Assert.That(state.IsEngaging, Is.False);
            Assert.That(_movement.MoveCount, Is.EqualTo(moves));
            Assert.That(state.IsComplete, Is.True);
            state.Exit();
        }

        [Test]
        public void AttackMove_LostTargetSearchesBeforeReturningToThePoint()
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            // Seeded so the random search delay is above zero; edit-mode time does not advance between ticks.
            UnityEngine.Random.InitState(7);
            _target.Transform.position = Vector3.right * 90f;
            Radar radar = new Radar();
            radar.Enemies.Add(_target);
            AttackMoveState state = new AttackMoveState(_movement, _weapon, radar, null, new FakeShipThreats());
            state.SetData(Vector3.zero, new AttackMoveEngagement());
            state.Enter();
            _movement.FinishLeg();
            typeof(AttackMoveState).GetField("_engagementTarget", PRIVATE_INSTANCE).SetValue(state, _target);
            state.Tick(0.1f);
            _movement.CurrentPosition = Vector3.right * 50f;
            int moves = _movement.MoveCount;

            radar.Enemies.Remove(_target);
            state.Tick(0.1f);
            Assert.That(state.IsEngaging, Is.False);
            Assert.That(_movement.MoveCount, Is.EqualTo(moves), "No return before the next target search.");

            typeof(AttackMoveState).GetField("_nextSearchTime", PRIVATE_INSTANCE)
                .SetValue(state, float.NegativeInfinity);
            state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(moves + 1));
            Assert.That(_movement.Destination, Is.EqualTo(Vector3.zero));
            state.Exit();
        }

        [Test]
        public void AttackMove_AfterAKillPicksTheNextTargetWithoutHeadingBackFirst()
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            GameObject nextObject = new GameObject("Next enemy");
            try
            {
                _target.Transform.position = Vector3.right * 50f;
                nextObject.transform.position = Vector3.forward * 60f;
                Target next = new Target(nextObject.transform) { Id = 2 };
                Radar radar = new Radar();
                radar.Enemies.Add(_target);
                radar.Enemies.Add(next);
                AttackMoveState state = new AttackMoveState(_movement, _weapon, radar, new AttackDataFactory(),
                    new FakeShipThreats());
                state.SetData(Vector3.zero, new AttackMoveEngagement());
                state.Enter();
                _movement.FinishLeg();
                typeof(AttackMoveState).GetField("_engagementTarget", PRIVATE_INSTANCE).SetValue(state, _target);
                state.Tick(0.1f);
                Assert.That(_movement.HeldTarget, Is.SameAs(_target));
                int moves = _movement.MoveCount;

                _target.Health.IsDestroyed = true;
                state.Tick(0.1f);
                typeof(AttackMoveState).GetField("_nextSearchTime", PRIVATE_INSTANCE)
                    .SetValue(state, float.NegativeInfinity);
                state.Tick(0.1f);

                Assert.That(state.IsEngaging, Is.True);
                Assert.That(_movement.HeldTarget, Is.SameAs(next));
                Assert.That(_movement.MoveCount, Is.EqualTo(moves), "No leg back to the point before the next target.");
                state.Exit();
            }
            finally { Object.DestroyImmediate(nextObject); }
        }

        [TestCase(ShipClass.Fighter, true, true, false)]
        [TestCase(ShipClass.Corvette, false, true, false)]
        [TestCase(ShipClass.Capital, true, false, false)]
        [TestCase(ShipClass.Corvette, true, true, true)]
        [TestCase(ShipClass.Capital, true, true, true)]
        public void AttackMove_EnRouteBreaksOffOnlyForAWarshipThatHitsIt(ShipClass shipClass, bool takesOrders,
            bool isAttacker, bool breaksOff)
        {
            _target.Transform.position = Vector3.right * 50f;
            _target.Health.ShipClass = shipClass;
            _target.HasOrders = takesOrders;
            Radar radar = new Radar();
            radar.Enemies.Add(_target);
            FakeShipThreats threats = new FakeShipThreats { WasDamagedRecently = true };
            if (isAttacker) threats.SelfAttackers.Add(_target);
            Vector3 point = Vector3.forward * 1000f;
            AttackMoveState state = new AttackMoveState(_movement, _weapon, radar, new AttackDataFactory(), threats);
            state.SetData(point, new AttackMoveEngagement());
            state.Enter();

            state.Tick(0.1f);

            Assert.That(state.IsEngagingEnRoute, Is.EqualTo(breaksOff));
            Assert.That(_movement.IsMoving, Is.EqualTo(!breaksOff), "Fly-first holds for everything else.");
            if (!breaksOff)
            {
                Assert.That(_movement.Destination, Is.EqualTo(point));
                state.Exit();
                return;
            }

            Assert.That(_movement.HeldTarget, Is.SameAs(_target));
            _target.Health.IsDestroyed = true;
            state.Tick(0.1f);
            Assert.That(state.IsEngagingEnRoute, Is.False);
            Assert.That(_movement.IsMoving, Is.True, "The flight resumes once the attacker is gone.");
            Assert.That(_movement.Destination, Is.EqualTo(point));
            state.Exit();
        }

        [Test]
        public void Attack_HeldShipKeepsHoldingInsideTheLeaveMargin()
        {
            _target.Transform.position = Vector3.right * 95f;
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target);
            state.Tick(0.1f);
            Assert.That(_movement.HeldTarget, Is.SameAs(_target));
            float reach = _weapon.GetEngagementRange(_target.Health.ShipClass);

            // Just beyond reach, still inside reach x the leave margin: no new approach.
            _target.Transform.position = Vector3.right *
                (reach * (1f + ShipEngagement.HELD_REACH_FACTOR) * 0.5f);
            for (int i = 0; i < 5; i++) state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.Zero);
            Assert.That(_movement.HeldTarget, Is.SameAs(_target));

            _target.Transform.position = Vector3.right * (reach * ShipEngagement.HELD_REACH_FACTOR + 5f);
            state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
        }

        [Test]
        public void Idle_StationAttackerOutranksANearerCorvetteAsMainTarget()
        {
            GameObject corvetteObject = new GameObject("Near corvette");
            try
            {
                corvetteObject.transform.position = Vector3.right * 30f;
                Target corvette = new Target(corvetteObject.transform) { Id = 2, HasOrders = true };
                corvette.Health.ShipClass = ShipClass.Corvette;
                _target.Transform.position = Vector3.forward * 90f;
                _target.Health.ShipClass = ShipClass.Frigate;
                Radar radar = new Radar();
                radar.Enemies.Add(corvette);
                radar.Enemies.Add(_target);
                FakeShipThreats threats = new FakeShipThreats();
                threats.StationAttackers.Add(_target);
                IdleState state = new IdleState(_movement, _weapon, radar, new AttackDataFactory(), threats);

                state.Tick(0.1f);

                Assert.That(_movement.LookTarget, Is.SameAs(_target));
                Assert.That(_movement.MoveCount, Is.Zero, "Idle never moves, not even to fight.");
                Assert.That(ShipEngagement.SelectThreat(new IEntity[] { corvette, _target }, _movement, _weapon,
                    new FakeShipThreats()), Is.SameAs(_target), "Without threats the bigger hull still wins.");
                threats.StationAttackers.Clear();
                threats.SelfAttackers.Add(corvette);
                Assert.That(ShipEngagement.SelectThreat(new IEntity[] { corvette, _target }, _movement, _weapon,
                    threats), Is.SameAs(corvette), "An attacker of this ship outranks a bigger hull.");
            }
            finally { Object.DestroyImmediate(corvetteObject); }
        }

        [Test]
        public void SelectThreat_ShipBuiltForShipsTakesAWarshipBeforeASquadronAttackingIt()
        {
            GameObject squadronObject = new GameObject("Bombers");
            try
            {
                squadronObject.transform.position = Vector3.right * 30f;
                Target squadron = new Target(squadronObject.transform) { Id = 2 };
                squadron.Health.ShipClass = ShipClass.Bomber;
                _target.Transform.position = Vector3.forward * 90f;
                _target.Health.ShipClass = ShipClass.Frigate;
                FakeShipThreats threats = new FakeShipThreats();
                threats.SelfAttackers.Add(squadron);
                threats.StationAttackers.Add(squadron);

                Assert.That(_weapon.PreferredTarget, Is.EqualTo(TargetPreference.Ships));
                Assert.That(ShipEngagement.SelectThreat(new IEntity[] { squadron, _target }, _movement, _weapon,
                    threats), Is.SameAs(_target), "Heavy guns leave squadrons to lasers while a ship is near.");
                Assert.That(ShipEngagement.Outranks(_target, squadron, _weapon, threats), Is.True);
                Assert.That(ShipEngagement.Outranks(squadron, _target, _weapon, threats), Is.False);
                Assert.That(ShipEngagement.SelectThreat(new IEntity[] { squadron }, _movement, _weapon, threats),
                    Is.SameAs(squadron), "Without a ship the squadron is still fought.");
            }
            finally { Object.DestroyImmediate(squadronObject); }
        }

        [Test]
        public void Idle_UnderFireTargetsATeamVisibleHostileInReachBeyondOwnRadar()
        {
            _target.Transform.position = Vector3.forward * 90f;
            FakeShipThreats threats = new FakeShipThreats { WasDamagedRecently = true };
            threats.TeamVisible.Add(_target);
            IdleState state = new IdleState(_movement, _weapon, new Radar(), new AttackDataFactory(), threats);

            state.Tick(0.1f);

            Assert.That(_movement.LookTarget, Is.SameAs(_target));
        }

        [Test]
        public void Hunt_NoVisibleEnemiesRemainsActiveAndStopsPreviousMovement()
        {
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            try
            {
                var players = TestPlayers.CreateDuel();
                HuntState state = new HuntState(_movement, _weapon, null, new EntityLocator(), null,
                    new VisionService(players), settings,
                    TestPlayers.Human);
                _movement.MoveToPosition(Vector3.forward * 100f);
                state.Enter();
                state.Tick(0.1f);
                Assert.That(state.IsComplete, Is.False);
                Assert.That(_movement.IsMoving, Is.False);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(3)]
        public void Hunt_DropsTargetAsSoonAsTeamVisionIsLost(int owner)
        {
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            try
            {
                var players = TestPlayers.CreateTeamGame();
                var vision = new VisionService(players);
                PlayerId side = new PlayerId(owner);
                if (owner == 3) _target.Owner = TestPlayers.Human;
                var locator = new EntityLocator();
                locator.AddEntity(_target);
                HuntState state = new HuntState(_movement, _weapon, null, locator, players, vision, settings, side);
                const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(HuntState).GetField("_target", PRIVATE_INSTANCE).SetValue(state, _target);
                typeof(HuntState).GetField("_retargetTimer", PRIVATE_INSTANCE).SetValue(state, 100f);
                _movement.MoveToPosition(_target.Transform.position);

                state.Tick(0.1f);

                Assert.That(typeof(HuntState).GetField("_target", PRIVATE_INSTANCE).GetValue(state), Is.Null);
                Assert.That(_movement.IsMoving, Is.False);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void AbilityApproach_SuccessfulCastResumesGuard()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            RecordingAbility ability = new RecordingAbility();
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(ability),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            _orderSettings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            GuardState guard = new GuardState(_movement, _weapon, new Radar(), null, _orderSettings, new FakeShipThreats());
            IdleState idle = new IdleState(_movement, _weapon, new Radar(), new AttackDataFactory(), new FakeShipThreats());
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                idle, null, null, null, guard, null, new AbilityApproachState(_movement), abilities, caster, new Radar(), null, new NoTeamContacts(), TestPlayers.Human);
            runner.Start();
            runner.Guard(_target, Vector3.forward * 20f);
            runner.CastAbility(ShipAbilityId.BoostEnginePower, _target, 100f);
            _movement.FinishLeg();

            runner.Tick(0.1f);

            Assert.That(ability.StartCount, Is.EqualTo(1));
            Assert.That(runner.CurrentOrder, Is.EqualTo(UnitOrderType.Guard));
            Assert.That(machine.CurrentState, Is.SameAs(guard));
            Assert.That(_movement.Destination, Is.EqualTo(_target.Transform.position + Vector3.forward * 20f));
            abilities.Clear();
        }

        [Test]
        public void Guard_RepeatedExplicitOrderCancelsCloak()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.Cloak);
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new CloakAbility()),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            _orderSettings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            GuardState guard = new GuardState(_movement, _weapon, new Radar(), null, _orderSettings, new FakeShipThreats());
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                new IdleState(_movement, _weapon, new Radar(), new AttackDataFactory(), new FakeShipThreats()), null, null, null, guard, null, null, abilities, caster,
                new Radar(), null, new NoTeamContacts(), TestPlayers.Human);
            runner.Start();
            runner.Guard(_target, Vector3.forward * 20f);
            Assert.That(abilities.TryActivate(caster, ShipAbilityId.Cloak, null), Is.True);
            Assert.That(caster.Modifiers.IsCloaked, Is.True);

            runner.Guard(_target, Vector3.forward * 20f);

            Assert.That(caster.Modifiers.IsCloaked, Is.False);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            abilities.Clear();
        }

        [Test]
        public void AttackMove_SameDestinationNewEngagementReplacesMembership()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var abilityRelations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                abilityRelations, new VisionService(abilityRelations, fogEnabled: false));
            Radar radar = new Radar();
            AttackMoveState attackMove = new AttackMoveState(_movement, _weapon, radar, null, new FakeShipThreats());
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, new UnitOrderModel(),
                new ShipStateMachine(), new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats()), null, null, attackMove,
                null, null, null, abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
            runner.Start();
            AttackMoveEngagement first = new AttackMoveEngagement();
            AttackMoveEngagement second = new AttackMoveEngagement();
            runner.AttackMoveTo(Vector3.forward * 100f, first);
            first.Report(0, new[] { _target });

            runner.AttackMoveTo(Vector3.forward * 100f, second);

            Assert.That(first.Contains(_target), Is.False);
            Assert.DoesNotThrow(() => second.Report(0, new[] { _target }));
            Assert.That(second.Contains(_target), Is.True);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));
            runner.Release();
        }

        [Test]
        public void AnchoredShip_RefusesMovementOrdersAndStillAttacks()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                relations, new VisionService(relations, fogEnabled: false));
            Radar radar = new Radar();
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            AttackTargetState attack = new AttackTargetState(null, _weapon, _movement);
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats()), null, attack, null, null, null,
                new AbilityApproachState(_movement), abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
            runner.Start();
            caster.Modifiers.AddMovementAnchor();

            runner.MoveTo(new Vector3(500f, 0f, 0f));
            runner.MoveAlong(new[] { new Vector3(500f, 0f, 0f) });
            runner.AttackMoveTo(new Vector3(500f, 0f, 0f), new AttackMoveEngagement());
            runner.Hunt();
            runner.Guard(_target, Vector3.zero);
            runner.CastAbility(ShipAbilityId.TractorBeam, _target, 100f);
            Assert.That(orders.Current, Is.EqualTo(UnitOrderType.None));
            Assert.That(runner.CurrentOrder, Is.EqualTo(UnitOrderType.None), "An anchored ship cannot approach to cast.");
            Assert.That(_movement.MoveCount, Is.Zero);

            runner.Attack(_target, Vector3.zero);
            Assert.That(orders.Current, Is.EqualTo(UnitOrderType.Attack));
            runner.MoveTo(new Vector3(500f, 0f, 0f));
            Assert.That(orders.Current, Is.EqualTo(UnitOrderType.Attack), "A refused order keeps the current one.");
            abilities.Clear();
        }

        [Test]
        public void AbilityFacing_NewOrdersWaitAndCastsAreRefusedUntilFacingEnds()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                relations, new VisionService(relations, fogEnabled: false));
            Radar radar = new Radar();
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            IdleState idle = new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats());
            AttackTargetState attack = new AttackTargetState(null, _weapon, _movement);
            AbilityApproachState approach = new AbilityApproachState(_movement);
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                idle, null, attack, null, null, null, approach, abilities, caster, radar, null,
                new NoTeamContacts(), TestPlayers.Human);
            runner.Start();
            runner.BeginAbilityFacing();

            runner.Attack(_target, Vector3.zero);
            runner.CastAbility(ShipAbilityId.TractorBeam, _target, 100f);

            Assert.That(runner.IsAbilityFacing, Is.True, "An order must not cancel an ability that steers the hull.");
            Assert.That(orders.Current, Is.EqualTo(UnitOrderType.Attack));
            Assert.That(machine.CurrentState, Is.SameAs(idle));

            runner.EndAbilityFacing();

            Assert.That(machine.CurrentState, Is.SameAs(attack), "The waiting order starts once facing ends.");
            abilities.Clear();
        }

        [Test]
        public void AnchoredShip_RetreatEndsTheGravityWellAndLeaves()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.Interdict);
            var relations = TestPlayers.CreateDuel();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new AnchorAbility()),
                relations, new VisionService(relations, fogEnabled: false));
            Radar radar = new Radar();
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                new IdleState(_movement, _weapon, radar, new AttackDataFactory(), new FakeShipThreats()), new NavigateState(_movement), null, null, null, null,
                new AbilityApproachState(_movement), abilities, caster, radar, null, new NoTeamContacts(), TestPlayers.Human);
            runner.Start();
            Assert.That(abilities.TryActivate(caster, ShipAbilityId.Interdict, null), Is.True);
            Assert.That(caster.Modifiers.IsAnchored, Is.True);

            runner.Retreat(new Vector3(500f, 0f, 0f));

            Assert.That(caster.Slots[0].State, Is.Not.EqualTo(ShipAbilityState.Active));
            Assert.That(caster.Modifiers.IsAnchored, Is.False);
            Assert.That(orders.Current, Is.EqualTo(UnitOrderType.Retreat));
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            abilities.Clear();
        }

        private sealed class AbilityCaster : IShipAbilityFacade
        {
            private readonly Movement _movement;
            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => _movement.CurrentPosition;
            public IEntity Entity { get; }
            public IHealthModelObserver Health => Entity.HealthModel;
            public float RadarRange => 100f;
            public AbilityCaster(IEntity entity, Movement movement, ShipAbilityId id)
            {
                Entity = entity;
                _movement = movement;
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                typeof(ShipAbilityDefinition).GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(definition, id == ShipAbilityId.Cloak
                        ? (ShipAbilitySettings)new CloakSettings() : new BoostEnginePowerSettings());
                typeof(ShipAbilityDefinition).GetField("canCancel", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(definition, true);
                Slots = new[] { new ShipAbilitySlot(this, definition, id) };
            }
        }

        [TestCase("attack")]
        [TestCase("formation")]
        [TestCase("minimum-range")]
        [TestCase("hardpoint")]
        [TestCase("artillery")]
        [TestCase("attack-move")]
        [TestCase("guard")]
        [TestCase("hunt")]
        public void CombatRangeHold_AllBranchesPreserveCorrectionAndCancelItOnExit(string branch)
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            _target.Transform.position = Vector3.right * 40f;
            _orderSettings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            Radar radar = new Radar();
            radar.Enemies.Add(_target);
            IBaseState state;
            if (branch == "attack-move")
            {
                _target.Transform.position = Vector3.right * 95f;
                var attackMove = new AttackMoveState(_movement, _weapon, radar, null, new FakeShipThreats());
                attackMove.SetData(Vector3.zero, new AttackMoveEngagement());
                attackMove.Enter();
                // Attack-move fights only once the ship has reached its point.
                _movement.FinishLeg();
                typeof(AttackMoveState).GetField("_engagementTarget", PRIVATE_INSTANCE).SetValue(attackMove, _target);
                state = attackMove;
            }
            else if (branch == "guard")
            {
                var guard = new GuardState(_movement, _weapon, radar, null, _orderSettings, new FakeShipThreats());
                guard.SetData(_target, Vector3.zero);
                typeof(GuardState).GetField("_engagementTarget", PRIVATE_INSTANCE).SetValue(guard, _target);
                state = guard;
            }
            else if (branch == "hunt")
            {
                var players = TestPlayers.CreateDuel();
                var vision = new VisionService(players);
                vision.Register(TestPlayers.Human, _target.Transform, 100f);
                var hunt = new HuntState(_movement, _weapon, null, new EntityLocator(), players,
                    vision, _orderSettings, TestPlayers.Human);
                typeof(HuntState).GetField("_target", PRIVATE_INSTANCE).SetValue(hunt, _target);
                typeof(HuntState).GetField("_retargetTimer", PRIVATE_INSTANCE).SetValue(hunt, 100f);
                state = hunt;
            }
            else
            {
                if (branch == "artillery") _target.Transform.position = Vector3.right * 180f;
                if (branch == "hardpoint")
                {
                    _target.Health.ShipClass = ShipClass.BattleCruiser;
                    _target.NavigationRadius = 150f;
                    _target.HasHealthFacade = true;
                }
                var attack = new AttackTargetState(new AttackDataFactory(), _weapon, _movement,
                    branch == "artillery" ? new ArtilleryCapability(200f) : null);
                attack.SetData(_target, branch == "formation" ? Vector3.forward * 20f : Vector3.zero,
                    UnitOrderModel.NO_HARD_POINT);
                if (branch == "minimum-range") _movement.MoveToPosition(Vector3.forward * 100f);
                attack.Enter();
                state = attack;
            }

            state.Tick(0.1f);
            Assert.That(_movement.HeldTarget, Is.SameAs(_target));
            Assert.That(_movement.LookTarget, Is.SameAs(_target));
            _movement.BeginSettling(Vector3.forward * 3f);
            int plans = _movement.MoveCount;
            for (int i = 0; i < 5; i++) state.Tick(0.1f);
            Assert.That(_movement.HeldTarget, Is.SameAs(_target));
            Assert.That(_movement.IsMoving, Is.True);
            Assert.That(_movement.Destination, Is.EqualTo(Vector3.forward * 3f));
            Assert.That(_movement.MoveCount, Is.EqualTo(plans));

            state.Exit();
            Assert.That(_movement.HeldTarget, Is.Null);
            Assert.That(_movement.IsMoving, Is.False);
        }

        [Test]
        public void Attack_ChangingTargetCancelsOldCorrectionBeforeNextTick()
        {
            _target.Transform.position = Vector3.right * 40f;
            var state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target);
            state.Tick(0.1f);
            _movement.BeginSettling(Vector3.forward * 3f);

            state.SetData(new Target(_target.Transform) { Id = 2 });

            Assert.That(_movement.HeldTarget, Is.Null);
            Assert.That(_movement.IsMoving, Is.False);
        }

        [Test]
        public void Guard_LosingTargetAtHomeCancelsCorrectionWithoutStartingFollow()
        {
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            _orderSettings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            _movement.CurrentPosition = _target.Transform.position;
            var radar = new Radar();
            radar.Enemies.Add(_target);
            var state = new GuardState(_movement, _weapon, radar, null, _orderSettings, new FakeShipThreats());
            state.SetData(_target, Vector3.zero);
            typeof(GuardState).GetField("_engagementTarget", PRIVATE_INSTANCE).SetValue(state, _target);
            state.Tick(0.1f);
            _movement.BeginSettling(_movement.CurrentPosition + Vector3.forward * 3f);
            int plans = _movement.MoveCount;

            radar.Enemies.Remove(_target);
            state.Tick(0.1f);

            Assert.That(_movement.HeldTarget, Is.Null);
            Assert.That(_movement.IsMoving, Is.False);
            Assert.That(_movement.MoveCount, Is.EqualTo(plans));
        }

        private sealed class AbilityFactory : IShipAbilityFactory
        {
            private readonly IShipAbility _ability;
            public AbilityFactory(IShipAbility ability) => _ability = ability;
            public IShipAbility Create(ShipAbilityDefinition definition) => _ability;
        }

        private sealed class AnchorAbility : IStoppableShipAbility
        {
            private IShipAbilityFacade _caster;
            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
            {
                _caster = caster;
                caster.Modifiers.AddMovementAnchor();
            }

            public void Stop()
            {
                if (_caster == null) return;
                _caster.Modifiers.RemoveMovementAnchor();
                _caster = null;
            }
        }

        private sealed class RecordingAbility : IShipAbility
        {
            public int StartCount { get; private set; }
            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) => StartCount++;
        }

        private sealed class Movement : IShipMovement
        {
            private IEntity _heldTarget;
            private bool _isSettling;
            public IEntity HeldTarget => _heldTarget;
            public IEntity LookTarget { get; private set; }
            public Vector3 CurrentPosition { get; set; }
            public Vector3 Destination { get; private set; }
            public bool IsMoving { get; private set; }
            public bool IsArriving { get; set; }
            public bool FailMoves { get; set; }
            public bool LastPreserveCourse { get; private set; }
            public float NavigationRadius { get; set; } = 5f;
            public int MoveCount { get; private set; }
            public float CurrentSpeed { get; set; } = 10f;
            public float PaceSpeed { get; set; } = 10f;
            public float UndamagedPaceSpeed { get; set; } = 10f;
            public float SpeedCap { get; private set; } = float.PositiveInfinity;
            public void SetSpeedCap(float cap) => SpeedCap = cap;
            public void MoveToPosition(Vector3 position, bool preserveCourse = false)
            {
                if (preserveCourse && _isSettling && IsMoving) return;
                CancelCombatHold();
                Destination = position; IsMoving = !FailMoves; LastPreserveCourse = preserveCourse; MoveCount++;
            }
            public void BeginSettling(Vector3 destination)
            { _isSettling = true; Destination = destination; IsMoving = true; MoveCount++; }
            public void FinishLeg() { CurrentPosition = Destination; IsMoving = false; }
            public void LookAtTarget(IEntity target) => LookTarget = target;
            public void LookInDirection(Vector3 direction) { }
            public float GetRange(Vector3 position) => Vector3.Distance(CurrentPosition, position);
            public void Stop() { CancelCombatHold(); IsMoving = false; }
            public void HoldPosition(IEntity target)
            {
                if (ReferenceEquals(_heldTarget, target)) return;
                Stop();
                _heldTarget = target;
            }
            public void CancelCombatHold()
            {
                if (_isSettling) IsMoving = false;
                _isSettling = false;
                _heldTarget = null;
            }
        }

        private sealed class Weapon : IWeaponComponent
        {
            public string Id => nameof(Weapon);
            public bool UseSiegeApproach { get; set; }
            public Vector3 HardPointApproach { get; set; }
            public int HardPointApproachCalls { get; private set; }
            public float AttackDistance => 100f;
            public TargetPreference PreferredTarget => TargetPreference.Ships;
            public float GetEngagementRange(ShipClass targetClass) => 100f;
            public bool HasEnoughRange(float distance, float contactDistance, ShipClass targetClass) =>
                distance <= Mathf.Max(80f, contactDistance * 1.15f);
            public bool IsWithinReach(float distance, float contactDistance, ShipClass targetClass) =>
                distance <= Mathf.Max(100f, contactDistance * 1.15f);
            public bool CanUseSiegeApproach(IEntity target) => UseSiegeApproach;
            public bool TryGetHardPointApproach(AttackData target, out Vector3 offset)
            {
                HardPointApproachCalls++;
                offset = HardPointApproach;
                return true;
            }
            public void AddTarget(AttackData data, AttackType type) { }
            public bool HasTarget(IHealthFacade healthFacade) => false;
            public void ResetTarget() { }
            public void Release() { }
        }

        private sealed class Radar : IRadarComponent
        {
            public string Id => nameof(Radar);
            public ObservableList<IEntity> Enemies { get; } = new ObservableList<IEntity>();
            public bool HasEnemy(IEntity entity) => Enemies.Contains(entity);
        }

        private sealed class ArtilleryCapability : IArtilleryCapability
        {
            private float _range;
            public int ArtilleryRevision { get; private set; }
            public ArtilleryCapability(float range) => _range = range;
            public float GetArtilleryRange(ShipClass targetClass) => _range;

            public void SetReach(float range)
            {
                _range = range;
                ArtilleryRevision++;
            }
        }

        private sealed class Target : IEntity, IEntityTransformFacade, INavigationFootprintFacade, IStationHullFacade,
            IHealthFacade, IUnitOrderObserverFacade
        {
            public long Id { get; set; } = 1;
            public Health Health { get; } = new Health();
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner { get; set; } = TestPlayers.Enemy;
            public Transform Transform { get; }
            public float NavigationRadius { get; set; } = 5f;
            public float HullRadius { get; set; }
            public bool HasStationHull { get; set; } = true;
            public bool HasHealthFacade { get; set; }
            // Mobile units (ships, squadrons) take orders; structures such as satellites do not.
            public bool HasOrders { get; set; }
            public UnitOrderType CurrentOrder => UnitOrderType.None;
            public IEntity AttackTarget => null;
            public bool IsEngagingEnRoute => false;
            public float HullBottom => -1f;
            public float HullTop => 1f;
            public float GetHullRadius(Vector3 center) => HullRadius;
            public void ApplyDamage(float damage, DamageType damageType, int id) => throw new NotSupportedException();
            public Target(Transform transform) => Transform = transform;
            public T GetFacade<T>() where T : IEntityFacade =>
                this is T facade ? facade : throw new InvalidOperationException(typeof(T).Name);
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                if ((HasStationHull || typeof(T) != typeof(IStationHullFacade)) &&
                    (HasHealthFacade || typeof(T) != typeof(IHealthFacade)) &&
                    (HasOrders || typeof(T) != typeof(IUnitOrderObserverFacade)) && this is T found)
                { facade = found; return true; }
                facade = default;
                return false;
            }
        }

        private sealed class Health : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Enemy;
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
