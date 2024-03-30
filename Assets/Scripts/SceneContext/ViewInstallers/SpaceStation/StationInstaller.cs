using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class StationInstaller : Installer
    {
        private readonly IRepository repository;
        private readonly FactionType factionType;
        private readonly PlayerType playerType;
        private readonly Vector3 startPosition;

        public StationInstaller(IRepository repository, FactionType factionType, PlayerType playerType,
            Vector3 startPosition)
        {
            this.repository = repository;
            this.factionType = factionType;
            this.playerType = playerType;
            this.startPosition = startPosition;
        }

        public override void InstallBindings()
        {
            ViewDependencyBuilder
                .ConstructBuilder(Container)
                .AppendToPath(factionType.ToString(), null)
                .BindFromNewComponent<SpaceStationView>(repository, true);

            Container.BindEntity(startPosition);
            Container.BindEntity(playerType);
            Container.BindEntity(factionType);
        }
    }
}