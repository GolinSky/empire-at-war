using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Focus fire claim for unit tests that do not exercise the cap: every target has a slot.</summary>
    public sealed class FreeFocusFireClaim : IFocusFireClaim
    {
        public static readonly FreeFocusFireClaim Instance = new FreeFocusFireClaim();

        public bool HasSlot(IEntity target) => true;

        public bool TryClaim(IEntity target) => true;

        public void Release()
        {
        }
    }
}
