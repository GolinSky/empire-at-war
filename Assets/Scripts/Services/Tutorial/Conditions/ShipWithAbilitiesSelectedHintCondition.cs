using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class ShipWithAbilitiesSelectedHintCondition : IHintCondition, IObserver<ISelectionSubject>
    {
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _player;

        public bool IsMet { get; private set; }

        public ShipWithAbilitiesSelectedHintCondition(ISelectionService selection, ILocalPlayer player)
        {
            _selection = selection;
            _player = player;
            _selection.AddObserver(this);
        }

        public void UpdateState(ISelectionSubject value)
        {
            foreach (IEntity entity in value.PlayerSelectionContext.Entities)
                if (_player.IsLocal(entity.Owner) && entity.TryGetFacade(out IShipAbilityFacade abilities) &&
                    abilities.Slots.Count > 0)
                {
                    IsMet = true;
                    return;
                }
        }

        public void Dispose() => _selection.RemoveObserver(this);
    }
}
