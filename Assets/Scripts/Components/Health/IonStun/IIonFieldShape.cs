using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>Ion-stun field of a unit whose hull changes shape, such as a station replacing its model per level.</summary>
    public interface IIonFieldShape
    {
        /// <param name="bounds">Hull bounds in the unit's view space.</param>
        void SetIonFieldBounds(Bounds bounds);
    }
}
