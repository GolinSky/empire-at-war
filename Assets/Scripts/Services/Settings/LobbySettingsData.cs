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
        // The online lobby hosts its own rows (open seats), so it remembers separately from the offline one.
        [SerializeField] private int[] onlineOccupantChoices = new int[SLOT_COUNT];

        public int GetOccupantChoice(int slotIndex, bool online = false) =>
            slotIndex < Choices(online).Length ? Choices(online)[slotIndex] - 1 : -1;

        public void SetOccupantChoice(int slotIndex, int occupant, bool online = false)
        {
            if (online)
            {
                if (onlineOccupantChoices.Length < SLOT_COUNT) Array.Resize(ref onlineOccupantChoices, SLOT_COUNT);
            }
            else if (occupantChoices.Length < SLOT_COUNT) Array.Resize(ref occupantChoices, SLOT_COUNT);
            Choices(online)[slotIndex] = occupant + 1;
        }

        public bool Matches(LobbySettingsData other) =>
            Same(occupantChoices, other.occupantChoices) && Same(onlineOccupantChoices, other.onlineOccupantChoices);

        private int[] Choices(bool online) => online ? onlineOccupantChoices : occupantChoices;

        private static bool Same(int[] first, int[] second)
        {
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (first[i] != second[i]) return false;
            return true;
        }
    }
}
