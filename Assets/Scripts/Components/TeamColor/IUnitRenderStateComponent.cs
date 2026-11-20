namespace EmpireAtWar.Components.TeamColor
{
    public interface IUnitRenderStateComponent
    {
        uint WreckUserValue { get; }

        void SetHighlight(UnitHighlight flags);

        void SetHullDamage(float hullPercentage);
    }
}
