using System;
using System.Collections.Generic;

namespace EmpireAtWar.Editor.Balance
{
    [Serializable]
    public sealed class BalanceDraftState
    {
        public List<BalanceChange> Changes = new List<BalanceChange>();
    }
}
