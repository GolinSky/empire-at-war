using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class UnitCompletedHintCondition : IHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;

        public bool IsMet { get; private set; }

        public UnitCompletedHintCondition(IPlayerFactionModelObserver faction)
        {
            _faction = faction;
            _faction.OnUnitCompleted += HandleUnitCompleted;
        }

        private void HandleUnitCompleted(UnitRequest request) => IsMet = true;

        public void Dispose() => _faction.OnUnitCompleted -= HandleUnitCompleted;
    }
}
