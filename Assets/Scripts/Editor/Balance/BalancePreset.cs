using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Editor.Balance
{
    // Editor assembly only: no runtime service or Addressables registration.
    public sealed class BalancePreset : ScriptableObject
    {
        public const int CURRENT_VERSION = 1;
        [SerializeField] private int version = CURRENT_VERSION;
        [SerializeField] private bool fullScope;
        [SerializeField] private List<BalanceSnapshot> values = new List<BalanceSnapshot>();
        public int Version => version;
        public bool FullScope => fullScope;
        public IReadOnlyList<BalanceSnapshot> Values => values;

        public void SetValues(bool full, List<BalanceSnapshot> snapshots)
        {
            version = CURRENT_VERSION;
            fullScope = full;
            values = snapshots;
        }
    }
}
