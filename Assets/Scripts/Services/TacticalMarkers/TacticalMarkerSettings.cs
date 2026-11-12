using UnityEngine;

namespace EmpireAtWar.Services.TacticalMarkers
{
    [CreateAssetMenu(menuName = "Empire At War/Tactical Markers")]
    public sealed class TacticalMarkerSettings : ScriptableObject
    {
        [SerializeField] private TacticalMarkerStyle[] styles =
        {
            new TacticalMarkerStyle { type = TacticalMarkerType.Look, lifetime = 8, color = new Color32(200, 205, 212, 255) },
            new TacticalMarkerStyle { type = TacticalMarkerType.Attack, lifetime = 8, color = new Color32(255, 75, 75, 255) },
            new TacticalMarkerStyle { type = TacticalMarkerType.Capture, lifetime = 30, color = new Color32(255, 197, 49, 255) },
            new TacticalMarkerStyle { type = TacticalMarkerType.Defend, lifetime = 30, color = new Color32(63, 140, 255, 255) },
            new TacticalMarkerStyle { type = TacticalMarkerType.Rally, lifetime = 15, color = new Color32(47, 224, 208, 255) },
            new TacticalMarkerStyle { type = TacticalMarkerType.Retreat, lifetime = 8, color = new Color32(176, 96, 255, 255) }
        };
        [SerializeField] private int maxPerPlayer = 4;
        [SerializeField] private int rateLimit = 3;
        [SerializeField] private float rateWindow = 2;
        [SerializeField] private float repeatRadius = 50;
        [SerializeField] private float objectiveRadius = 750;
        [SerializeField] private Sprite octagonFrame;
        [SerializeField] private Sprite squareFrame;
        [SerializeField] private Material worldMaterial;

        public int MaxPerPlayer => maxPerPlayer;
        public int RateLimit => rateLimit;
        public float RateWindow => rateWindow;
        public float RepeatRadius => repeatRadius;
        public float ObjectiveRadius => objectiveRadius;
        public Sprite OctagonFrame => octagonFrame;
        public Sprite SquareFrame => squareFrame;
        public Material WorldMaterial => worldMaterial;
        public float GetLifetime(TacticalMarkerType type) => GetStyle(type).lifetime;
        public Color GetColor(TacticalMarkerType type) => GetStyle(type).color;
        public Sprite GetIcon(TacticalMarkerType type) => GetStyle(type).icon;
        public AudioClip GetSound(TacticalMarkerType type) => GetStyle(type).sound;

        private TacticalMarkerStyle GetStyle(TacticalMarkerType type)
        {
            foreach (TacticalMarkerStyle style in styles)
                if (style.type == type) return style;
            throw new System.InvalidOperationException($"Missing tactical marker style: {type}.");
        }
    }
}
