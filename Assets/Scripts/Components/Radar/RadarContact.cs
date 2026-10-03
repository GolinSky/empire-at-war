using UnityEngine;

namespace EmpireAtWar.Components.Radar
{
    public readonly struct RadarContact
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        public bool IsShip { get; }

        public RadarContact(Vector3 position, float radius, bool isShip)
        {
            Position = position;
            Radius = radius;
            IsShip = isShip;
        }
    }
}
