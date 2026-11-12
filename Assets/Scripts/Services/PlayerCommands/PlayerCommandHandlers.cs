using System.Collections.Generic;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>
    /// Battle-scope lookup of the authority-side handlers that live in player scopes or optional modules.
    /// Handlers register themselves on initialize, so the sink never depends on them directly.
    /// </summary>
    public sealed class PlayerCommandHandlers
    {
        private readonly Dictionary<PlayerId, IProductionCommandHandler> _production =
            new Dictionary<PlayerId, IProductionCommandHandler>();
        private readonly Dictionary<PlayerId, ISuperWeaponCommandHandler> _superWeapons =
            new Dictionary<PlayerId, ISuperWeaponCommandHandler>();
        private readonly Dictionary<PlayerId, IReinforcementCommandHandler> _reinforcements =
            new Dictionary<PlayerId, IReinforcementCommandHandler>();
        private ISiteConstructionCommandHandler _siteConstruction;
        private IShipAbilityCommandHandler _abilities;
        private ITacticalMarkerCommandHandler _tacticalMarkers;

        public void RegisterProduction(PlayerId owner, IProductionCommandHandler handler) => _production.Add(owner, handler);
        public void UnregisterProduction(PlayerId owner) => _production.Remove(owner);
        public IProductionCommandHandler GetProduction(PlayerId owner) => _production[owner];

        public void RegisterSuperWeapons(PlayerId owner, ISuperWeaponCommandHandler handler) => _superWeapons.Add(owner, handler);
        public void UnregisterSuperWeapons(PlayerId owner) => _superWeapons.Remove(owner);
        public ISuperWeaponCommandHandler GetSuperWeapons(PlayerId owner) => _superWeapons[owner];

        public void RegisterReinforcements(PlayerId owner, IReinforcementCommandHandler handler) =>
            _reinforcements.Add(owner, handler);
        public void UnregisterReinforcements(PlayerId owner) => _reinforcements.Remove(owner);
        public IReinforcementCommandHandler GetReinforcements(PlayerId owner) => _reinforcements[owner];

        public void RegisterSiteConstruction(ISiteConstructionCommandHandler handler) => _siteConstruction = handler;
        public void UnregisterSiteConstruction() => _siteConstruction = null;

        public void RegisterAbilities(IShipAbilityCommandHandler handler) => _abilities = handler;
        public void UnregisterAbilities() => _abilities = null;

        public void RegisterTacticalMarkers(ITacticalMarkerCommandHandler handler) => _tacticalMarkers = handler;
        public void UnregisterTacticalMarkers() => _tacticalMarkers = null;

        public ITacticalMarkerCommandHandler TacticalMarkers => _tacticalMarkers ??
            throw new System.InvalidOperationException("Tactical markers are not initialized in this battle.");

        public IShipAbilityCommandHandler Abilities => _abilities ??
            throw new System.InvalidOperationException("Ship abilities are not available in this battle.");

        public ISiteConstructionCommandHandler SiteConstruction => _siteConstruction ??
            throw new System.InvalidOperationException("Capture site construction is not available in this battle.");
    }
}
