using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmpireAtWar.SceneContext.ViewInstallers.Ui
{
    public class ReinforcementInstaller : StaticViewInstaller<ReinforcementController, ReinforcementModel>
    {
        [FormerlySerializedAs("SceneContext")] [SerializeField] private Zenject.SceneContext sceneContext;
        
        protected override void BindController()
        {
            base.BindController();
            sceneContext
                .Container
                .Bind<IReinforcementChain>()
                .WithId(PlayerType.Player)
                .FromMethod(()=>Container.Resolve<IReinforcementChain>());
        }
    }
}