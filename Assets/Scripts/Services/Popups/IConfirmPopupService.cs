using System;

namespace EmpireAtWar.Services.Popups
{
    public interface IConfirmPopupService
    {
        void ShowAccept(AcceptPopupContent content, Action<ConfirmPopupResult> answered);
    }
}
