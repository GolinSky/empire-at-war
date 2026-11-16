using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.Cheats;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Views.Cheats;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class CheatPresenterTests
    {
        private FactionCatalog _catalog;
        private FactionDefinition _republic;
        private FactionDefinition _empire;
        private DefendPlatformCatalog _platforms;
        private CheatView _view;
        private CheatServiceSpy _service;
        private CheatPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _republic = CreateFaction(FactionType.Republic);
            _empire = CreateFaction(FactionType.Empire);
            _catalog = ScriptableObject.CreateInstance<FactionCatalog>();
            typeof(FactionCatalog).GetField("factions", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_catalog, new[] { _republic, _empire });
            _platforms = ScriptableObject.CreateInstance<DefendPlatformCatalog>();
            JsonUtility.FromJsonOverwrite("{\"entries\":{\"keyValue\":[]}}", _platforms);
            _view = new CheatView();
            _service = new CheatServiceSpy();
            PlayerRoster players = TestPlayers.CreateDuel();
            _presenter = new CheatPresenter(_view, _service, _catalog, null, null, _platforms,
                players, players, players.Get(TestPlayers.Human));
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.LateDispose();
            UnityEngine.Object.DestroyImmediate(_catalog);
            UnityEngine.Object.DestroyImmediate(_republic);
            UnityEngine.Object.DestroyImmediate(_empire);
            UnityEngine.Object.DestroyImmediate(_platforms);
        }

        [Test]
        public void Initialize_SharedShipTypeAcceptsDistinctFactionDataAndSelectsFirstFaction()
        {
            Assert.That(_republic.Ships[ShipType.Tector], Is.Not.SameAs(_empire.Ships[ShipType.Tector]));

            Assert.DoesNotThrow(() => _presenter.Initialize());

            Assert.That(_view.Factions, Is.EqualTo(new[] { FactionType.Republic, FactionType.Empire }));
            Assert.That(_view.Ships, Is.EqualTo(new[] { ShipType.Tector }));
            _view.RequestReinforcement(ShipType.Tector);
            Assert.That(_service.Reinforcements[0].FactionData, Is.SameAs(_republic.Ships[ShipType.Tector]));
        }

        [TestCase(FactionType.Empire)]
        [TestCase(FactionType.Republic)]
        public void SingleShipActions_UseSelectedFactionsData(FactionType selectedFaction)
        {
            _presenter.Initialize();
            _view.SelectFaction(FactionType.Empire);
            _view.SelectFaction(selectedFaction);

            _view.RequestReinforcement(ShipType.Tector);
            _view.RequestSpawn(ShipType.Tector);

            FactionData expected = _catalog.Get(selectedFaction).Ships[ShipType.Tector];
            Assert.That(_service.Reinforcements, Has.Count.EqualTo(1));
            Assert.That(_service.Spawns, Has.Count.EqualTo(1));
            Assert.That(_service.Reinforcements[0].Key, Is.EqualTo(ShipType.Tector));
            Assert.That(_service.Spawns[0].Key, Is.EqualTo(ShipType.Tector));
            Assert.That(_service.Reinforcements[0].FactionData, Is.SameAs(expected));
            Assert.That(_service.Spawns[0].FactionData, Is.SameAs(expected));
        }

        [TestCase(FactionType.Republic, FactionType.Empire)]
        [TestCase(FactionType.Empire, FactionType.Republic)]
        public void SpawnAll_UsesRequestedFactionWithoutChangingSingleShipSelection(
            FactionType selectedFaction, FactionType requestedFaction)
        {
            _presenter.Initialize();
            _view.SelectFaction(selectedFaction);

            _view.RequestSpawnAll(requestedFaction);
            _view.RequestSpawn(ShipType.Tector);

            Assert.That(_service.Spawns, Has.Count.EqualTo(2));
            Assert.That(_service.Spawns[0].Key, Is.EqualTo(ShipType.Tector));
            Assert.That(_service.Spawns[0].FactionData,
                Is.SameAs(_catalog.Get(requestedFaction).Ships[ShipType.Tector]));
            Assert.That(_service.Spawns[1].FactionData,
                Is.SameAs(_catalog.Get(selectedFaction).Ships[ShipType.Tector]));
        }

        private static FactionDefinition CreateFaction(FactionType type)
        {
            var faction = ScriptableObject.CreateInstance<FactionDefinition>();
            JsonUtility.FromJsonOverwrite("{\"factionType\":" + (int)type +
                ",\"ships\":{\"keyValue\":[{\"key\":" + (int)ShipType.Tector + ",\"value\":{}}]}}", faction);
            return faction;
        }

        private sealed class CheatServiceSpy : ICheatService
        {
            public List<ShipUnitRequest> Reinforcements { get; } = new List<ShipUnitRequest>();
            public List<ShipUnitRequest> Spawns { get; } = new List<ShipUnitRequest>();
            public void AddShipReinforcement(ShipUnitRequest request) => Reinforcements.Add(request);
            public bool ForceSpawnShipAtDefaultZone(ShipUnitRequest request)
            {
                Spawns.Add(request);
                return true;
            }
            public void AddMoney(float amount) => throw new NotSupportedException();
            public bool GrantSuperWeapon(SuperWeaponType type) => throw new NotSupportedException();
            public void SetRangeDebug(bool isEnabled) => throw new NotSupportedException();
            public int DestroyOwnShips() => throw new NotSupportedException();
        }

        private sealed class CheatView : ICheatView
        {
            public IReadOnlyList<FactionType> Factions { get; private set; }
            public IReadOnlyList<ShipType> Ships { get; private set; }
            public event Action<FactionType> FactionSelected;
            public event Action<ShipType> AddReinforcementRequested;
            public event Action<ShipType> SpawnForceRequested;
            public event Action<FactionType> SpawnAllShipsRequested;
            public void SelectFaction(FactionType faction) => FactionSelected(faction);
            public void RequestReinforcement(ShipType ship) => AddReinforcementRequested(ship);
            public void RequestSpawn(ShipType ship) => SpawnForceRequested(ship);
            public void RequestSpawnAll(FactionType faction) => SpawnAllShipsRequested(faction);
            public void SetFactions(IReadOnlyList<FactionType> factions) => Factions = factions;
            public void SetShips(IReadOnlyList<ShipType> ships) => Ships = ships;
            public void SetStatus(string status) { }
            public void SetEnemies(IReadOnlyList<PlayerSlot> enemies) { }
            public void SetPlatforms(IReadOnlyList<DefendPlatformType> platforms) { }
            public event Action LevelUpStationRequested { add { } remove { } }
            public event Action<DefendPlatformType> AddPlatformReinforcementRequested { add { } remove { } }
            public event Action<DefendPlatformType> SpawnPlatformRequested { add { } remove { } }
            public event Action<PlayerId, string> AddEnemyMoneyRequested { add { } remove { } }
            public event Action<string> AddAllEnemiesMoneyRequested { add { } remove { } }
            public event Action<PlayerId> LevelUpEnemyStationRequested { add { } remove { } }
            public event Action LevelUpAllEnemyStationsRequested { add { } remove { } }
            public event Action RestartHintsRequested { add { } remove { } }
            public event Action<string> AddMoneyRequested { add { } remove { } }
            public event Action<SuperWeaponType> GrantSuperWeaponRequested { add { } remove { } }
            public event Action GrantAllSuperWeaponsRequested { add { } remove { } }
            public event Action<bool> RangeDebugToggled { add { } remove { } }
            public event Action DestroyOwnShipsRequested { add { } remove { } }
        }
    }
}
