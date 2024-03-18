using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.MiningFacility;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.MiningFacility
{
    public interface IMiningFacilityCommand: ICommand
    {
        
    }
    
    public class MiningFacilityController : Controller<MiningFacilityModel>, IMiningFacilityCommand, IIncomeProvider, IInitializable, ILateDisposable
    {
        private readonly IEconomyProvider economyProvider;
        
        public float Income => Model.Income;

        public MiningFacilityController(MiningFacilityModel model, IEconomyProvider economyProvider) : base(model)
        {
            this.economyProvider = economyProvider;
        }

        public void Initialize()
        {
            Model.HealthModel.OnDestroy += HandleDestroy;
            economyProvider.AddProvider(this);
        }
        
        public void LateDispose()
        {
            Model.HealthModel.OnDestroy -= HandleDestroy;
            economyProvider.RemoveProvider(this);
        }
        
        private void HandleDestroy()
        {
            economyProvider.RemoveProvider(this);
        }
    }
}