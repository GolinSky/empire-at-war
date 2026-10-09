using System;

namespace EmpireAtWar.Editor.Balance
{
    [Serializable]
    public sealed class BalanceFileSnapshot
    {
        public string Path;
        public string Before;
        public string AfterHash;
    }
}
