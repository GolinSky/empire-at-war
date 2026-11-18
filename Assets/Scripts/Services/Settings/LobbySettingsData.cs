using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>Skirmish lobby choices that survive restarts.</summary>
    [Serializable]
    public sealed class LobbySettingsData
    {
        public const int SLOT_COUNT = 8;

        // Per lobby row: the last chosen occupant + 1, so 0 means "never chosen" and the lobby default applies.
        [SerializeField] private int[] occupantChoices = new int[SLOT_COUNT];

        public int GetOccupantChoice(int slotIndex) =>
            slotIndex < occupantChoices.Length ? occupantChoices[slotIndex] - 1 : -1;

        public void SetOccupantChoice(int slotIndex, int occupant)
        {
            if (occupantChoices.Length < SLOT_COUNT) Array.Resize(ref occupantChoices, SLOT_COUNT);
            occupantChoices[slotIndex] = occupant + 1;
        }

        public bool Matches(LobbySettingsData other)
        {
            if (occupantChoices.Length != other.occupantChoices.Length) return false;
            for (int i = 0; i < occupantChoices.Length; i++)
                if (occupantChoices[i] != other.occupantChoices[i]) return false;
            return true;
        }
    }
}
