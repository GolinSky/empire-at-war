using System.Collections.Generic;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>Host-side lobby members, ready ticks and slot assignment. The host is connection 0 and always ready.</summary>
    public sealed class OnlineLobby
    {
        public const int MAX_MEMBERS = 4;
        public const int HOST_CONNECTION_ID = 0;

        private readonly List<LobbyMember> _members = new List<LobbyMember>();

        public IReadOnlyList<LobbyMember> Members => _members;
        public bool IsFull => _members.Count >= MAX_MEMBERS;

        public bool AllReady
        {
            get
            {
                foreach (LobbyMember member in _members)
                    if (member.Connected && !member.Ready) return false;
                return true;
            }
        }

        public void Reset(string hostName)
        {
            _members.Clear();
            _members.Add(new LobbyMember
                { ConnectionId = HOST_CONNECTION_ID, Name = hostName, Ready = true, SlotIndex = -1, Connected = true });
        }

        public void Replace(LobbyMember[] members)
        {
            _members.Clear();
            _members.AddRange(members);
        }

        public void Add(int connectionId, string name) =>
            _members.Add(new LobbyMember { ConnectionId = connectionId, Name = name, SlotIndex = -1, Connected = true });

        public bool Remove(int connectionId) => _members.RemoveAll(member => member.ConnectionId == connectionId) > 0;

        /// <summary>The slot's player stayed away past the grace period; the AI keeps the slot, so no rejoin.</summary>
        public void RemoveAbandoned(int slotIndex) =>
            _members.RemoveAll(member => !member.Connected && member.SlotIndex == slotIndex);

        public void SetReady(int connectionId, bool ready)
        {
            int index = IndexOf(connectionId);
            if (index < 0) return;
            LobbyMember member = _members[index];
            member.Ready = ready;
            _members[index] = member;
        }

        public void MarkDisconnected(int connectionId)
        {
            int index = IndexOf(connectionId);
            if (index < 0) return;
            LobbyMember member = _members[index];
            member.Connected = false;
            _members[index] = member;
        }

        /// <summary>Rejoin: a disconnected member with the same name takes the new connection and keeps its slot.</summary>
        public bool TryRebind(string name, int connectionId, out int slotIndex)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                LobbyMember member = _members[i];
                if (member.Connected || member.Name != name) continue;
                member.ConnectionId = connectionId;
                member.Connected = true;
                _members[i] = member;
                slotIndex = member.SlotIndex;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        /// <summary>Members take the human slots in line-up order, host first. False when there are fewer human slots.</summary>
        public bool TryAssignSlots(IReadOnlyList<PlayerSlot> slots)
        {
            int next = 0;
            for (int i = 0; i < _members.Count; i++)
            {
                while (next < slots.Count && slots[next].Controller != PlayerController.Human) next++;
                if (next >= slots.Count) return false;
                LobbyMember member = _members[i];
                member.SlotIndex = slots[next++].Id.Index;
                _members[i] = member;
            }

            return true;
        }

        /// <summary>Skirmish: the setup screen already seated every member; <paramref name="memberSlots"/> maps connection id → slot index.</summary>
        public void AssignSlots(IReadOnlyDictionary<int, int> memberSlots)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                LobbyMember member = _members[i];
                member.SlotIndex = memberSlots[member.ConnectionId];
                _members[i] = member;
            }
        }

        public bool TryGetMember(int connectionId, out LobbyMember member)
        {
            foreach (LobbyMember candidate in _members)
            {
                if (candidate.ConnectionId != connectionId) continue;
                member = candidate;
                return true;
            }

            member = default;
            return false;
        }

        private int IndexOf(int connectionId) => _members.FindIndex(member => member.ConnectionId == connectionId);
    }
}
