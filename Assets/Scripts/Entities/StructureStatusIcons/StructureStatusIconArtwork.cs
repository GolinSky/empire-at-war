using System;
using UnityEngine;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    [Serializable]
    public sealed class StructureStatusIconArtwork
    {
        [SerializeField] private Sprite captured;
        [SerializeField] private Sprite destroyed;
        [SerializeField] private Sprite buildAvailable;

        public Sprite GetSprite(StructureStatusIconState state) => state switch
        {
            StructureStatusIconState.Captured => captured,
            StructureStatusIconState.Destroyed => destroyed,
            StructureStatusIconState.BuildAvailable => buildAvailable,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        public Color GetColor(StructureStatusIconState state) => state switch
        {
            StructureStatusIconState.Captured => new Color32(210, 169, 94, 255),
            StructureStatusIconState.Destroyed => new Color32(232, 110, 108, 255),
            StructureStatusIconState.BuildAvailable => new Color32(109, 184, 226, 255),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }
}
