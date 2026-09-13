using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IHardPointView
    {
        bool IsDestroyed { get; }
        int Id { get; }
        HardPointType HardPointType { get; }
        Vector3 Position { get; }
        Transform Transform { get; }
        void UpdateData(float healthPercentage);
    }
}
