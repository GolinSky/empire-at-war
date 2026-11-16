using System;
using UnityEngine;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    [Serializable]
    public sealed class StructureStatusIconArtwork
    {
        [SerializeField] private Sprite mining;
        [SerializeField] private Sprite satellite;
        [SerializeField] private Sprite relay;
        [SerializeField] private Sprite captured;
        [SerializeField] private Sprite built;
        [SerializeField] private Sprite destroyed;

        public Sprite GetGlyph(StructureStatusIconKind kind) => kind switch
        {
            StructureStatusIconKind.Mining => mining,
            StructureStatusIconKind.Satellite => satellite,
            StructureStatusIconKind.Relay => relay,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        public Sprite GetBadge(StructureStatusIconState state) => state switch
        {
            StructureStatusIconState.Captured => captured,
            StructureStatusIconState.Built => built,
            StructureStatusIconState.Destroyed => destroyed,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        public Color GetColor(StructureStatusIconState state) => state switch
        {
            StructureStatusIconState.Captured => new Color32(210, 169, 94, 255),
            StructureStatusIconState.Built => new Color32(109, 184, 226, 255),
            StructureStatusIconState.Destroyed => new Color32(232, 110, 108, 255),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }
}
