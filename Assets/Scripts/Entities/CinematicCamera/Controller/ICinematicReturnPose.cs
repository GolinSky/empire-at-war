using UnityEngine;

namespace EmpireAtWar.Entities.CinematicCamera.Controller
{
    /// <summary>The gameplay camera pose the cinematic mode returns to when it exits.</summary>
    public interface ICinematicReturnPose
    {
        /// <summary>False while the cinematic mode is inactive; the live camera is the gameplay pose then.</summary>
        bool TryGetReturnPose(out Vector3 position, out Quaternion rotation);
    }
}
