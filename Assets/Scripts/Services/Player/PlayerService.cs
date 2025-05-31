using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
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
        private readonly IUiService _uiService;

        [Inject(Id = PlayerType.Player)]
        private FactionType FactionType { get; }
        
        public PlayerService(
            SpaceStationViewFacade spaceStationViewFacade,
            LazyInject<IMapModelObserver> mapModel,
            IUiService uiService)
        {
            _spaceStationViewFacade = spaceStationViewFacade;
            _mapModel = mapModel;
            _uiService = uiService;
        }

        public void Initialize()
        {
            // create ui for economy here as economy controller is used for both enemy and player 
            _uiService.CreateUi(UiType.Economy);
            
            _spaceStationViewFacade.Create(
                PlayerType.Player,
                FactionType,
                _mapModel.Value.GetStationPosition(PlayerType.Player));
            
        }
    }
}