using System.Collections.Generic;
using EmpireAtWar.Commands.PopupCommands;
using EmpireAtWar.Ui.Popups;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Popup
{
    public interface IPopupService : IService
    {
        void OpenPopup(PopupType popupType);
        void ClosePopup(PopupType popupType);
    }
    
    public class PopupService : Service, IPopupService,IPopupCommand
    {

        private readonly PopupUiFacade popupUiFacade;
        private readonly Transform popupParent;

        private Dictionary<PopupType, PopupUi> popupDictionary = new Dictionary<PopupType, PopupUi>();

        public PopupService(PopupUiFacade popupUiFacade, Transform popupParent)
        {
            this.popupUiFacade = popupUiFacade;
            this.popupParent = popupParent;
        }

        public void OpenPopup(PopupType popupType)
        {
            if (popupDictionary.TryGetValue(popupType, out PopupUi popupUI))
            {
                popupUI.OpenPopup();
            }
            else
            {
                PopupUi newPopupUI = popupUiFacade.Create(popupType);
                popupDictionary.Add(popupType, newPopupUI);
                newPopupUI.SetParent(popupParent);
                newPopupUI.OpenPopup();
            }
        }

        public void ClosePopup(PopupType popupType)
        {
            if (popupDictionary.TryGetValue(popupType, out PopupUi popupUI))
            {
                popupUI.ClosePopup();
            }
            else
            {
                Debug.LogError($"No popup with id {popupType} found for closing");
            }
        }
    }
}