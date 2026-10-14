using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ProductionAccessTests
    {
        [Test]
        public void HostedAccess_UsesOnlyLivingHostBases()
        {
            EntityLocator entities = new EntityLocator();
            Base host = new Base(1, TestPlayers.Human);
            Base enemy = new Base(2, TestPlayers.Enemy);
            entities.AddEntity(enemy);
            HostedProductionAccess access = new HostedProductionAccess(TestPlayers.Human, new StationRegistry(entities));
            Assert.That(access.CanProduce, Is.False);
            entities.AddEntity(host);
            Assert.That(access.TryGetProductionBase(out IEntity found), Is.True);
            Assert.That(found, Is.SameAs(host));
            host.Health.Destroyed = true;
            Assert.That(access.CanProduce, Is.False);
            Base replacement = new Base(3, TestPlayers.Human);
            entities.AddEntity(replacement);
            Assert.That(access.CanProduce, Is.True);
            entities.RemoveEntity(replacement);
            Assert.That(access.CanProduce, Is.False);
        }

        [Test]
        public void SelfAccess_DoesNotBorrowAlliedProduction()
        {
            EntityLocator entities = new EntityLocator();
            entities.AddEntity(new Base(1, TestPlayers.Human));
            Assert.That(new SelfProductionAccess(TestPlayers.Enemy, new StationRegistry(entities)).CanProduce, Is.False);
        }

        [Test]
        public void HostedLevel_CopiesInitialLevelAndChangesUntilDisposed()
        {
            PlayerFactionModel host = new PlayerFactionModel(null, null) { CurrentLevel = 5 };
            EnemyFactionModel ally = new EnemyFactionModel(null, null);
            HostedFactionLevel binding = new HostedFactionLevel(host, ally);
            Assert.That(ally.CurrentLevel, Is.EqualTo(5));
            host.CurrentLevel = 4;
            Assert.That(ally.CurrentLevel, Is.EqualTo(4));
            binding.Dispose();
            host.CurrentLevel = 3;
            Assert.That(ally.CurrentLevel, Is.EqualTo(4));
        }

        private sealed class Base : IEntity
        {
            private readonly IProductionBaseFacade _production = new ProductionBaseFacade();
            public long Id { get; }
            public PlayerId Owner { get; }
            public Health Health { get; } = new Health();
            public IHealthModelObserver HealthModel => Health;
            public Base(long id, PlayerId owner) { Id = id; Owner = owner; }
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                facade = _production is T result ? result : default;
                return _production is T;
            }
            public T GetFacade<T>() where T : IEntityFacade => (T)_production;
        }

        private sealed class Health : IHealthModelObserver
        {
            public bool Destroyed;
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => default;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => Destroyed ? 0 : 100;
            public float HullPercentage => Destroyed ? 0 : 1;
            public float Shields => 0;
            public float ShieldPercentage => 0;
            public bool IsDestroyed => Destroyed;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !Destroyed;
            public bool HasLiveHardPoints => !Destroyed;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
