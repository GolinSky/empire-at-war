using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using Zenject;
using Zenject.SpaceFighter;

namespace EmpireAtWar.SceneContext.ViewInstallers.Ui
{
    //todo: move it to player context installer
    public class EconomyInstaller: StaticViewInstaller<EconomyController,EconomyModel>
    {
        [Inject] private Zenject.SceneContext SceneContext { get; }

        protected override void BindController()
        {
            base.BindController();
            SceneContext
                .Container
                .Bind<IEconomyProvider>()
                .WithId(PlayerType.Player)
                .FromMethod(()=>Container.Resolve<IEconomyProvider>());
            
            SceneContext
                .Container
                .Bind<IPurchaseChain>()
                .WithId(PlayerType.Player)
                .FromMethod(()=>Container.Resolve<IPurchaseChain>());
        }
    }
}