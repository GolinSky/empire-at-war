using System;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Services.Popups
{
    public interface IPopupController
    {
        /// <summary>Creates the popup view. Invoke complete after a player action to close it.</summary>
        BaseUi Open(Action complete);
        void SetCovered(bool covered);
        void Cancel();
        void Dispose(bool dismissed);
    }
}
