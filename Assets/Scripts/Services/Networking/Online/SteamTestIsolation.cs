namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>
    /// Editor-session switch that keeps the real Steam API off while a test run is active, including tests that
    /// enter Play Mode. Session state survives the Play Mode domain reload and resets when the Editor restarts.
    /// </summary>
    public static class SteamTestIsolation
    {
        private const string SESSION_KEY = "EmpireAtWar.SteamTestIsolation";

        public static bool IsActive
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetBool(SESSION_KEY, false);
#else
                return false;
#endif
            }
        }

#if UNITY_EDITOR
        public static void SetActive(bool active) => UnityEditor.SessionState.SetBool(SESSION_KEY, active);
#endif
    }
}
