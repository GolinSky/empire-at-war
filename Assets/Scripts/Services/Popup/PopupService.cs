using System.Collections.Generic;
using EmpireAtWar.Commands.PopupCommands;
using EmpireAtWar.Ui.Popups;
using UnityEngine;
using LightWeightFramework.Command;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.Popup
{
    public interface IPopupService : IService
    {
        void OpenPopup(PopupType popupType);
        void ClosePopup(PopupType popupType);
    }
    
    public class PopupService : Service, IPopupService,IPopupCommand
    {
        private readonly PopupUiFacade _popupUiFacade;
        private readonly Transform _popupParent;

        private Dictionary<PopupType, PopupUi> _popupDictionary = new Dictionary<PopupType, PopupUi>();

        public PopupService(PopupUiFacade popupUiFacade, Transform popupParent)
        {
            _popupUiFacade = popupUiFacade;
            _popupParent = popupParent;
        }

        public void OpenPopup(PopupType popupType)
        {
            if (_popupDictionary.TryGetValue(popupType, out PopupUi popupUI))
            {
                popupUI.OpenPopup();
            }
            else
            {
                PopupUi newPopupUI = _popupUiFacade.Create(popupType);
                _popupDictionary.Add(popupType, newPopupUI);
                newPopupUI.SetParent(_popupParent);
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