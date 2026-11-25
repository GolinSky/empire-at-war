using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Focus fire slots for tests that do not exercise the cap: every target is unlimited.</summary>
    public sealed class UnlimitedFocusFireSlots : IFocusFireSlots
    {
        public static readonly UnlimitedFocusFireSlots Instance = new UnlimitedFocusFireSlots();

        public event Action<IEntity> Changed
        {
            add { }
            remove { }
        }

        public int GetCap(IEntity target, bool isSquadron) => FocusFireCaps.UNLIMITED;

        public int CountHolders(PlayerId team, IEntity target, bool isSquadron) => 0;

        public int FreeSlots(PlayerId team, IEntity target, bool isSquadron) => int.MaxValue;

        public bool Holds(IEntity attacker, IEntity target) => false;

        public bool HasSlotFor(IEntity attacker, IEntity target) => true;

        public bool TryClaim(IEntity attacker, IEntity target) => true;

        public void Release(long attackerId)
        {
        }

        public void CopyHeldTargets(List<IEntity> destination) => destination.Clear();
    }
}
