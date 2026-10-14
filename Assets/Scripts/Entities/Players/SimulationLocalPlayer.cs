namespace EmpireAtWar.Models.Players
{
    public sealed class SimulationLocalPlayer : ILocalPlayer
    {
        private readonly IPlayerRoster _roster;
        public SimulationLocalPlayer(IPlayerRoster roster) => _roster = roster;
        public PlayerId Id => new PlayerId(0);
        public PlayerSlot Slot => _roster.Get(Id);
        public bool IsLocal(PlayerId owner) => false;
        public bool IsFriendly(PlayerId owner) => _roster.IsAllied(Id, owner);
        public bool IsHostile(PlayerId owner) => _roster.IsHostile(Id, owner);
        public OwnerRelation GetRelation(PlayerId owner) => owner.IsNone ? OwnerRelation.Neutral :
            IsFriendly(owner) ? OwnerRelation.Ally : OwnerRelation.Enemy;
    }
}
