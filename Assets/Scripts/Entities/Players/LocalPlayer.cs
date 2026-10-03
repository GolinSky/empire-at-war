namespace EmpireAtWar.Models.Players
{
    public sealed class LocalPlayer : ILocalPlayer
    {
        private readonly IPlayerRelations _relations;

        public PlayerId Id => Slot.Id;
        public PlayerSlot Slot { get; }

        public LocalPlayer(IPlayerRoster roster)
        {
            _relations = roster;
            Slot = MatchRules.FindHuman(roster.Players);
        }

        public bool IsLocal(PlayerId owner) => owner == Id;

        public bool IsFriendly(PlayerId owner) => _relations.IsAllied(Id, owner);

        public bool IsHostile(PlayerId owner) => _relations.IsHostile(Id, owner);

        public OwnerRelation GetRelation(PlayerId owner)
        {
            if (owner.IsNone)
            {
                return OwnerRelation.Neutral;
            }

            if (IsLocal(owner))
            {
                return OwnerRelation.Own;
            }

            return IsFriendly(owner) ? OwnerRelation.Ally : OwnerRelation.Enemy;
        }
    }
}
