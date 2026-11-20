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
        [Tooltip("How far smooth noise waves the round edge in and out, in mark radii.")]
        [SerializeField, Range(0f, 0.6f)] private float edgeBreakup = 0.35f;
        [Tooltip("Waves along a mark's edge per mark spacing. Higher = more ragged outline.")]
        [SerializeField, Min(0.1f)] private float edgeNoiseFrequency = 3f;

        [Header("Surface")]
        [SerializeField] private Color sootColor = new Color(0.015f, 0.013f, 0.012f, 1f);
        [Tooltip("Each mark gets its own opacity between these two; most marks stay near the lowest.")]
        [FormerlySerializedAs("sootOpacity")]
        [SerializeField, Range(0f, 1f)] private float sootOpacityMin = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sootOpacityMax = 1f;
        [Tooltip("Soot opacity at the very centre of every mark; fades out to the mark's own opacity.")]
        [SerializeField, Range(0f, 1f)] private float sootOpacityCenter = 1f;
        [Tooltip("Share of the mark radius where the solid centre has faded to the mark's own opacity. Solid up to half of it.")]
        [SerializeField, Range(0.05f, 1f)] private float centerFade = 0.8f;

        public float MarkRadiusMin => markRadiusMin;
        public float MarkRadiusMax => markRadiusMax;
        public float MarkDensity => markDensity;
        public float EdgeBreakup => edgeBreakup;
        public float EdgeNoiseFrequency => edgeNoiseFrequency;
        public Color SootColor => sootColor;
        public float SootOpacityMin => sootOpacityMin;
        public float SootOpacityMax => sootOpacityMax;
        public float SootOpacityCenter => sootOpacityCenter;
        public float CenterFade => centerFade;
    }
}
