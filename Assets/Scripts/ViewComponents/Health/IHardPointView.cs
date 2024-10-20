using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IHardPointView
    {
        bool IsDestroyed { get; }
        int Id { get; }
        Vector3 Position { get; }
    }
}