using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Round 10 strength rating (W61–W64, W67, W48/W49 decision parts). Relations only, no tuned values.</summary>
    [Category("EnemyAi")]
    public sealed class EnemyStrengthRatingTests
    {
        private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float SHIP_SPEED = 30f;

        private readonly List<GameObject> _views = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject view in _views) UnityEngine.Object.DestroyImmediate(view);
            _views.Clear();
        }

        // W64
        [Test]
        public void Smoother_DropBelowTheRetreatLineMustLastBeforeItCounts()
        {
            const float LINE = 0.5f;
            var smoother = new EnemyAdvantageSmoother();
            smoother.Sample(2f, 1f, 0f, retreatLine: LINE);
            smoother.Sample(LINE * 0.8f, 1f, 0.5f, retreatLine: LINE);
            Assert.That(smoother.Local, Is.GreaterThan(LINE), "One sample below the line is noise.");

            smoother.Sample(LINE * 0.8f, 1f, 0.5f + EnemyAdvantageSmoother.RETREAT_CONFIRM_SECONDS * 0.5f,
                retreatLine: LINE);
            Assert.That(smoother.Local, Is.GreaterThan(LINE));

            smoother.Sample(LINE * 0.8f, 1f, 0.5f + EnemyAdvantageSmoother.RETREAT_CONFIRM_SECONDS, retreatLine: LINE);
            Assert.That(smoother.Local, Is.LessThanOrEqualTo(LINE), "A sustained drop counts.");
        }

        [Test]
        public void Smoother_FarBelowTheRetreatLineCountsAtOnce()
        {
            const float LINE = 0.5f;
            var smoother = new EnemyAdvantageSmoother();
            smoother.Sample(2f, 1f, 0f, retreatLine: LINE);
            smoother.Sample(LINE * EnemyAdvantageSmoother.FAR_BELOW_SHARE * 0.5f, 1f, 0.5f, retreatLine: LINE);

            Assert.That(smoother.Local, Is.LessThan(LINE * EnemyAdvantageSmoother.FAR_BELOW_SHARE));
        }

        [Test]
        public void Smoother_RetreatLineDoesNotDelayTheRise()
        {
            const float LINE = 0.5f;
            var withLine = new EnemyAdvantageSmoother();
            var withoutLine = new EnemyAdvantageSmoother();
            foreach (var smoother in new[] { withLine, withoutLine })
            {
                bool lined = smoother == withLine;
                smoother.Sample(0.2f, 1f, 0f, retreatLine: lined ? LINE : 0f);
                smoother.Sample(3f, 1f, 1f, retreatLine: lined ? LINE : 0f);
            }

            Assert.That(withLine.Local, Is.EqualTo(withoutLine.Local).Within(1e-5f));
        }

        [Test]
        public void Difficulty_HarderNeverRetreatsEarlierThanEasier()
        {
            EnemyAiDifficulty[] order =
                { EnemyAiDifficulty.Easy, EnemyAiDifficulty.Medium, EnemyAiDifficulty.Hard, EnemyAiDifficulty.UltraHard };
            for (int i = 1; i < order.Length; i++)
                Assert.That(EnemyAiDifficultyProfile.Get(order[i]).RetreatAdvantage,
                    Is.LessThanOrEqualTo(EnemyAiDifficultyProfile.Get(order[i - 1]).RetreatAdvantage), order[i].ToString());
            Assert.That(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).RetreatAdvantage,
                Is.LessThan(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Hard).RetreatAdvantage));
        }

        // W67
        [Test]
        public void TradeLedger_WithoutFightsTheCorrectionIsNeutral()
        {
            Assert.That(new EnemyTradeLedger().GetCorrection(10f), Is.EqualTo(1f));
        }

        [Test]
        public void TradeLedger_BetterThanPredictedTradeRaisesTheCorrectionWithinItsBound()
        {
            var ledger = new EnemyTradeLedger();
            ledger.RecordPrediction(1f, 4000f, 4000f, 0f);
            ledger.RecordLoss(false, 4000f, 1f);
            float correction = ledger.GetCorrection(1f);
            Assert.That(correction, Is.GreaterThan(1f));

            for (int i = 0; i < 100; i++) ledger.RecordLoss(false, 100000f, 1f);
            Assert.That(ledger.GetCorrection(1f), Is.GreaterThanOrEqualTo(correction)
                .And.LessThanOrEqualTo(EnemyTradeLedger.MAX_CORRECTION));
        }

        [Test]
        public void TradeLedger_WorseThanPredictedTradeLowersTheCorrectionWithinItsBound()
        {
            var ledger = new EnemyTradeLedger();
            ledger.RecordPrediction(3f, 4000f, 4000f, 0f);
            ledger.RecordLoss(true, 4000f, 1f);
            Assert.That(ledger.GetCorrection(1f), Is.LessThan(1f));

            for (int i = 0; i < 100; i++) ledger.RecordLoss(true, 100000f, 1f);
            Assert.That(ledger.GetCorrection(1f), Is.GreaterThanOrEqualTo(EnemyTradeLedger.MIN_CORRECTION));
        }

        [Test]
        public void TradeLedger_CorrectionFadesBackTowardNeutral()
        {
            var ledger = new EnemyTradeLedger();
            ledger.RecordPrediction(1f, 4000f, 4000f, 0f);
            ledger.RecordLoss(false, 8000f, 0f);
            float fresh = ledger.GetCorrection(0f);
            float later = ledger.GetCorrection(EnemyTradeLedger.MEMORY_SECONDS * 3f);

            Assert.That(later, Is.GreaterThanOrEqualTo(1f).And.LessThan(fresh));
        }

        [Test]
        public void TradeLedger_GoodTradesRaiseTheExpectedGainOfAnAttack()
        {
            var fresh = new EnemyTradeLedger();
            var proven = new EnemyTradeLedger();
            proven.RecordPrediction(1f, 4000f, 4000f, 0f);
            proven.RecordLoss(false, 8000f, 0f);

            Assert.That(proven.ExpectedGain(0.9f, 5000f, 5000f, 1000f, 0f),
                Is.GreaterThan(fresh.ExpectedGain(0.9f, 5000f, 5000f, 1000f, 0f)));
        }

        [Test]
        public void FightRecorder_CountsHostileDeathsOfItsFightOnly()
        {
            var ledgers = new Dictionary<TeamId, EnemyTradeLedger>();
            EnemyTradeLedger Ledger(TeamId team)
            {
                if (!ledgers.TryGetValue(team, out EnemyTradeLedger ledger)) ledgers.Add(team, ledger = new EnemyTradeLedger());
                return ledger;
            }
            var team = new TeamId(0);
            var destroyed = new HashSet<long>();
            var recorder = new EnemyFightRecorder(Ledger);

            recorder.RecordOwnLoss(4000f, 0f);
            recorder.TrackHostile(5, team, 4000f);
            Assert.That(ledgers.Count, Is.Zero, "Nothing counts outside a fight.");

            recorder.Update(1f, true, team, 1f, 4000f, 4000f, destroyed.Contains);
            Assert.That(recorder.IsActive, Is.True);
            recorder.TrackHostile(5, team, 4000f);
            destroyed.Add(5);
            recorder.Update(2f, true, team, 1f, 4000f, 4000f, destroyed.Contains);
            Assert.That(Ledger(team).GetCorrection(2f), Is.GreaterThan(1f), "A destroyed hostile of the fight counts.");

            recorder.Update(2f + EnemyFightRecorder.FIGHT_END_SECONDS, false, team, 1f, 0f, 0f, destroyed.Contains);
            Assert.That(recorder.IsActive, Is.False, "The fight ends after a quiet pause.");
        }

        // W48, W49
        [Test]
        public void LateGame_FleetBelowTheRetreatLineNeitherCapturesNorRaids()
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard);
            EnemyStrategicState late = new EnemyStrategicDecisionModel()
                .Evaluate(Snapshot(profile.RetreatAdvantage * 0.5f, isLateGame: true, hasRaid: true)).State;
            EnemyStrategicState opening = new EnemyStrategicDecisionModel()
                .Evaluate(Snapshot(profile.RetreatAdvantage * 0.5f, isLateGame: false, hasRaid: true)).State;

            Assert.That(late, Is.Not.EqualTo(EnemyStrategicState.CaptureZone).And.Not.EqualTo(EnemyStrategicState.RaidStructures));
            Assert.That(opening, Is.EqualTo(EnemyStrategicState.CaptureZone), "The opening is unchanged.");
            Assert.That(new EnemyStrategicDecisionModel()
                    .Evaluate(Snapshot(profile.RetreatAdvantage, isLateGame: true, hasRaid: true)).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone), "A fleet at the line still captures late.");
        }

        [Test]
        public void StateHold_NonUrgentSwitchWaitsWhileTheStateStillHasItsReason()
        {
            var model = new EnemyStrategicDecisionModel();
            Assert.That(model.Evaluate(Snapshot(1f, hasCapture: false, hasGuardSite: true), now: 0f).State,
                Is.EqualTo(EnemyStrategicState.GuardSite));

            Assert.That(model.Evaluate(Snapshot(1f, hasGuardSite: true), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.GuardSite));
            Assert.That(model.Evaluate(Snapshot(1f, hasGuardSite: true), now: EnemyStrategicDecisionModel.MIN_STATE_SECONDS)
                .State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void StateHold_IdleHoldStartsANewTaskAtOnce()
        {
            var model = new EnemyStrategicDecisionModel();
            Assert.That(model.Evaluate(Snapshot(1f, hasCapture: false), now: 0f).State, Is.EqualTo(EnemyStrategicState.Hold));

            Assert.That(model.Evaluate(Snapshot(1f), now: 1f).State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void Retreat_StaysLatchedUntilTheGatheringPointIsReached()
        {
            var model = new EnemyStrategicDecisionModel();
            Assert.That(model.Evaluate(Snapshot(1f, localAdvantage: 0.1f, canDisengage: true), now: 0f).State,
                Is.EqualTo(EnemyStrategicState.RetreatValue));

            EnemyStrategicDecision onTheWay = model.Evaluate(Snapshot(1f, reachedRetreatPoint: false),
                now: EnemyStrategicDecisionModel.MIN_STATE_SECONDS * 2f);
            Assert.That(onTheWay.State, Is.EqualTo(EnemyStrategicState.RetreatValue),
                "Losing sight of the hostiles does not turn the group back.");
            Assert.That(onTheWay.CommittedShipCount, Is.EqualTo(OWN_SHIPS));

            Assert.That(model.Evaluate(Snapshot(1f), now: EnemyStrategicDecisionModel.MIN_STATE_SECONDS * 3f).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void Retreat_LatchYieldsToStationPressure()
        {
            var model = new EnemyStrategicDecisionModel();
            model.Evaluate(Snapshot(1f, localAdvantage: 0.1f, canDisengage: true), now: 0f);

            Assert.That(model.Evaluate(Snapshot(1f, reachedRetreatPoint: false, stationPressure: true), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.DefendBase));
        }

        // W61
        [Test]
        public void DeathOutsideTheGroupFight_LeavesTheLocalRatingAlone()
        {
            Scene scene = CreateScene();
            scene.AddShip(1, TestPlayers.Enemy, Vector3.zero, 1000f);
            scene.AddShip(2, TestPlayers.Human, Vector3.right * 300f, 200f);
            scene.AddShip(3, TestPlayers.Human, Vector3.right * 3300f, 5000f);
            scene.AddShip(4, TestPlayers.Human, Vector3.right * 3300f, 5000f);
            float baseline = scene.RateGroupFight(1, null);

            float rating = scene.RateGroupFight(1, Vector3.right * 3000f);

            Assert.That(scene.Builder.LocalOriginSource, Is.EqualTo("all"));
            Assert.That(rating, Is.EqualTo(baseline).Within(1e-5f));
        }

        [Test]
        public void DeathInsideTheGroupFight_StillReRatesAtTheDeathSpot()
        {
            Scene scene = CreateScene();
            scene.AddShip(1, TestPlayers.Enemy, Vector3.zero, 1000f);
            scene.AddShip(2, TestPlayers.Human, Vector3.right * 300f, 200f);
            scene.AddShip(3, TestPlayers.Human, Vector3.right * 1600f, 5000f);
            scene.AddShip(4, TestPlayers.Human, Vector3.right * 1600f, 5000f);
            float baseline = scene.RateGroupFight(1, null);

            float rating = scene.RateGroupFight(1, Vector3.right * 900f);

            Assert.That(scene.Builder.LocalOriginSource, Is.EqualTo("death"));
            Assert.That(rating, Is.LessThan(baseline));
        }

        // W62
        [Test]
        public void OwnStationAtTheFight_RaisesTheLocalRating()
        {
            Scene scene = CreateScene();
            scene.AddShip(1, TestPlayers.Enemy, Vector3.zero, 1000f);
            scene.AddShip(2, TestPlayers.Human, Vector3.right * 300f, 1000f);
            float without = scene.RateFight(Vector3.zero);

            scene.AddStation(10, TestPlayers.Enemy, Vector3.left * 300f, 5000f);

            Assert.That(scene.RateFight(Vector3.zero), Is.GreaterThan(without));
        }

        [Test]
        public void KnownHostileStationAtTheFight_LowersTheLocalRating()
        {
            Scene scene = CreateScene();
            scene.AddShip(1, TestPlayers.Enemy, Vector3.zero, 1000f);
            scene.AddShip(2, TestPlayers.Human, Vector3.right * 300f, 1000f);
            float without = scene.RateFight(Vector3.zero);

            scene.AddStation(10, TestPlayers.Human, Vector3.right * 600f, 5000f);

            Assert.That(scene.RateFight(Vector3.zero), Is.LessThan(without));
        }

        [Test]
        public void SeenHostileStation_IsNoShipSightingAndNoFleetStrength()
        {
            Scene scene = CreateScene();
            scene.AddShip(2, TestPlayers.Human, Vector3.right * 300f, 1000f, report: false);
            StubEntity station = scene.AddStation(10, TestPlayers.Human, Vector3.right * 600f, 5000f);
            station.UnitTypeId = UnitTypeId.Ship(ShipType.Venator);
            station.HasUnitType = true;
            var service = new TeamHostileIntelService(scene.Locator, scene.Vision, scene.Roster, scene.Intel, scene.World);
            service.RegisterPlayer(scene.Roster.Get(TestPlayers.Enemy));

            service.GatherAll(1f);

            HostileIntelModel intel = scene.Intel.Get(scene.Roster.Get(TestPlayers.Enemy).Team);
            Assert.That(intel.Sightings.Count, Is.EqualTo(1), "Only the ship is a sighting.");
            Assert.That((int)typeof(EnemyStrategicContextBuilder).GetMethod("CountKnownShips", PRIVATE)
                .Invoke(scene.Builder, new object[] { TestPlayers.Human, 1f }), Is.EqualTo(1));
        }

        // W63
        [Test]
        public void ReachRating_CountsOnlyHostilesThatCanArriveInTheWindow()
        {
            Scene scene = CreateScene();
            scene.AddShip(2, TestPlayers.Human, Vector3.right * 1500f, 1000f);
            scene.AddShip(3, TestPlayers.Human, Vector3.right * 20000f, 1000f);
            scene.RefreshPicture(1f);
            MethodInfo collect = typeof(EnemyStrategicContextBuilder).GetMethod("CollectReach", PRIVATE);
            var hostile = (ForceComposition)typeof(EnemyStrategicContextBuilder).GetField("_reachHostileForce", PRIVATE)
                .GetValue(scene.Builder);

            collect.Invoke(scene.Builder, new object[] { new ForceComposition(), Vector3.zero, false });
            Assert.That(hostile.UnitCount, Is.EqualTo(1), "The far ship cannot be there in time.");

            collect.Invoke(scene.Builder, new object[] { new ForceComposition(), Vector3.zero, true });
            Assert.That(hostile.UnitCount, Is.EqualTo(2));
        }

        private const int OWN_SHIPS = 5;

        private static EnemyStrategicSnapshot Snapshot(float fleetAdvantage, bool isLateGame = false,
            bool hasCapture = true, bool hasGuardSite = false, float localAdvantage = CombatMatchup.MAX_ADVANTAGE,
            bool canDisengage = false, bool reachedRetreatPoint = true, bool stationPressure = false,
            bool hasRaid = false) =>
            new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                ownShipCount: OWN_SHIPS, enemyShipCount: 6, ownedCapturableZoneCount: 1, fleetAdvantage: fleetAdvantage,
                baseThreatRatio: stationPressure ? 1f : 0f, hasCaptureTarget: hasCapture, hasEnemyBaseTarget: true,
                hasOwnBase: true, localAdvantage: localAdvantage,
                hasLocalThreat: localAdvantage < CombatMatchup.MAX_ADVANTAGE, hasRaidTarget: hasRaid,
                hasStationPressure: stationPressure, hasGuardSite: hasGuardSite,
                canDisengage: canDisengage, isLateGame: isLateGame, hasReachedRetreatPoint: reachedRetreatPoint);

        private Scene CreateScene()
        {
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var damage = new float[ForceComposition.ClassSlots];
            damage[(int)ShipClass.Capital] = 100f;
            damage[(int)ShipClass.Structure] = 100f;
            ((Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog).GetField("_profiles", PRIVATE)
                .GetValue(catalog)).Add(UnitTypeId.Ship(ShipType.Venator), new UnitCombatProfile(ShipClass.Capital, 1000f,
                0f, damage, damage, new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>(),
                speed: SHIP_SPEED, price: 1000f));
            return new Scene(roster, catalog, _views);
        }

        private sealed class Scene
        {
            public readonly PlayerRoster Roster;
            public readonly EntityLocator Locator = new EntityLocator();
            public readonly AiWorldSnapshot World;
            public readonly TeamIntelRegistry Intel = new TeamIntelRegistry();
            public readonly VisionService Vision;
            public readonly EnemyStrategicContextBuilder Builder;
            private readonly List<GameObject> _views;

            public Scene(PlayerRoster roster, UnitCombatProfileCatalog catalog, List<GameObject> views)
            {
                Roster = roster;
                _views = views;
                World = new AiWorldSnapshot(Locator);
                Vision = new VisionService(roster, fogEnabled: false);
                var zones = new EmptyZones();
                Builder = new EnemyStrategicContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(),
                    Locator, null, roster, new ForceCompositionBuilder(Locator, catalog, World), Intel, Vision,
                    roster.Get(TestPlayers.Enemy), null, null, zones, null, new EnemyStationPressureTracker(), null,
                    World, new TeamAiPictureRegistry(Intel, roster, zones, catalog, World), new EnemyFactionModel(null, null));
            }

            public StubEntity AddShip(long id, PlayerId owner, Vector3 position, float hull, bool report = true)
            {
                StubEntity ship = Add(id, owner, position, hull, ShipClass.Capital);
                ship.HasUnitType = true;
                ship.UnitTypeId = UnitTypeId.Ship(ShipType.Venator);
                if (report && owner != TestPlayers.Enemy)
                    Intel.Get(Roster.Get(TestPlayers.Enemy).Team).Report(new HostileSighting(id, ship.UnitTypeId, owner,
                        ToPoint(position), hull, 0f, 1f, 0f));
                return ship;
            }

            public StubEntity AddStation(long id, PlayerId owner, Vector3 position, float hull)
            {
                StubEntity station = Add(id, owner, position, hull, ShipClass.Structure);
                station.IsPlayerBase = true;
                return station;
            }

            /// <summary>The non-group local rating around <paramref name="origin"/>.</summary>
            public float RateFight(Vector3 origin)
            {
                RefreshPicture(0f);
                typeof(EnemyStrategicContextBuilder).GetMethod("BuildLocalForces", PRIVATE)
                    .Invoke(Builder, new object[] { Array.Empty<IShipEntity>(), origin, 0f });
                return LocalRating();
            }

            /// <summary>The group's local rating with own ship <paramref name="shipId"/> and a recent group death.</summary>
            public float RateGroupFight(long shipId, Vector3? death)
            {
                RefreshPicture(0f);
                typeof(EnemyStrategicContextBuilder).GetField("_groupScoped", PRIVATE).SetValue(Builder, true);
                var ids = (HashSet<long>)typeof(EnemyStrategicContextBuilder).GetField("_ownShipIds", PRIVATE)
                    .GetValue(Builder);
                ids.Clear();
                ids.Add(shipId);
                typeof(EnemyStrategicContextBuilder).GetField("_lastDeathPosition", PRIVATE)
                    .SetValue(Builder, death ?? Vector3.zero);
                typeof(EnemyStrategicContextBuilder).GetField("_lastDeathTime", PRIVATE)
                    .SetValue(Builder, death.HasValue ? 0f : float.NegativeInfinity);
                Vector3 position = ((StubEntity)Locator.GetEntity(shipId)).Transform.position;
                typeof(EnemyStrategicContextBuilder).GetMethod("BuildLocalForces", PRIVATE).Invoke(Builder,
                    new object[] { new IShipEntity[] { new StubShip(shipId, position) }, position, 0f });
                return LocalRating();
            }

            public void RefreshPicture(float now) =>
                ((TeamAiPicture)typeof(EnemyStrategicContextBuilder).GetField("_teamPicture", PRIVATE).GetValue(Builder))
                    .Refresh(now);

            private float LocalRating() => CombatMatchup.Advantage(Field("_localOwnForce"), Field("_localThreatForce"));

            private ForceComposition Field(string name) =>
                (ForceComposition)typeof(EnemyStrategicContextBuilder).GetField(name, PRIVATE).GetValue(Builder);

            private StubEntity Add(long id, PlayerId owner, Vector3 position, float hull, ShipClass shipClass)
            {
                var view = new GameObject("Rated unit " + id);
                _views.Add(view);
                view.transform.position = position;
                var entity = new StubEntity(id, owner, new StubHealth(hull, shipClass, owner), view.transform);
                Locator.AddEntity(entity);
                return entity;
            }

            private static FormationPoint ToPoint(Vector3 position) => new FormationPoint(position.x, position.z);
        }

        private sealed class StubEntity : GameEntity, IEntityTransformFacade, IUnitTypeFacade, IPlayerBaseFacade
        {
            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }
            public Transform Transform { get; }
            public UnitTypeId UnitTypeId { get; set; }
            public bool HasUnitType { get; set; }
            public bool IsPlayerBase { get; set; }

            public StubEntity(long id, PlayerId owner, IHealthModelObserver health, Transform transform)
            {
                Id = id;
                Owner = owner;
                HealthModel = health;
                Transform = transform;
            }

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (!HasUnitType && typeof(TFacade) == typeof(IUnitTypeFacade) ||
                    !IsPlayerBase && typeof(TFacade) == typeof(IPlayerBaseFacade) || !(this is TFacade self))
                {
                    facade = default;
                    return false;
                }
                facade = self;
                return true;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                TryGetFacade(out TFacade facade) ? facade : throw new InvalidOperationException(typeof(TFacade).Name);
        }

        private sealed class StubHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass { get; }
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<IReadOnlyHardPointModel>();
            public float Hull { get; }
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner { get; }

            public StubHealth(float hull, ShipClass shipClass, PlayerId owner)
            {
                Hull = hull;
                ShipClass = shipClass;
                Owner = owner;
            }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }

        private sealed class StubShip : IShipEntity
        {
            public PlayerId Owner => TestPlayers.Enemy;
            public Vector3 WorldPosition { get; }
            public float NavigationRadius => 5f;
            public float NavigationSpeed => SHIP_SPEED;
            public long EntityId { get; }
            public UnitOrderType CurrentOrder => UnitOrderType.None;

            public StubShip(long id, Vector3 position)
            {
                EntityId = id;
                WorldPosition = position;
            }
        }

        private sealed class EmptyZones : IReinforcementZoneSource
        {
            public IReadOnlyList<ReinforcementZonePresenter> Zones => Array.Empty<ReinforcementZonePresenter>();
            public float SquadronCaptureWeight => 0.5f;
            public (float Friendly, float Hostile) GetTeamStrengths(ReinforcementZonePresenter zone, PlayerId owner) =>
                default;
        }
    }
}
