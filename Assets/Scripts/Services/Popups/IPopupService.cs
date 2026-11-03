using System;

namespace EmpireAtWar.Services.Popups
{
    public interface IPopupService
    {
        /// <summary>Queues an accept/decline popup; <paramref name="answered"/> receives true on accept.</summary>
        void ShowAccept(AcceptPopupContent content, Action<bool> answered);
    }
}
