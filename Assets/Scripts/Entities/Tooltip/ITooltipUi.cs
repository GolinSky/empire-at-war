using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public interface ITooltipUi
    {
        void Render(TooltipContent content);
        void Place(TooltipAnchor anchor);
        void Show();
        void Hide();
    }
}
