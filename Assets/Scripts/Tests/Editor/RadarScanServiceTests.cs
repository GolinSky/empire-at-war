using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Radar;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class RadarScanServiceTests
    {
        private const float RANGE = 100f;
        private const float DELAY = 0.25f;

        private readonly List<GameObject> _gameObjects = new List<GameObject>();
        private EntityLocator _locator;
        private RadarScanService _service;

        [SetUp]
        public void SetUp()
        {
            _locator = new EntityLocator();
            _service = new RadarScanService(_locator, TestPlayers.CreateDuel());
            _service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            foreach (GameObject gameObject in _gameObjects)
            {
                Object.DestroyImmediate(gameObject);
            }

            _gameObjects.Clear();
        }

        [Test]
        public void FixedTick_MovedTargetIsDeliveredInTheSameTick()
        {
            FakeEntity target = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            FakeScanner scanner = new FakeScanner(TestPlayers.Human, Vector3.zero) { DelayValue = 0f };
            _service.Register(scanner);

            _service.FixedTick();
            Assert.That(scanner.ScanCount, Is.EqualTo(1));
            Assert.That(scanner.LastScan, Is.EqualTo(new IEntity[] { target }));

            target.GetFacade<IEntityTransformFacade>().Transform.position = new Vector3(200f, 0f, 0f);
            _service.FixedTick();
            Assert.That(scanner.ScanCount, Is.EqualTo(2));
            Assert.That(scanner.LastScan, Is.Empty);
        }

        [Test]
        public void Scan_DeliveryMutationsDoNotChangeOtherResultsFromTheSameSnapshot()
        {
            FakeEntity original = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            FakeEntity replacement = null;
            FakeScanner first = new FakeScanner(TestPlayers.Human, Vector3.zero) { DelayValue = 0f };
            FakeScanner second = new FakeScanner(TestPlayers.Human, Vector3.zero) { DelayValue = 0f };
            _service.Register(first);
            _service.Register(second);
            first.OnScan = () =>
            {
                original.GetFacade<IEntityTransformFacade>().Transform.position = new Vector3(200f, 0f, 0f);
                _locator.RemoveEntity(original);
                replacement = AddEntity(2, TestPlayers.Enemy, new Vector3(20f, 0f, 0f));
                first.OnScan = null;
            };

            _service.FixedTick();
            Assert.That(first.LastScan, Is.EqualTo(new IEntity[] { original }));
            Assert.That(second.LastScan, Is.EqualTo(new IEntity[] { original }));

            _service.FixedTick();
            Assert.That(first.LastScan, Is.EqualTo(new IEntity[] { replacement }));
            Assert.That(second.LastScan, Is.EqualTo(new IEntity[] { replacement }));
        }

        [Test]
        public void Scan_DestroyedTransform_DoesNotReusePreviousPosition()
        {
            FakeEntity destroyed = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            FakeEntity alive = AddEntity(2, TestPlayers.Enemy, new Vector3(20f, 0f, 0f));
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);
            TickUntilScanned(scanner);
            Assert.That(scanner.LastScan, Has.Member(destroyed));

            Object.DestroyImmediate(destroyed.GetFacade<IEntityTransformFacade>().Transform.gameObject);
            TickUntilScanned(scanner);

            Assert.That(scanner.LastScan, Is.EqualTo(new IEntity[] { alive }));
        }

        [Test]
        public void Scan_SwapRemovalAcrossBitWords_RebuildsOwnerEligibility()
        {
            FakeEntity removed = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            List<IEntity> enemies = new List<IEntity>();
            for (int i = 2; i <= 65; i++)
                enemies.Add(AddEntity(i, TestPlayers.Enemy, new Vector3(20f, 0f, 0f)));
            FakeEntity friendly = AddEntity(66, TestPlayers.Human, new Vector3(30f, 0f, 0f));
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);
            TickUntilScanned(scanner);

            _locator.RemoveEntity(removed);
            TickUntilScanned(scanner);

            Assert.That(scanner.LastScan, Is.EquivalentTo(enemies));
            Assert.That(scanner.LastScan, Has.No.Member(friendly));
            ((FakeEntity)enemies[0]).Health.IsDestroyedValue = true;
            enemies.RemoveAt(0);
            TickUntilScanned(scanner);
            Assert.That(scanner.LastScan, Is.EquivalentTo(enemies));
        }

        [Test]
        public void Scan_UnregisterDuringDelivery_SkipsLaterDueRadar()
        {
            AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            FakeScanner first = new FakeScanner(TestPlayers.Human, Vector3.zero) { DelayValue = 0f };
            FakeScanner second = new FakeScanner(TestPlayers.Human, Vector3.zero) { DelayValue = 0f };
            _service.Register(first);
            _service.Register(second);
            first.OnScan = () => _service.Unregister(second);

            _service.FixedTick();

            Assert.That(first.ScanCount, Is.EqualTo(1));
            Assert.That(second.ScanCount, Is.Zero);
        }

        [Test]
        public void Scan_DetectsOnlyHostileTargetsWithinPlanarRange()
        {
            FakeEntity inRange = AddEntity(1, TestPlayers.Enemy, new Vector3(60f, 0f, 60f));
            FakeEntity farAboveInRange = AddEntity(2, TestPlayers.Enemy, new Vector3(0f, 900f, 90f));
            AddEntity(3, TestPlayers.Enemy, new Vector3(80f, 0f, 80f));
            AddEntity(4, TestPlayers.Human, new Vector3(10f, 0f, 0f));
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);

            TickUntilScanned(scanner);

            Assert.That(scanner.LastScan, Is.EquivalentTo(new IEntity[] { inRange, farAboveInRange }));
        }

        [Test]
        public void Scan_IgnoresDestroyedAndUnitlessTargets()
        {
            FakeEntity alive = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            AddEntity(2, TestPlayers.Enemy, new Vector3(20f, 0f, 0f)).Health.IsDestroyedValue = true;
            AddEntity(3, TestPlayers.Enemy, new Vector3(30f, 0f, 0f)).Health.HasUnitsValue = false;
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);

            TickUntilScanned(scanner);

            Assert.That(scanner.LastScan, Is.EquivalentTo(new IEntity[] { alive }));
        }

        [Test]
        public void Scan_AfterTargetRemoval_KeepsRemainingTargetsMapped()
        {
            FakeEntity removed = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            FakeEntity first = AddEntity(2, TestPlayers.Enemy, new Vector3(20f, 0f, 0f));
            FakeEntity last = AddEntity(3, TestPlayers.Enemy, new Vector3(30f, 0f, 0f));
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);
            TickUntilScanned(scanner);

            _locator.RemoveEntity(removed);
            TickUntilScanned(scanner);

            Assert.That(scanner.LastScan, Is.EquivalentTo(new IEntity[] { first, last }));
        }

        [Test]
        public void Scan_TargetRemovedAndReaddedBetweenSteps_StaysDetectedOnce()
        {
            FakeEntity pooled = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            FakeEntity other = AddEntity(2, TestPlayers.Enemy, new Vector3(20f, 0f, 0f));
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);
            TickUntilScanned(scanner);

            // A pooled unit despawns and respawns before the radar applies either change.
            _locator.RemoveEntity(pooled);
            _locator.AddEntity(pooled);
            TickUntilScanned(scanner);

            Assert.That(scanner.LastScan, Is.EquivalentTo(new IEntity[] { pooled, other }));
        }

        [Test]
        public void Scan_TargetAddedAndRemovedBetweenSteps_IsNeverDetected()
        {
            FakeEntity kept = AddEntity(1, TestPlayers.Enemy, new Vector3(10f, 0f, 0f));
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);
            TickUntilScanned(scanner);

            FakeEntity transient = AddEntity(2, TestPlayers.Enemy, new Vector3(20f, 0f, 0f));
            _locator.RemoveEntity(transient);
            TickUntilScanned(scanner);

            Assert.That(scanner.LastScan, Is.EquivalentTo(new IEntity[] { kept }));
        }

        [Test]
        public void FixedTick_StaggersRadarsAcrossSteps()
        {
            int intervalSteps = Mathf.FloorToInt(DELAY / Time.fixedDeltaTime) + 1;
            List<FakeScanner> scanners = new List<FakeScanner>();
            for (int i = 0; i < intervalSteps; i++)
            {
                scanners.Add(Register(TestPlayers.Human, Vector3.zero));
            }

            for (int step = 0; step < intervalSteps; step++)
            {
                int scansBefore = TotalScans(scanners);
                _service.FixedTick();
                Assert.That(TotalScans(scanners) - scansBefore, Is.EqualTo(1), $"Step {step}");
            }

            Assert.That(scanners.TrueForAll(scanner => scanner.ScanCount == 1), Is.True);
        }

        [Test]
        public void FixedTick_UnregisteredRadar_StopsScanning()
        {
            FakeScanner scanner = Register(TestPlayers.Human, Vector3.zero);
            TickUntilScanned(scanner);

            _service.Unregister(scanner);
            int scans = scanner.ScanCount;
            for (int i = 0; i < 50; i++)
            {
                _service.FixedTick();
            }

            Assert.That(scanner.ScanCount, Is.EqualTo(scans));
        }

        [Test]
        public void RadarComponent_ReleasedAfterDispose_DoesNotUnregisterTwice()
        {
            GameObject gameObject = new GameObject("Radar");
            _gameObjects.Add(gameObject);
            RadarComponent radar = gameObject.AddComponent<RadarComponent>();
            RadarModel model = new RadarModel(new RadarData(), TestPlayers.Human, new CombatModifiers());
            typeof(RadarComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(radar, new object[] { _service, null, null, model, null, gameObject.transform });
            _service.Register(radar);

            // The entity owns component release; repeated cleanup remains safe.
            ((EmpireAtWar.Lifecycle.IOrderedDisposable)radar).Dispose();

            Assert.DoesNotThrow(radar.Release);
        }

        private FakeScanner Register(PlayerId owner, Vector3 center)
        {
            FakeScanner scanner = new FakeScanner(owner, center);
            _service.Register(scanner);
            return scanner;
        }

        private FakeEntity AddEntity(long id, PlayerId owner, Vector3 position)
        {
            GameObject gameObject = new GameObject($"RadarTarget{id}");
            _gameObjects.Add(gameObject);
            gameObject.transform.position = position;
            FakeEntity entity = new FakeEntity(id, owner, new EntityTransformFacade(gameObject.transform));
            _locator.AddEntity(entity);
            return entity;
        }

        private void TickUntilScanned(FakeScanner scanner)
        {
            int scans = scanner.ScanCount;
            for (int i = 0; i < 100 && scanner.ScanCount == scans; i++)
            {
                _service.FixedTick();
            }

            Assert.That(scanner.ScanCount, Is.GreaterThan(scans), "The radar never scanned.");
        }

        private static int TotalScans(List<FakeScanner> scanners)
        {
            int total = 0;
            foreach (FakeScanner scanner in scanners)
            {
                total += scanner.ScanCount;
            }

            return total;
        }

        private sealed class RadarData : IRadarData
        {
            public float Range => RANGE;
            public float Delay => DELAY;
        }

        private sealed class FakeScanner : IRadarScanner
        {
            public PlayerId Owner { get; }
            public float Range => RANGE;
            public float DelayValue { get; set; } = DELAY;
            public float Delay => DelayValue;
            public Action OnScan { get; set; }
            public Vector3 ScanCenter { get; }
            public List<IEntity> LastScan { get; } = new List<IEntity>();
            public int ScanCount { get; private set; }

            public FakeScanner(PlayerId owner, Vector3 center)
            {
                Owner = owner;
                ScanCenter = center;
            }

            public void ApplyScan(IReadOnlyList<IEntity> detectedEnemies)
            {
                ScanCount++;
                LastScan.Clear();
                for (int i = 0; i < detectedEnemies.Count; i++)
                {
                    LastScan.Add(detectedEnemies[i]);
                }
                if (OnScan != null) OnScan();
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IEntityTransformFacade _transformFacade;

            public long Id { get; }
            public FakeHealth Health { get; } = new FakeHealth();
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner { get; }

            public FakeEntity(long id, PlayerId owner, IEntityTransformFacade transformFacade)
            {
                Id = id;
                Owner = owner;
                _transformFacade = transformFacade;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                if (TryGetFacade(out TFacade facade)) return facade;
                throw new InvalidOperationException($"No {typeof(TFacade).Name}.");
            }

            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade
            {
                if (_transformFacade is TFacade facade)
                {
                    entityFacade = facade;
                    return true;
                }

                entityFacade = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public bool IsDestroyedValue { get; set; }
            public bool HasUnitsValue { get; set; } = true;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => IsDestroyedValue;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => HasUnitsValue;
            public PlayerId Owner => TestPlayers.Enemy;
            public bool HasShields => true;

            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
