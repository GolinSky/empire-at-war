using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Stations;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyDefenseSnapshotBuilder
    {
        private const float THREAT_RADIUS = 1000f;
        private readonly ICaptureSitesSystem _sites;
        private readonly ForceCompositionBuilder _forces;
        private readonly HostileIntelModel _intel;
        private readonly IEntityLocator _entities;
        private readonly IPlayerRoster _players;
        private readonly PlayerSlot _owner;
        private readonly IEconomyModelObserver _money;
        private readonly EconomyService _economy;
        private readonly EconomyData _economyData;
        private readonly EnemyProductionStrategy _production;
        private readonly IEnemyAiDebugInfo _strategy;
        private readonly IAssetService _assets;
        private readonly CaptureSiteData _siteData;
        private readonly WeaponsData _weapons;
        private readonly DamageMatrixData _damage;
        private readonly IProductionAccess _productionAccess;
        private readonly ForceComposition _own = new ForceComposition();
        private readonly ForceComposition _hostile = new ForceComposition();
        private readonly List<EnemyDefenseOption> _options = new List<EnemyDefenseOption>();
        private readonly List<EnemyDefensePad> _pads = new List<EnemyDefensePad>();

        public EnemyDefenseSnapshotBuilder(ICaptureSitesSystem sites, ForceCompositionBuilder forces,
            TeamIntelRegistry intel, IEntityLocator entities, IPlayerRoster players, PlayerSlot owner,
            IEconomyModelObserver money, EconomyService economy, EconomyData economyData,
            EnemyProductionStrategy production, IEnemyAiDebugInfo strategy,
            IAssetService assets, CaptureSiteData siteData, WeaponsData weapons, DamageMatrixData damage,
            IProductionAccess productionAccess)
        {
            _sites = sites;
            _forces = forces;
            _intel = intel.Get(owner.Team);
            _entities = entities;
            _players = players;
            _owner = owner;
            _money = money;
            _economy = economy;
            _economyData = economyData;
            _production = production;
            _strategy = strategy;
            _assets = assets;
            _siteData = siteData;
            _weapons = weapons;
            _damage = damage;
            _productionAccess = productionAccess;
        }

        public EnemyDefenseSnapshot Build()
        {
            float now = Time.time;
            _forces.Build(_own, entity => entity.Owner == _owner.Id);
            _forces.BuildKnown(_hostile, _intel, now, sighting => _players.IsHostile(_owner.Id, sighting.Owner));
            if (_options.Count == 0) LoadOptions();
            _pads.Clear();
            float mineIncome = _assets.Load<MiningFacilityData>("AsteroidMiningFacilityData").Income / _economyData.IncomeDelay;
            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                if (site.Kind != CaptureSiteKind.Satellite || site.Owner != _owner.Id || site.State != CaptureSiteState.Owned)
                    continue;
                float protectedIncome = 0f;
                bool threatened = false;
                List<SiteFacilityType> existing = new List<SiteFacilityType>();
                foreach (ICaptureSite sibling in _sites.Sites)
                {
                    if (sibling.PointId != site.PointId || sibling.Owner != _owner.Id) continue;
                    if (sibling.IsOperational && sibling.FacilityType == SiteFacilityType.Mining) protectedIncome += mineIncome;
                    if (sibling.Kind == CaptureSiteKind.Satellite &&
                        (sibling.State == CaptureSiteState.Constructing || sibling.IsOperational)) existing.Add(sibling.FacilityType);
                    if (_sites.HasHostileUnits(sibling, _owner.Id)) threatened = true;
                }
                float lastSeen = float.PositiveInfinity;
                foreach (HostileSighting sighting in _intel.Sightings)
                {
                    if (!_players.IsHostile(_owner.Id, sighting.Owner) || _intel.GetConfidence(sighting, now) <= 0f) continue;
                    if ((ToVector(sighting.Position) - site.Center).sqrMagnitude <= THREAT_RADIUS * THREAT_RADIUS)
                        lastSeen = Mathf.Min(lastSeen, now - sighting.SeenAt);
                }
                float ownDistance = float.PositiveInfinity;
                float enemyDistance = float.PositiveInfinity;
                foreach (IEntity entity in _entities.Entities)
                {
                    if (!entity.IsPlayerBase() || entity.HealthModel.IsDestroyed) continue;
                    float distance = Vector3.Distance(site.Center, entity.GetFacade<IEntityTransformFacade>().Transform.position);
                    if (entity.Owner == _productionAccess.ProductionHost) ownDistance = Mathf.Min(ownDistance, distance);
                    if (_players.IsHostile(_owner.Id, entity.Owner)) enemyDistance = Mathf.Min(enemyDistance, distance);
                }
                float exposure = float.IsPositiveInfinity(enemyDistance) ? 0f : float.IsPositiveInfinity(ownDistance) ? 1f :
                    ownDistance / Mathf.Max(1f, ownDistance + enemyDistance);
                _pads.Add(new EnemyDefensePad(i, protectedIncome, exposure, lastSeen,
                    _sites.HasHostileUnits(site, _owner.Id), threatened || lastSeen <= 5f,
                    existing));
            }
            return new EnemyDefenseSnapshot(_money.Money, _economy.TotalIncome / _economyData.IncomeDelay,
                _production.DefenseReserve, _strategy.LastSnapshot, _own, _hostile, _pads, _options);
        }

        private void LoadOptions()
        {
            foreach (SiteFacilityType type in _siteData.GetAllowedFacilities(CaptureSiteKind.Satellite))
            {
                DefendPlatformData data = _assets.Load<DefendPlatformData>(SiteFacilityBuilder.SatelliteType(type) + "Data");
                EntityComponentData components = data.ComponentData;
                UnitCombatProfile profile = UnitCombatProfileFactory.Create(components.ShipClass, components.Hull,
                    components.Shields, data.WeaponLoadout, _weapons, _damage, Array.Empty<UnitCombatProfile>());
                float range = 0f;
                foreach (WeaponLoadoutEntry entry in data.WeaponLoadout)
                    range = Mathf.Max(range, _weapons.GetProfile(entry.WeaponType).Range);
                _options.Add(new EnemyDefenseOption(type, _siteData.GetCost(type).Price, profile, range));
            }
        }
    }
}
