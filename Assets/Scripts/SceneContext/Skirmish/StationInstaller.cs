using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Views.SpaceStation;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class StationInstaller : Installer
    {
        private readonly FactionType factionType;
        private readonly PlayerType playerType;
        private readonly Vector3 startPosition;

        public StationInstaller(FactionType factionType, PlayerType playerType, Vector3 startPosition)
        {
            this.factionType = factionType;
            this.playerType = playerType;
            this.startPosition = startPosition;
        }

        public override void InstallBindings()
        {
            Container
                .BindInstance(startPosition)
                .AsSingle();
            
            Container
                .BindInstance(playerType)
                .AsSingle();
            
            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container
                        .BindInterfacesAndSelfTo<SelectionComponent>()
                        .AsSingle();
                    
                    Container
                        .BindEntityFromPrefab<SpaceStationController, SpaceStationView, SpaceStationModel,
                            SpaceStationCommand>(factionType);
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container
                        .BindEntityFromPrefab<SpaceStationController, SpaceStationView, SpaceStationModel,
                            EnemySpaceStationCommand>(factionType);
                    
                    Container
                        .BindInterfacesAndSelfTo<EnemySelectionComponent>()
                        .AsSingle();
                    
                    break;
                }
            }
        }
    }
}