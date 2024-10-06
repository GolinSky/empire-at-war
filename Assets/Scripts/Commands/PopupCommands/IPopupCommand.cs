using EmpireAtWar.Services.Popup;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.PopupCommands
{
    public interface IPopupCommand:ICommand
    {
        void ClosePopup(PopupType popupType);
    }
    
}