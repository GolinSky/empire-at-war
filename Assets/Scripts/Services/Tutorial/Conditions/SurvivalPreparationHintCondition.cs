using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class SurvivalPreparationHintCondition : IHintCondition
    {
        private readonly SurvivalModel _survival;

        public bool IsMet => _survival.Phase == SurvivalPhase.Preparation;

        public SurvivalPreparationHintCondition(SurvivalModel survival)
        {
            _survival = survival;
        }

        public void Dispose()
        {
        }
    }
}
