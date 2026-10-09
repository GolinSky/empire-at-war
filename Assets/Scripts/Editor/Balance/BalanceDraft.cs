using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpireAtWar.Editor.Balance
{
    // Editor state has no live asset references. Canonical keys link every view to one value.
    [Serializable]
    public sealed class BalanceDraft
    {
        public List<BalanceChange> Changes = new List<BalanceChange>();
        public List<BalanceDraftState> UndoStates = new List<BalanceDraftState>();
        public List<BalanceDraftState> RedoStates = new List<BalanceDraftState>();

        public string Value(string key, string current) =>
            Changes.FirstOrDefault(change => change.Key == key) is BalanceChange change ? change.After : current;

        public void Set(BalanceSnapshot current, string value) => SetMany(new[] { current }, new[] { value });

        public void SetMany(IReadOnlyList<BalanceSnapshot> current, IReadOnlyList<string> values)
        {
            if (current.Count != values.Count) throw new InvalidOperationException("Bulk target/value count differs.");
            Remember();
            for (int i = 0; i < current.Count; i++)
            {
                BalanceSnapshot target = current[i];
                BalanceChange change = Changes.FirstOrDefault(entry => entry.Key == target.Key);
                if (change == null)
                {
                    if (target.Value != values[i]) Changes.Add(new BalanceChange(target.Key, target.Schema, target.Value, values[i]));
                }
                else if (change.Before == values[i]) Changes.Remove(change);
                else change.After = values[i];
            }
        }

        public void ReplaceScope(IReadOnlyList<BalanceSnapshot> preset, IReadOnlyDictionary<string, BalanceSnapshot> current)
        {
            if (preset.Select(entry => entry.Key).Distinct().Count() != preset.Count)
                throw new InvalidOperationException("Preset contains duplicate canonical targets.");
            foreach (BalanceSnapshot entry in preset)
                if (!current.TryGetValue(entry.Key, out BalanceSnapshot target) || target.Schema != entry.Schema)
                    throw new InvalidOperationException($"Preset target is missing or incompatible: {entry.Key}");
            Remember();
            foreach (BalanceSnapshot entry in preset)
            {
                Changes.RemoveAll(change => change.Key == entry.Key);
                BalanceSnapshot target = current[entry.Key];
                if (target.Value != entry.Value)
                    Changes.Add(new BalanceChange(entry.Key, entry.Schema, target.Value, entry.Value));
            }
        }

        public void Rebase(BalanceSnapshot current)
        {
            BalanceChange change = Changes.Single(entry => entry.Key == current.Key);
            Remember();
            change.Before = current.Value;
            change.Schema = current.Schema;
            if (change.After == change.Before) Changes.Remove(change);
        }

        public void Remove(string key)
        {
            Remember();
            Changes.RemoveAll(change => change.Key == key);
        }

        public void Discard()
        {
            Remember();
            Changes.Clear();
        }

        public void Undo() => Transfer(UndoStates, RedoStates);
        public void Redo() => Transfer(RedoStates, UndoStates);

        private void Remember()
        {
            UndoStates.Add(Capture());
            RedoStates.Clear();
        }

        private BalanceDraftState Capture() => new BalanceDraftState
        {
            Changes = Changes.Select(change => new BalanceChange(change.Key, change.Schema, change.Before, change.After)).ToList()
        };

        private void Transfer(List<BalanceDraftState> source, List<BalanceDraftState> destination)
        {
            if (source.Count == 0) return;
            destination.Add(Capture());
            Changes = source[source.Count - 1].Changes;
            source.RemoveAt(source.Count - 1);
        }
    }
}
