namespace EmpireAtWar.Entities.MainMenu.ServerBrowser
{
    public readonly struct ServerBrowserRowState
    {
        public readonly string Host;
        public readonly string Mode;
        public readonly string Players;

        public ServerBrowserRowState(string host, string mode, string players)
        {
            Host = host;
            Mode = mode;
            Players = players;
        }
    }
}
