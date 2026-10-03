namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// The human sitting at this machine. Answers every "is this mine / my ally / my enemy" question
    /// for input, UI, fog of war, audio and other presentation code.
    /// </summary>
    public interface ILocalPlayer
    {
        PlayerId Id { get; }
        PlayerSlot Slot { get; }

        bool IsLocal(PlayerId owner);

        /// <summary>The local player or one of its allies.</summary>
        bool IsFriendly(PlayerId owner);

        bool IsHostile(PlayerId owner);

        OwnerRelation GetRelation(PlayerId owner);
    }
}
