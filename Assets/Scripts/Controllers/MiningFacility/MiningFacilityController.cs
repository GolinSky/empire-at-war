using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Services.EconomyMediator;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.MiningFacility
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
            IEconomyMediator economyMediator) : base(model)
        {
            _economyProvider = economyMediator.GetProvider(playerType);
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