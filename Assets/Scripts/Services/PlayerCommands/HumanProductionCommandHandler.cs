using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factions;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>Applies one human's purchase and cancel commands; <see cref="IFactionService"/> checks money and limits.</summary>
    public sealed class HumanProductionCommandHandler : IProductionCommandHandler, IInitializable, ILateDisposable
    {
        private readonly PlayerSlot _owner;
        private readonly IFactionService _faction;
        private readonly IUnitRequestFactory _requests;
        private readonly FactionRoster _roster;
        private readonly StationLevelData _levels;
        private readonly PlayerCommandHandlers _handlers;

        public HumanProductionCommandHandler(PlayerSlot owner, IFactionService faction, IUnitRequestFactory requests,
            FactionRoster roster, StationLevelData levels, PlayerCommandHandlers handlers)
        {
            _owner = owner;
            _faction = faction;
            _requests = requests;
            _roster = roster;
            _levels = levels;
            _handlers = handlers;
        }

        public void Initialize() => _handlers.RegisterProduction(_owner.Id, this);

        public void LateDispose() => _handlers.UnregisterProduction(_owner.Id);

        public void Purchase(PurchaseUnitCommand command) =>
            _faction.TryPurchaseUnit(new UnitRequestKey(command.Kind, command.Key, command.Tier)
                .ToRequest(_requests, _roster, _levels));

        public void Cancel(CancelPurchaseCommand command) => _faction.CancelBuilding(command.RequestId);
    }
}
