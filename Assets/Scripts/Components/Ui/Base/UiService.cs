using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    public interface IUiService
    {
        BaseUi CreateUi(UiType uiType);
    }
    
    public class UiService: MonoBehaviour, IUiService
    {
        [SerializeField] private Transform _location;
        private UiFacade _uiFacade;

        [Inject]
        public void Constructor(UiFacade uiFacade)
        {
            _uiFacade = uiFacade;
        }
        
        public BaseUi CreateUi(UiType uiType)
        {
            return _uiFacade.Create(uiType, _location);
        }
    }
}
