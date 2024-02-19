using System;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IShipUnitView
    {
        int Id { get; }
        Vector3 Position { get; }
    }
}