using System;

namespace EmpireAtWar.Editor.Balance
{
    [Serializable]
    public sealed class BalanceSnapshot
    {
        public string Key;
        public string Schema;
        public string Value;

        public BalanceSnapshot(string key, string schema, string value)
        {
            Key = key;
            Schema = schema;
            Value = value;
        }
    }
}
