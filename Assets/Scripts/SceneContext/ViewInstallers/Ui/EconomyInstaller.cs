using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using Zenject.SpaceFighter;

namespace EmpireAtWar.SceneContext.ViewInstallers.Ui
{
    public class EconomyInstaller: StaticViewInstaller<EconomyController,EconomyModel>
    {
        [SerializeField] private Zenject.SceneContext SceneContext;

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