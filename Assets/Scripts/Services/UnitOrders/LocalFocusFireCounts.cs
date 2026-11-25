using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>Offline and host: the local team's holders straight from the slot registry.</summary>
    public sealed class LocalFocusFireCounts : IFocusFireCounts
    {
        private readonly IFocusFireSlots _slots;
        private readonly ILocalPlayer _localPlayer;

        public LocalFocusFireCounts(IFocusFireSlots slots, ILocalPlayer localPlayer)
        {
            _slots = slots;
            _localPlayer = localPlayer;
        }

        public int CountHolders(IEntity target, bool isSquadron) =>
            _slots.CountHolders(_localPlayer.Id, target, isSquadron);
    }
}
