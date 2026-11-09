using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class ResearchCompletedHintCondition : IHintCondition
    {
        private readonly IFactionResearchModelObserver _research;

        public bool IsMet { get; private set; }

        public ResearchCompletedHintCondition(IFactionResearchModelObserver research)
        {
            _research = research;
            _research.OnResearchCompleted += HandleResearchCompleted;
        }

        private void HandleResearchCompleted(ResearchType research) => IsMet = true;

        public void Dispose() => _research.OnResearchCompleted -= HandleResearchCompleted;
    }
}
