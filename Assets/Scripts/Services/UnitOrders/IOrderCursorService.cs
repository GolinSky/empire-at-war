namespace EmpireAtWar.Services.UnitOrders
{
    public interface IOrderCursorService
    {
        /// <summary>The cursor icon for the entity under the pointer and the current selection and targeting.</summary>
        OrderCursor Resolve();
    }
}
