using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.PopupCommands;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Ui.Popups;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Popup
{
    public interface IPopupService : IService
    {
        void OpenPopup(PopupType popupType);
        void ClosePopup(PopupType popupType);
    }
    
    public class PopupService : Service, IPopupService, IPopupCommand
    {
        private readonly PopupUiFacade _popupUiFacade;
        private readonly IUiService _uiService;

        private readonly Dictionary<PopupType, PopupUi> _popupDictionary = new();

        public PopupService(PopupUiFacade popupUiFacade, IUiService uiService)
        {
            _popupUiFacade = popupUiFacade ?? throw new ArgumentNullException(nameof(popupUiFacade));
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        }

        public void OpenPopup(PopupType popupType)
        {
            if (_popupDictionary.TryGetValue(popupType, out PopupUi popupUI))
            {
                popupUI.OpenPopup();
            }
            else
            {
                PopupUi newPopupUI = _popupUiFacade.Create(
                    popupType,
                    _uiService.PopupCanvasTransform);
                _popupDictionary.Add(popupType, newPopupUI);
                newPopupUI.OpenPopup();
            }
        }

        public void ClosePopup(PopupType popupType)
        {
            // if (popupDictionary.TryGetValue(popupType, out PopupUi popupUI))
            // {
            //     //popupUI.ClosePopup();
            // }
            // else
            // {
            //     Debug.LogError($"No popup with id {popupType} found for closing");
            // }
        }
    }
}
