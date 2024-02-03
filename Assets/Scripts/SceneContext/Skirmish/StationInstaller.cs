using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Services.Game;
using EmpireAtWar.Views.SpaceStation;
using UnityEngine;
using WorkShop.LightWeightFramework.Repository;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class StationInstaller : Installer
    {
        private readonly IRepository repository;
        private readonly FactionType factionType;
        private readonly PlayerType playerType;
        private readonly Vector3 startPosition;
        private readonly ISkirmishGameService skirmishGameService;


        public StationInstaller(IRepository repository, FactionType factionType, PlayerType playerType, Vector3 startPosition)
        {
            this.repository = repository;
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
                        .BindEntityFromPrefab<SpaceStationController, SpaceStationView, SpaceStationModel,
                            SpaceStationCommand>(
                            repository,
                            factionType);
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container
                        .BindEntityFromPrefab<SpaceStationController, SpaceStationView, SpaceStationModel,
                            EnemySpaceStationCommand>(
                            repository,
                            factionType);
                    break;
                }
            }
        }
    }
}