using EmpireAtWar.Services.Popup;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.PopupCommands
{
    public interface IPopupCommand:ICommand
    {
        void ClosePopup(PopupType popupType);
    }
    
}