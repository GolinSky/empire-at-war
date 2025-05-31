using EmpireAtWar.Services.Location;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Ui.Base
{
    public interface IUiService
    {
        void CreateUi(UiType uiType);
    }
    
    public class UiService: Service, IUiService
    {
        private const LocationType DEFAULT_LOCATION_TYPE = LocationType.UiCanvas;
        
        private readonly ILocationService _locationService;
        private readonly UiFacade _uiFacade;

        public UiService(ILocationService locationService, UiFacade uiFacade)
        {
            _locationService = locationService;
            _uiFacade = uiFacade;
        }        
        
        public void CreateUi(UiType uiType)
        {
            _uiFacade.Create(uiType, _locationService.GetLocation(DEFAULT_LOCATION_TYPE));
        }
    }
}