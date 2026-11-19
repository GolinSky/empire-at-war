using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MiningFacility
{
    public sealed class MiningFacilityFacade : IMiningFacilityFacade
    {
        private readonly MiningFacility _facility;
        private readonly MiningFacilityModel _model;
        private readonly IFactionResearchModelObserver _research;

        public MiningFacilityFacade(MiningFacility facility, MiningFacilityModel model,
            IFactionResearchModelObserver research)
        {
            _facility = facility;
            _model = model;
            _research = research;
        }

        public float GrossIncome => _facility.IsConstructing ? 0f : _model.BaseIncome * _research.IncomeMultiplier;
    }
}
