using System;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraInput
    {
        event Action<Vector2> Panned;

        event Action<float> Zoomed;

        /// <summary>Keyboard and screen-edge movement, clamped to unit length.</summary>
        Vector2 Move { get; }
    }
}
