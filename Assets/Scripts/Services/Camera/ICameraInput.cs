using System;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraInput
    {
        event Action<Vector2> Panned;

        /// <summary>Held zoom axis (keyboard), raised every frame it is non-zero.</summary>
        event Action<float> Zoomed;

        /// <summary>Mouse-wheel notches this frame; one notch is ±1.</summary>
        event Action<float> ZoomScrolled;

        /// <summary>Keyboard and screen-edge movement, clamped to unit length.</summary>
        Vector2 Move { get; }
    }
}
