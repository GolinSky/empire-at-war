using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.SceneContext.ViewInstallers.Ui
{
    public class ReinforcementInstaller : BaseViewInstaller<ReinforcementController, ReinforcementModel>
    {
        [SerializeField] private Zenject.SceneContext SceneContext;
        
        protected override void BindController()
        {
            base.BindController();
            SceneContext.Container.Bind<IReinforcementChain>().FromMethod(()=>Container.Resolve<IReinforcementChain>());
        }
    }
}