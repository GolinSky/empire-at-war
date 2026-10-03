namespace EmpireAtWar.Services.Tooltip
{
    public interface ITooltipService
    {
        TooltipHandle Show(ITooltipContentProvider provider, TooltipAnchor anchor);

        void Hide(TooltipHandle handle);

        void HideAll();
    }
}
