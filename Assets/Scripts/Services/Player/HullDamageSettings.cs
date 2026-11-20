using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    /// <summary>Look of hull damage on every EmpireAtWar/Ship Lit unit (ShipHullDamage.hlsl). Starting values; tune in the preview.</summary>
    [CreateAssetMenu(fileName = nameof(HullDamageSettings), menuName = "Data/HullDamageSettings")]
    public sealed class HullDamageSettings : Data
    {
        [Header("Detail")]
        [Tooltip("R height, G cracks, B roughness, A ember mask. Tools/Empire At War/Bake Hull Damage Texture.")]
        [SerializeField] private Texture2D detailMap;
        [SerializeField, Min(0.0001f)] private float detailSize = 12f;

        [Header("Patches")]
        [SerializeField, Min(0.01f)] private float patchSize = 12f;
        [SerializeField, Range(0.01f, 0.5f)] private float edgeWidth = 0.08f;
        [SerializeField, Range(0f, 1f)] private float heightBlend = 0.25f;
        [Tooltip("How far the burn threshold drops at full damage; higher covers more of the hull.")]
        [SerializeField, Range(0f, 1f)] private float coverage = 0.7f;

        [Header("Surface")]
        [SerializeField] private Color charColor = new Color(0.07f, 0.06f, 0.055f, 1f);
        [SerializeField] private Color metalColor = new Color(0.2f, 0.19f, 0.18f, 1f);
        [SerializeField, Min(0f)] private float normalStrength = 0.4f;

        [Header("Embers")]
        [SerializeField, ColorUsage(false, true)] private Color emberColor = new Color(1f, 0.4f, 0.1f, 1f);
        [SerializeField, Min(0f)] private float emberStrength = 3f;
        [SerializeField, Min(0f)] private float emberFlickerSpeed = 2f;

        public Texture2D DetailMap => detailMap;
        public float DetailSize => detailSize;
        public float PatchSize => patchSize;
        public float EdgeWidth => edgeWidth;
        public float HeightBlend => heightBlend;
        public float Coverage => coverage;
        public Color CharColor => charColor;
        public Color MetalColor => metalColor;
        public float NormalStrength => normalStrength;
        public Color EmberColor => emberColor;
        public float EmberStrength => emberStrength;
        public float EmberFlickerSpeed => emberFlickerSpeed;
    }
}
