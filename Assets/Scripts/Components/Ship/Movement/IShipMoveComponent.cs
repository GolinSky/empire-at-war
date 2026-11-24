using System;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>Movement as the owning ship entity sees it: behaviour API plus wiring and notifications.</summary>
    public interface IShipMoveComponent : IComponent, IShipMovement
    {
        event Action<Vector3> DestinationChanged;

        event Action<Vector3> LookingAt;


        event Action HyperSpaceCompleted;

        /// <summary>Raised when <see cref="IShipMovement.IsMoving"/> flips; the argument is the new value.</summary>
        event Action<bool> MovingChanged;

        float NavigationSpeed { get; }

        void ApplyMoveCoefficient(float coefficient);

        void HandleSelection(bool isSelected);

    }
}
