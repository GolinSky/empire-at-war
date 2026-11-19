using System;
using UnityEngine;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Pointer gestures aimed at the battlefield: ignored over UI and while gameplay input is locked.
    /// </summary>
    public interface IPointerGestures
    {
        event Action<Vector2> WorldPressed;

        event Action<Vector2> WorldClicked;

        event Action<Vector2> WorldCommandPressed;

        event Action<Vector2> WorldCommanded;

        /// <summary>
        /// Successive command (right) clicks ending in the current <see cref="WorldCommanded"/>: 2 for a double
        /// right-click. Counts the command button only, unlike the device click count.
        /// </summary>
        int CommandClickCount { get; }

        event Action<Vector2> DragStarted;

        event Action<Vector2> DragChanged;

        event Action<Vector2> DragEnded;
        event Action DragCanceled;
    }
}
