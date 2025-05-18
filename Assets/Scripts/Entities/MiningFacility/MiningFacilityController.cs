using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityCommand : ICommand
    {
    }

    public class MiningFacilityController : Controller<MiningFacilityModel>, IMiningFacilityCommand, IIncomeProvider,
        IInitializable, ILateDisposable
    {
        private readonly IEconomyProvider _economyProvider;

        public float Income => Model.Income;

        public MiningFacilityController(
            MiningFacilityModel model,
            PlayerType playerType,
            IEconomyProvider economyProvider) : base(model)
        {
            _economyProvider = economyProvider;
        }

        public void Initialize()
        {
            Model.HealthModel.OnDestroy += HandleDestroy;
            _economyProvider.AddProvider(this);
        }

        public void LateDispose()
        {
            Model.HealthModel.OnDestroy -= HandleDestroy;
            _economyProvider.RemoveProvider(this);
        }

        private void HandleDestroy()
        {
            _economyProvider.RemoveProvider(this);
        }
    }
}