using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.UnitOrders
{
    public interface IFocusFireCaps
    {
        /// <summary>Main-target slots <paramref name="target"/> offers one team's ships or squadrons; 0 = unlimited.</summary>
        int GetCap(IEntity target, bool isSquadron);
    }
}
