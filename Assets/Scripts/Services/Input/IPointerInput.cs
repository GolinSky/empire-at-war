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
        /// <summary>False until the device reports a real position; until then <see cref="Position"/> reads zero.</summary>
        bool HasPosition { get; }
        int ClickCount { get; }
    }
}
