using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmpireAtWar.Services.Player
{
    /// <summary>Look of hull damage on every EmpireAtWar/Ship Lit unit (ShipHullDamage.hlsl). Tune in the preview window.</summary>
    [CreateAssetMenu(fileName = nameof(HullDamageSettings), menuName = "Data/HullDamageSettings")]
    public sealed class HullDamageSettings : Data
    {
        [Header("Marks")]
        [Tooltip("Smallest mark radius in world units, before the ship material's Mark Scale.")]
        [SerializeField, Min(0.01f)] private float markRadiusMin = 3.5f;
        [Tooltip("Largest mark radius in world units, before the ship material's Mark Scale. Also sets the spacing of mark spots.")]
        [SerializeField, Min(0.01f)] private float markRadiusMax = 6.1f;
        [Tooltip("Share of mark spots in use at 0 hull. Each damage step switches on 1/15 of them.")]
        [SerializeField, Range(0f, 1f)] private float markDensity = 1f;
        [Tooltip("How far smooth noise waves the outline in and out, in mark radii.")]
        [SerializeField, Range(0f, 0.6f)] private float edgeBreakup = 0.2f;
        [Tooltip("Waves along a mark's outline per mark spacing. Higher = more ragged outline.")]
        [SerializeField, Min(0.1f)] private float edgeNoiseFrequency = 3f;

        [Header("Star Shape")]
        [Tooltip("How deep the gaps between a mark's rays cut in. 0 = round marks.")]
        [SerializeField, Range(0f, 1f)] private float starStrength = 0.8f;
        [Tooltip("Marks smaller than this share of the size range stay round. 0 = every mark is a star.")]
        [SerializeField, Range(0f, 1f)] private float starMinSize = 0f;

        [Header("Soot")]
        [SerializeField] private Color sootColor = new Color(0.015f, 0.013f, 0.012f, 1f);
        [Tooltip("Middle-band opacity: each mark picks one between these two; most stay near the lowest. The centre is always fully opaque.")]
        [FormerlySerializedAs("sootOpacity")]
        [SerializeField, Range(0f, 1f)] private float sootOpacityMin = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sootOpacityMax = 1f;
        [Tooltip("Opacity near the outline, before the final fade.")]
        [SerializeField, Range(0f, 1f)] private float sootOpacityEdge = 0.15f;

        [Header("Burnt Paint")]
        [Tooltip("Thin glowing lines along each mark's rays, from the centre out to this share of the outline.")]
        [SerializeField, Range(0f, 1f)] private float emberLength = 0.45f;
        [Tooltip("Line width in mark radii.")]
        [SerializeField, Range(0.005f, 0.2f)] private float emberWidth = 0.012f;
        [SerializeField] private Color emberColor = new Color(1f, 0.4f, 0.1f, 1f);
        [Tooltip("Glow brightness; keep low so the lines stay faint.")]
        [SerializeField, Min(0f)] private float emberStrength = 0.35f;

        public float MarkRadiusMin => markRadiusMin;
        public float MarkRadiusMax => markRadiusMax;
        public float MarkDensity => markDensity;
        public float EdgeBreakup => edgeBreakup;
        public float EdgeNoiseFrequency => edgeNoiseFrequency;
        public float StarStrength => starStrength;
        public float StarMinSize => starMinSize;
        public Color SootColor => sootColor;
        public float SootOpacityMin => sootOpacityMin;
        public float SootOpacityMax => sootOpacityMax;
        public float SootOpacityEdge => sootOpacityEdge;
        public float EmberLength => emberLength;
        public float EmberWidth => emberWidth;
        public Color EmberColor => emberColor;
        public float EmberStrength => emberStrength;
    }
}
