using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>The save being loaded and the views spawned from it, by saved entity id.</summary>
    public sealed class BattleRestoreContext
    {
        public BattleSave Save { get; }
        public Dictionary<long, ShipEntity> Ships { get; } = new Dictionary<long, ShipEntity>();
        public Dictionary<long, Squadron> Squadrons { get; } = new Dictionary<long, Squadron>();
        public Dictionary<long, MiningFacilityEntity> MiningFacilities { get; } = new Dictionary<long, MiningFacilityEntity>();
        public Dictionary<long, DefendPlatformEntity> DefendPlatforms { get; } = new Dictionary<long, DefendPlatformEntity>();
        /// <summary>Squadrons a hangar bay launched; they belong to the bay, not to their owner's unit limits.</summary>
        public HashSet<long> HangarSquadrons { get; } = new HashSet<long>();
        /// <summary>Facilities a capture site built; they belong to the site, not to their owner's structure limits.</summary>
        public HashSet<long> SiteFacilities { get; } = new HashSet<long>();

        public BattleRestoreContext(BattleSave save)
        {
            Save = save;
        }
    }
}
