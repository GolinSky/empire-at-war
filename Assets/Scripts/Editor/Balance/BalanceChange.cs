using System;

namespace EmpireAtWar.Editor.Balance
{
    [Serializable]
    public sealed class BalanceChange
    {
        public string Key;
        public string Schema;
        public string Before;
        public string After;

        public BalanceChange(string key, string schema, string before, string after)
        {
            Key = key;
            Schema = schema;
            Before = before;
            After = after;
        }
    }
}
