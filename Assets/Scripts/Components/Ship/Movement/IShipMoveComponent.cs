using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>Movement as the owning ship entity sees it: behaviour API plus wiring and notifications.</summary>
    public interface IShipMoveComponent : IComponent, IShipMovement
    {
        event Action<Vector3> DestinationChanged;
        event Action<Vector3> LookingAt;
        event Action Stopped;
        event Action HyperSpaceCompleted;
        float NavigationSpeed { get; }
        void ApplyMoveCoefficient(float coefficient);
        void HandleSelection(bool isSelected);
        void HandleRadarContacts(IReadOnlyList<RadarContact> contacts);
    }
}
