using System;

namespace EmpireAtWar.Services.Popups
{
    /// <summary>Shows one popup at a time and reports the player's answer once.</summary>
    public interface IPopupController
    {
        void Show(AcceptPopupContent content, Action<bool> answered);
    }
}
