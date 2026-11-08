namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    public static class UnitOrderTypeExtensions
    {
        /// <summary>Orders that send the unit somewhere; units answer them with a move line.</summary>
        public static bool IsMoveOrder(this UnitOrderType order) =>
            order == UnitOrderType.Move || order == UnitOrderType.WaypointMove ||
            order == UnitOrderType.Retreat || order == UnitOrderType.AttackMove;

        /// <summary>Orders that chase a target; units answer them with an attack line.</summary>
        public static bool IsAttackOrder(this UnitOrderType order) =>
            order == UnitOrderType.Attack || order == UnitOrderType.Hunt;
    }
}
