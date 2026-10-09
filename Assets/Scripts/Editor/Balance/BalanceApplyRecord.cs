using System;
using System.Collections.Generic;

namespace EmpireAtWar.Editor.Balance
{
    [Serializable]
    public sealed class BalanceApplyRecord
    {
        public bool Complete;
        public List<BalanceFileSnapshot> Files = new List<BalanceFileSnapshot>();
    }
}
