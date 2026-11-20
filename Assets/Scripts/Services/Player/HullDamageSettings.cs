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
        [Tooltip("How deep the gaps between a large mark's rays cut in. 0 = round marks.")]
        [SerializeField, Range(0f, 1f)] private float starStrength = 0.8f;
        [Tooltip("Marks smaller than this share of the size range stay round; rays grow in above it.")]
        [SerializeField, Range(0f, 1f)] private float starMinSize = 0.35f;

        [Header("Soot")]
        [SerializeField] private Color sootColor = new Color(0.015f, 0.013f, 0.012f, 1f);
        [Tooltip("Middle-band opacity: each mark picks one between these two; most stay near the lowest. The centre is always fully opaque.")]
        [FormerlySerializedAs("sootOpacity")]
        [SerializeField, Range(0f, 1f)] private float sootOpacityMin = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sootOpacityMax = 1f;
        [Tooltip("Opacity near the outline, before the final fade.")]
        [SerializeField, Range(0f, 1f)] private float sootOpacityEdge = 0.15f;

        [Header("Burnt Paint")]
        [Tooltip("Dull flakes in the middle of each mark, as a share of its radius.")]
        [SerializeField, Range(0f, 1f)] private float blisterRadius = 0.3f;
        [SerializeField] private Color blisterColor = new Color(0.2f, 0.18f, 0.16f, 1f);

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
        public float BlisterRadius => blisterRadius;
        public Color BlisterColor => blisterColor;
    }
}
