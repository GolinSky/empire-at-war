using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmpireAtWar.Services.Player
{
    /// <summary>Look of hull damage on every EmpireAtWar/Ship Lit unit (ShipHullDamage.hlsl). Tune in the preview window.</summary>
    [CreateAssetMenu(fileName = nameof(HullDamageSettings), menuName = "Data/HullDamageSettings")]
    public sealed class HullDamageSettings : Data
    {
        [Header("Detail")]
        [Tooltip("R height, G cracks, B roughness. Tools/Empire At War/Bake Hull Damage Texture.")]
        [SerializeField] private Texture2D detailMap;
        [SerializeField, Min(0.0001f)] private float detailSize = 4f;

        [Header("Marks")]
        [Tooltip("Smallest mark radius in world units, before the ship material's Mark Scale.")]
        [SerializeField, Min(0.01f)] private float markRadiusMin = 3.5f;
        [Tooltip("Largest mark radius in world units, before the ship material's Mark Scale. Also sets the spacing of mark spots.")]
        [SerializeField, Min(0.01f)] private float markRadiusMax = 7f;
        [Tooltip("Share of mark spots in use at 0 hull. Each damage step switches on 1/15 of them.")]
        [SerializeField, Range(0f, 1f)] private float markDensity = 1f;
        [Tooltip("How far the detail texture frays the round edge, in mark radii.")]
        [SerializeField, Range(0f, 0.6f)] private float edgeBreakup = 0.35f;

        [Header("Surface")]
        [SerializeField] private Color sootColor = new Color(0.06f, 0.05f, 0.045f, 1f);
        [Tooltip("Each mark gets its own opacity between these two; most marks stay near the lowest.")]
        [FormerlySerializedAs("sootOpacity")]
        [SerializeField, Range(0f, 1f)] private float sootOpacityMin = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sootOpacityMax = 1f;
        [SerializeField] private Color blisterColor = new Color(0.32f, 0.3f, 0.28f, 1f);

        public Texture2D DetailMap => detailMap;
        public float DetailSize => detailSize;
        public float MarkRadiusMin => markRadiusMin;
        public float MarkRadiusMax => markRadiusMax;
        public float MarkDensity => markDensity;
        public float EdgeBreakup => edgeBreakup;
        public Color SootColor => sootColor;
        public float SootOpacityMin => sootOpacityMin;
        public float SootOpacityMax => sootOpacityMax;
        public Color BlisterColor => blisterColor;
    }
}
