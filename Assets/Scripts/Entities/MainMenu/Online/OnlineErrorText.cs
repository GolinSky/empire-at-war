using System;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>Player-facing text for an <see cref="OnlineError"/>.</summary>
    public static class OnlineErrorText
    {
        public static string Describe(OnlineError error) => error switch
        {
            OnlineError.SteamNotRunning => "Steam is still not running. Start Steam and sign in, then press RETRY STEAM.",
            OnlineError.LobbyFull => "The lobby is full.",
            OnlineError.VersionMismatch => "Game versions differ.",
            OnlineError.HostLeft => "Host ended the match.",
            OnlineError.ConnectionLost => "Connection lost.",
            OnlineError.ReconnectExpired => "The rejoin window expired. Your slot is no longer available.",
            OnlineError.ReconnectRejected => "This player cannot rejoin that match. Start or join another game.",
            OnlineError.MatchEnded => "That match has ended. Start or join another game.",
            OnlineError.RecoveryRecordInvalid => "The saved rejoin session could not be read and was discarded.",
            OnlineError.RecoveryStorageFailed => "Could not access the saved recovery record. Check local storage permissions.",
            OnlineError.Kicked => "You were removed from the lobby.",
            OnlineError.BattleInProgress => "That battle is already running.",
            OnlineError.SurvivalCoopNeedsLargerMap => "Survival co-op needs a Medium or Large map.",
            OnlineError.SurvivalLobbyTooLarge => "Survival co-op holds at most 3 players.",
            OnlineError.ConnectionFailed => "Could not connect.",
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, "No text for this error.")
        };
    }
}
