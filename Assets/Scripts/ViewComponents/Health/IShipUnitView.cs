using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IShipUnitView
    {
        bool IsDestroyed { get; }
        int Id { get; }
        Vector3 Position { get; }
    }
}