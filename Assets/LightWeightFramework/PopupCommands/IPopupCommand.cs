using EmpireAtWar.Services.Popup;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.LightWeightFramework.PopupCommands
{
    public interface IPopupCommand:ICommand
    {
        void ClosePopup(PopupType popupType);
    }
    
}