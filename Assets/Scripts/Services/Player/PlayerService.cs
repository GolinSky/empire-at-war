using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Player
{
    public interface IPlayerService : IService
    {
    }

    public class PlayerService : Service, IInitializable, IPlayerService
    {
        private readonly SpaceStationViewFacade _spaceStationViewFacade;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly UiService _uiService;

        [Inject(Id = PlayerType.Player)]
        private FactionType FactionType { get; }
        
        public PlayerService(
            SpaceStationViewFacade spaceStationViewFacade,
            LazyInject<IMapModelObserver> mapModel,
            UiService uiService)
        {
            _spaceStationViewFacade = spaceStationViewFacade;
            _mapModel = mapModel;
            _uiService = uiService;
        }

        public void Initialize()
        {
            _uiService.CreateUi(UiType.Reinforcement);
            _uiService.CreateUi(UiType.Faction);
            _uiService.CreateUi(UiType.Economy);
            
            _spaceStationViewFacade.Create(
                PlayerType.Player,
                FactionType,
                _mapModel.Value.GetStationPosition(PlayerType.Player));
            

        }
    }
}