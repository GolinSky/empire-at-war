namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>What <see cref="EntitySpawnMessage.Payload"/> holds: a unit or structure save, or nothing to spawn.</summary>
    public enum EntitySpawnKind : byte
    {
        Untracked,
        Unit,
        Structure
    }
}
