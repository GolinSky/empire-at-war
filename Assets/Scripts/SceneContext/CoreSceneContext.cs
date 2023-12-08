using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class SkirmishSceneInstaller : MonoInstaller
    {
        [SerializeField] private FactionUiView factionUiView;
        [SerializeField] private FactionModel factionModel;
        public override void InstallBindings()
        {
            Container
                .BindFactory<ShipView, ShipView.ShipFactory>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<ShipInstaller>();
            
            Container
                .BindEntityNoCommand<FactionController, FactionUiView, FactionModel>(
                    Instantiate(factionModel),
                    factionUiView);
        }
    }
}
