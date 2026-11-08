namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>Why an online session failed or ended; shown on the online menu's error line.</summary>
    public enum OnlineError
    {
        None,
        SteamNotRunning,
        LobbyFull,
        VersionMismatch,
        HostLeft,
        Kicked,
        ConnectionFailed,
        BattleInProgress
    }
}
