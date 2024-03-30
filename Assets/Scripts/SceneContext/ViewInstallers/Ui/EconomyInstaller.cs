using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Economy;
using UnityEngine;

namespace EmpireAtWar.SceneContext.ViewInstallers.Ui
{
    public class EconomyInstaller: BaseViewInstaller<EconomyController,EconomyModel>
    {
        [SerializeField] private Zenject.SceneContext SceneContext;

        protected override void BindController()
        {
            base.BindController();
            SceneContext.Container.Bind<IEconomyProvider>().FromMethod(()=>Container.Resolve<IEconomyProvider>());
            SceneContext.Container.Bind<IPurchaseChain>().FromMethod(()=>Container.Resolve<IPurchaseChain>());
        }
    }
}