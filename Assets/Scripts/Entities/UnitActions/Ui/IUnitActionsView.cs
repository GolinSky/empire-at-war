using EmpireAtWar.Components.Ui.Tooltip;
using System;

namespace EmpireAtWar.Entities.UnitActions.Ui
{
    public interface IUnitActionsView
    {
        TooltipHoverView TooltipHover { get; }
        event Action<UnitActionId> ActionPressed;

        void Initialize();

        void Dispose();

        void SetVisible(bool visible);

        void SetAvailable(UnitActionId action, bool available);

        void SetPending(UnitActionId? action);
    }
}
