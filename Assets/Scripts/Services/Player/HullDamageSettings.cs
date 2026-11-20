using EmpireAtWar.Mvc;
using UnityEngine;

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
        [Tooltip("World units between possible mark spots. Smaller = smaller, denser marks.")]
        [SerializeField, Min(0.01f)] private float markSpacing = 8f;
        [Tooltip("Largest mark radius as a share of the spacing. Clamped to about 0.3 so a mark never spills past its search range.")]
        [SerializeField, Range(0.05f, 0.5f)] private float markSize = 0.3f;
        [Tooltip("Share of mark spots in use at 0 hull. Each damage step switches on 1/15 of them.")]
        [SerializeField, Range(0f, 1f)] private float markDensity = 0.85f;
        [Tooltip("How far the detail texture frays the round edge, in mark radii.")]
        [SerializeField, Range(0f, 0.6f)] private float edgeBreakup = 0.35f;

        [Header("Surface")]
        [SerializeField] private Color sootColor = new Color(0.06f, 0.05f, 0.045f, 1f);
        [SerializeField, Range(0f, 1f)] private float sootOpacity = 0.7f;
        [SerializeField] private Color blisterColor = new Color(0.32f, 0.3f, 0.28f, 1f);

        public Texture2D DetailMap => detailMap;
        public float DetailSize => detailSize;
        public float MarkSpacing => markSpacing;
        public float MarkSize => markSize;
        public float MarkDensity => markDensity;
        public float EdgeBreakup => edgeBreakup;
        public Color SootColor => sootColor;
        public float SootOpacity => sootOpacity;
        public Color BlisterColor => blisterColor;
    }
}
