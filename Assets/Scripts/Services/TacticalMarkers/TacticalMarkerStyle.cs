using System;
using UnityEngine;

namespace EmpireAtWar.Services.TacticalMarkers
{
    [Serializable]
    public sealed class TacticalMarkerStyle
    {
        public TacticalMarkerType type;
        public float lifetime;
        public Color color;
        public Sprite icon;
        public AudioClip sound;
    }
}
