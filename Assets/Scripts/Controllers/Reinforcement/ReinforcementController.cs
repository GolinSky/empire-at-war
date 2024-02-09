using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Models.Reinforcement;
using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Controllers.Reinforcement
{
    public class ReinforcementController:Controller<ReinforcementModel>, IReinforcementCommand
    {
        public ReinforcementController(ReinforcementModel model) : base(model)
        {
        }

        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            throw new System.NotImplementedException();
        }
    }
}