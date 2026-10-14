using System.Threading;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SimulationResetService
    {
        private readonly PlayerScopeBootstrap _players;
        private readonly PlayerRoster _roster;
        private readonly BattleUnitSpawner _spawner;
        private readonly ShotPoolPrewarmer _shots;
        private readonly BoltFlightBatch _bolts;
        private readonly IncomingMissileRegistry _missiles;
        private readonly UnitWreckService _wrecks;
        private readonly UnitExplosionService _explosions;
        private readonly FighterExplosionPool _fighters;
        private readonly HardPointExplosionPool _hardPoints;
        private readonly ImpactEffectView _impacts;
        private readonly BattleVictoryService _victory;
        private readonly TeamIntelRegistry _intel;
        private readonly ShipAbilityService _abilities;

        public SimulationResetService(PlayerScopeBootstrap players, PlayerRoster roster, BattleUnitSpawner spawner,
            ShotPoolPrewarmer shots, BoltFlightBatch bolts, IncomingMissileRegistry missiles, UnitWreckService wrecks,
            UnitExplosionService explosions, FighterExplosionPool fighters, HardPointExplosionPool hardPoints,
            ImpactEffectView impacts, BattleVictoryService victory, TeamIntelRegistry intel, ShipAbilityService abilities)
        {
            _players = players; _roster = roster; _spawner = spawner; _shots = shots; _bolts = bolts;
            _missiles = missiles; _wrecks = wrecks; _explosions = explosions; _fighters = fighters;
            _hardPoints = hardPoints; _impacts = impacts; _victory = victory;
            _intel = intel; _abilities = abilities;
        }

        public async Awaitable ResetAsync(SimulationSetup setup, CancellationToken cancellationToken)
        {
            _abilities.Clear();
            _players.Dispose();
            _intel.Clear();
            _spawner.Clear();
            _shots.Clear(); _bolts.Clear(); _missiles.Clear();
            _wrecks.Clear(); _explosions.Clear(); _fighters.Clear(); _hardPoints.Clear(); _impacts.Clear();
            await Awaitable.NextFrameAsync(cancellationToken);
            _roster.Replace(SimulationPreset.Create(setup).Build().Slots);
            _players.Initialize();
            _victory.Reset();
        }
    }
}
