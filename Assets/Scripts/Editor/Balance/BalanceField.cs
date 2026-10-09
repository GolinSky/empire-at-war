using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public sealed class BalanceField
    {
        public string Key;
        public string Stat;
        public string Label;
        public BalanceFieldGroup Group;
        public BalanceFieldOwner Owner;
        public string Context;
        public string AliasWarning = "";
        public string Dependency = "";
        public Object Target;
        public string Path;
        public BalanceValueKind Kind;
        public Type EnumType;
        public double Minimum;
        public double Maximum = double.PositiveInfinity;
        public bool Positive;
        public bool Shared;
        public bool SharedMountSource;
        public bool InheritsSource;
        public string SourceKey = "";
        public readonly List<BalanceUnit> Users = new List<BalanceUnit>();
        public string AssetPath => AssetDatabase.GetAssetPath(Target);
        public string PresetSchema => $"{Kind}|{EnumType}|{AliasWarning}|{Dependency}|{SourceKey}";
        public string Schema => $"{PresetSchema}|{InheritsSource}|{string.Join(";", Users.Select(unit => unit.Id).Distinct().OrderBy(id => id))}";
        public string ScopeLabel => DescribeScope(Users);
        public string GroupLabel => ObjectNames.NicifyVariableName(Group.ToString());
        public string OwnerLabel => ObjectNames.NicifyVariableName(Owner.ToString());

        public string DescribeScope(IEnumerable<BalanceUnit> users)
        {
            if (!Shared) return OwnerLabel;
            BalanceUnit[] consumers = users.GroupBy(unit => unit.Id).Select(group => group.First()).ToArray();
            string[] factions = consumers.Select(unit => unit.Faction).Distinct().OrderBy(name => name).ToArray();
            return factions.Length == 0 ? "Global profile — no current users"
                : $"{(factions.Length > 1 ? "Shared across factions" : "Shared profile")} — {string.Join(", ", factions)} · {consumers.Length} units";
        }

        public SerializedProperty Property(SerializedObject serialized)
        {
            SerializedProperty property = serialized.FindProperty(Path);
            if (property == null) throw new InvalidOperationException($"Missing approved field {AssetPath}: {Path}");
            return property;
        }

        public BalanceSnapshot Snapshot() => new BalanceSnapshot(Key, Schema, Read());

        public string DraftValue(BalanceDraft draft)
        {
            BalanceChange local = draft.Changes.FirstOrDefault(change => change.Key == Key);
            BalanceChange source = InheritsSource ? draft.Changes.FirstOrDefault(change => change.Key == SourceKey) : null;
            return local != null ? local.After : source != null ? source.After : Read();
        }

        public string Read()
        {
            using (SerializedObject serialized = new SerializedObject(Target)) return BalanceValue.Read(Property(serialized), Kind);
        }

        public string Validate(string value)
        {
            if (Kind == BalanceValueKind.Boolean) return value == "true" || value == "false" ? "" : "Expected true/false.";
            if (Kind == BalanceValueKind.Enum || Kind == BalanceValueKind.EnumSet)
            {
                string[] tokens = Kind == BalanceValueKind.EnumSet && value.Length == 0 ? Array.Empty<string>() : value.Split(',');
                if (tokens.Distinct().Count() != tokens.Length) return "Duplicate canonical enum IDs.";
                foreach (string token in tokens)
                    if (!int.TryParse(token, out int id) || !Enum.IsDefined(EnumType, id)) return "Unknown canonical enum ID.";
                return "";
            }
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                || double.IsNaN(number) || double.IsInfinity(number)) return "Value must be finite.";
            if (Kind == BalanceValueKind.Float && (number > float.MaxValue || number < -float.MaxValue)) return "Float overflow.";
            if (Kind == BalanceValueKind.Integer && (number != Math.Round(number) || number > int.MaxValue || number < int.MinValue))
                return "Expected a 32-bit integer; bulk operations round away from zero.";
            return number < Minimum || number > Maximum || (Positive && number <= 0) ? $"Required range: {(Positive ? "> 0" : Minimum.ToString(CultureInfo.InvariantCulture))} … {Maximum}." : "";
        }
    }
}
