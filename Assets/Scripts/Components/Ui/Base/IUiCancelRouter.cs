using System;

namespace EmpireAtWar.Ui.Base
{
    public interface IUiCancelRouter
    {
        /// <summary>Raised when cancel was pressed and no focused UI consumed it.</summary>
        event Action CancelUnhandled;

        /// <summary>Makes the handler the selected UI: it gets the next cancel request first.</summary>
        void Focus(IUiCancelHandler handler);

        void Unfocus(IUiCancelHandler handler);
    }
}
