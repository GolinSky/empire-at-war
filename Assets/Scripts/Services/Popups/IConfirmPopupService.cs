using System;

namespace EmpireAtWar.Services.Popups
{
    public interface IConfirmPopupService
    {
        PopupHandle ShowAccept(AcceptPopupContent content, Action<ConfirmPopupResult> answered);
        void Close(PopupHandle handle);
    }
}
