using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.SceneContext.ViewInstallers.Ui
{
    public class ReinforcementInstaller : StaticViewInstaller<ReinforcementController, ReinforcementModel>
    {
        [SerializeField] private Zenject.SceneContext SceneContext;
        
        protected override void BindController()
        {
            base.BindController();
            SceneContext
                .Container
                .Bind<IReinforcementChain>()
                .WithId(PlayerType.Player)
                .FromMethod(()=>Container.Resolve<IReinforcementChain>());
        }
    }
}