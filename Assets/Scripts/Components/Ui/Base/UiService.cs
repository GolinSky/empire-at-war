using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    public interface IUiService
    {
        BaseUi CreateUi(UiType uiType);
    }
    
    [RequireComponent(typeof(Canvas))]
    public class UiService : MonoBehaviour, IUiService
    {
        private UiFacade _uiFacade;

        [Inject]
        public void Constructor(UiFacade uiFacade)
        {
            _uiFacade = uiFacade ?? throw new ArgumentNullException(nameof(uiFacade));
        }
        
        public BaseUi CreateUi(UiType uiType)
        {
            if (_uiFacade == null)
                throw new InvalidOperationException($"{nameof(UiService)} has not been initialized.");

            return _uiFacade.Create(uiType, transform);
        }
    }
}
