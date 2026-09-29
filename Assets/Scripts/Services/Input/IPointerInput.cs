using System;
using UnityEngine;

namespace EmpireAtWar.Services.Input
{
    /// <summary>Raw pointer state. Stays active while gameplay input is locked.</summary>
    public interface IPointerInput
    {
        event Action<Vector2> PrimaryPressed;
        event Action<Vector2> PrimaryReleased;

        Vector2 Position { get; }
        int ClickCount { get; }
    }
}
