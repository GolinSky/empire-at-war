using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AuditSharedOperationsTests
    {
        [Test]
        public void ShipStrength_UsesAreaAndOwnerAndReadsCurrentMembership()
        {
            ShipService ships = new ShipService();
            FakeShip player = new FakeShip(TestPlayers.Human, 5f);
            ships.Add(player);
            ships.Add(new FakeShip(TestPlayers.Enemy, 3f));
            ships.Add(new FakeShip(TestPlayers.Enemy, 8f));
            ships.Add(new FakeShip(PlayerId.None, 2f));
            CaptureStrengthBuilder tally = new CaptureStrengthBuilder(TestPlayers.CreateDuel());

            ships.AddShipStrength(position => position.x <= 5f, tally);
            CaptureStrength bothSides = tally.Build();
            Assert.That(bothSides.PresentTeamCount, Is.EqualTo(2));
            Assert.That(bothSides.IsContested, Is.True);

            ships.Remove(player);
            tally.Clear();
            ships.AddShipStrength(position => position.x <= 5f, tally);
            CaptureStrength enemyOnly = tally.Build();
            Assert.That(enemyOnly.LeadingPlayer, Is.EqualTo(TestPlayers.Enemy));
            Assert.That(enemyOnly.Advantage, Is.EqualTo(1f));

            tally.Clear();
            ships.AddShipStrength(position => false, tally);
            Assert.That(tally.Build().HasUnits, Is.False);
        }

        [Test]
        public void MarkerCleanup_RemovesOnlyOwnedMarkersAndCanBeRepeated()
        {
            MiniMapData data = ScriptableObject.CreateInstance<MiniMapData>();
            try
            {
                MiniMapMarkerCollection<string> sites = new MiniMapMarkerCollection<string>(data);
                MiniMapMarkerCollection<string> zones = new MiniMapMarkerCollection<string>(data);
                MiniMapMarker site = new MiniMapMarker(MarkType.CaptureSite, TestPlayers.Human);
                MiniMapMarker zone = new MiniMapMarker(MarkType.ReinforcementZone, TestPlayers.Enemy);
                int removed = 0;
                data.OnMarkerRemoved += marker => removed++;
                sites.Add("site", site);
                zones.Add("zone", zone);
                Assert.That(data.Markers, Is.EquivalentTo(new[] { site, zone }));
                Assert.That(sites.Pairs.Single().Value, Is.SameAs(site));

                sites.Clear();
                sites.Clear();
                Assert.That(data.Markers, Is.EqualTo(new[] { zone }));
                Assert.That(sites.Pairs, Is.Empty);
                Assert.That(removed, Is.EqualTo(1));

                sites.Add("site", site);
                zones.Clear();
                Assert.That(data.Markers, Is.EqualTo(new[] { site }));
                sites.Clear();
                Assert.That(data.Markers, Is.Empty);
                Assert.That(removed, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void SquadronStrength_SkipsDestroyedAndNonSquadronsAndAppliesWeightInsideArea()
        {
            List<GameObject> objects = new List<GameObject>();
            try
            {
                EntityLocator locator = new EntityLocator();
                FakeEntity inside = CreateEntity(1, TestPlayers.Human, 5f, true, false, objects);
                FakeEntity outside = CreateEntity(2, TestPlayers.Enemy, 6f, true, false, objects);
                locator.AddEntity(inside);
                SquadronRegistry registry = new SquadronRegistry(locator);
                locator.AddEntity(outside);
                locator.AddEntity(CreateEntity(3, TestPlayers.Enemy, 0f, true, true, objects));
                locator.AddEntity(CreateEntity(4, TestPlayers.Enemy, 0f, false, false, objects));
                CaptureStrengthBuilder tally = new CaptureStrengthBuilder(TestPlayers.CreateDuel());

                registry.AddSquadronStrength(position => position.x <= 5f, 0.5f, tally);
                registry.Dispose();
                CaptureStrength humanOnly = tally.Build();
                Assert.That(humanOnly.LeadingPlayer, Is.EqualTo(TestPlayers.Human));
                Assert.That(humanOnly.Advantage, Is.EqualTo(0.5f));
            }
            finally
            {
                foreach (GameObject gameObject in objects)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        private static FakeEntity CreateEntity(long id, PlayerId owner, float x, bool isSquadron, bool isDestroyed,
            List<GameObject> objects)
        {
            GameObject gameObject = new GameObject("Squadron");
            gameObject.transform.position = new Vector3(x, 0f, 0f);
            objects.Add(gameObject);
            return new FakeEntity(id: id, owner: owner, role: isSquadron ? new UnitTypeFacade(UnitTypeId.Squadron(default)) : null,
                health: new FakeHealth(gameObject.transform, isDestroyed));
        }

        private sealed class FakeShip : IShipEntity
        {
            public PlayerId Owner { get; }
            public Vector3 WorldPosition { get; }
            public float NavigationRadius => 1f;
            public float NavigationSpeed => 1f;
            public long EntityId => 0;
            public UnitOrderType CurrentOrder => UnitOrderType.None;

            public FakeShip(PlayerId owner, float x)
            {
                Owner = owner;
                WorldPosition = new Vector3(x, 0f, 0f);
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IEntityFacade _role;

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public FakeEntity(IEntityFacade role, FakeHealth health, PlayerId owner, long id)
            {
                Id = id;
                Owner = owner;
                _role = role;
                HealthModel = health;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                TryGetFacade(out TFacade facade);
                return facade;
            }

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (HealthModel is TFacade transformFacade)
                {
                    facade = transformFacade;
                    return true;
                }

                if (_role is TFacade roleFacade)
                {
                    facade = roleFacade;
                    return true;
                }

                facade = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public Transform Transform { get; }
            public bool IsDestroyed { get; }
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => PlayerId.None;
            public bool HasShields => true;

            public FakeHealth(Transform transform, bool isDestroyed)
            {
                Transform = transform;
                IsDestroyed = isDestroyed;
            }

            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
