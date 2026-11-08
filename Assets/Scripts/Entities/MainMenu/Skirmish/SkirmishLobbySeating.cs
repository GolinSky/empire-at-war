using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>Online lobby rules for the Skirmish rows: who sits where, and the rows as snapshot data.</summary>
    public static class SkirmishLobbySeating
    {
        private const int HOST_ROW = 0;

        /// <summary>The host in row 0, an open seat in row 1, the rest closed.</summary>
        public static void SeatHost(IReadOnlyList<SkirmishSlotSetup> slots, int hostMemberId, string hostName)
        {
            foreach (SkirmishSlotSetup slot in slots)
            {
                slot.Occupant = SkirmishSlotOccupant.Closed;
                slot.ClearMember();
            }

            SkirmishSlotSetup host = slots[HOST_ROW];
            host.Occupant = SkirmishSlotOccupant.Human;
            host.MemberId = hostMemberId;
            host.PlayerName = hostName;
            host.Ready = true;
            slots[HOST_ROW + 1].Occupant = SkirmishSlotOccupant.Vacant;
        }

        /// <summary>
        /// A joining member takes the first open seat, else a closed row, else an AI row; a member who left leaves
        /// an open seat. True when a row changed.
        /// </summary>
        public static bool SeatMembers(IReadOnlyList<SkirmishSlotSetup> slots, IReadOnlyList<SkirmishLobbySeat> members)
        {
            bool changed = false;
            foreach (SkirmishSlotSetup slot in slots)
            {
                if (slot.MemberId == SkirmishModel.NO_MEMBER ||
                    members.Any(member => member.MemberId == slot.MemberId)) continue;
                slot.Occupant = SkirmishSlotOccupant.Vacant;
                slot.ClearMember();
                changed = true;
            }

            foreach (SkirmishLobbySeat member in members)
            {
                SkirmishSlotSetup slot = slots.FirstOrDefault(row => row.MemberId == member.MemberId);
                if (slot == null)
                {
                    slot = FindFreeSeat(slots);
                    slot.Occupant = SkirmishSlotOccupant.Human;
                    slot.MemberId = member.MemberId;
                    changed = true;
                }

                if (slot.PlayerName == member.Name && slot.Ready == member.Ready && slot.SteamId == member.SteamId)
                    continue;
                slot.PlayerName = member.Name;
                slot.Ready = member.Ready;
                slot.SteamId = member.SteamId;
                changed = true;
            }

            return changed;
        }

        /// <summary>Member connection id to its slot index among the playing rows, in row order.</summary>
        public static IReadOnlyDictionary<int, int> CreateMemberSlots(IReadOnlyList<SkirmishSlotSetup> slots)
        {
            Dictionary<int, int> memberSlots = new Dictionary<int, int>();
            int playerIndex = 0;
            foreach (SkirmishSlotSetup slot in slots)
            {
                if (!slot.IsOpen) continue;
                if (slot.MemberId != SkirmishModel.NO_MEMBER) memberSlots.Add(slot.MemberId, playerIndex);
                playerIndex++;
            }

            return memberSlots;
        }

        public static SkirmishLobbySlotSnapshot Capture(SkirmishSlotSetup slot) => new SkirmishLobbySlotSnapshot
        {
            occupant = (int)slot.Occupant, faction = (int)slot.Faction, team = slot.Team, color = slot.ColorIndex,
            memberId = slot.MemberId, name = slot.PlayerName, ready = slot.Ready, steamId = slot.SteamId
        };

        public static void Apply(SkirmishSlotSetup slot, SkirmishLobbySlotSnapshot row)
        {
            slot.Occupant = (SkirmishSlotOccupant)row.occupant;
            slot.Faction = (FactionType)row.faction;
            slot.Team = row.team;
            slot.ColorIndex = row.color;
            slot.MemberId = row.memberId;
            slot.PlayerName = row.name;
            slot.Ready = row.ready;
            slot.SteamId = row.steamId;
        }

        private static SkirmishSlotSetup FindFreeSeat(IReadOnlyList<SkirmishSlotSetup> slots) =>
            slots.FirstOrDefault(slot => slot.Occupant == SkirmishSlotOccupant.Vacant) ??
            slots.FirstOrDefault(slot => slot.Occupant == SkirmishSlotOccupant.Closed) ??
            slots.First(slot => !slot.IsHuman);
    }
}
