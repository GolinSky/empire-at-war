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

        event Action<Vector2> WorldCommanded;

        event Action<Vector2> DragStarted;

        event Action<Vector2> DragChanged;

        event Action<Vector2> DragEnded;
    }
}
