using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public sealed class BalanceRegistration
    {
        public readonly Dictionary<string, BalanceField> Fields = new Dictionary<string, BalanceField>();
        public readonly List<BalanceUnit> Units = new List<BalanceUnit>();
        public readonly List<string> Errors = new List<string>();
        private readonly Dictionary<Object, string> _targetIdentities = new Dictionary<Object, string>();
        private readonly Dictionary<Object, List<BalanceField>> _targetFields = new Dictionary<Object, List<BalanceField>>();

        public IEnumerable<BalanceField> FieldsFor(Object target) => _targetFields.TryGetValue(target, out List<BalanceField> fields)
            ? fields : Enumerable.Empty<BalanceField>();

        public BalanceField Add(Object target, string key, string path, string group, string context,
            string owner, bool shared, IEnumerable<BalanceUnit> users, Type enumType = null, double minimum = 0,
            double maximum = double.PositiveInfinity, bool positive = false, string alias = "", string dependency = "", SerializedObject serializedTarget = null)
        {
            using (SerializedObject owned = serializedTarget == null ? new SerializedObject(target) : null)
            {
                SerializedObject serialized = serializedTarget ?? owned;
                SerializedProperty property = serialized.FindProperty(path);
                if (property == null) throw new InvalidOperationException($"Approved schema missing: {target.name}/{path}");
                BalanceValueKind kind = property.isArray ? BalanceValueKind.EnumSet : property.propertyType switch
                {
                    SerializedPropertyType.Float => BalanceValueKind.Float,
                    SerializedPropertyType.Integer => BalanceValueKind.Integer,
                    SerializedPropertyType.Boolean => BalanceValueKind.Boolean,
                    SerializedPropertyType.Enum => BalanceValueKind.Enum,
                    _ => throw new InvalidOperationException($"Unapproved value type: {path}")
                };
                if (!_targetIdentities.TryGetValue(target, out string targetIdentity))
                {
                    targetIdentity = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target)) + "/"
                        + (target is UnityEngine.Component ? GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() : "root");
                    _targetIdentities.Add(target, targetIdentity);
                }
                string identity = targetIdentity + "/" + key;
                if (!Fields.TryGetValue(identity, out BalanceField field))
                {
                    field = new BalanceField
                    {
                        Key = identity, Stat = key, Path = path, Target = target, Group = group, Context = context,
                        Owner = owner, Shared = shared, Kind = kind, EnumType = enumType, Minimum = minimum,
                        Maximum = maximum, Positive = positive, Label = ObjectNames.NicifyVariableName(key.Split('/').Last()),
                        AliasWarning = alias, Dependency = dependency
                    };
                    if ((kind == BalanceValueKind.Enum || kind == BalanceValueKind.EnumSet) && enumType == null)
                        throw new InvalidOperationException($"Missing enum schema: {key}");
                    Fields.Add(identity, field);
                    if (!_targetFields.TryGetValue(target, out List<BalanceField> targetFields))
                    {
                        targetFields = new List<BalanceField>();
                        _targetFields.Add(target, targetFields);
                    }
                    targetFields.Add(field);
                }
                foreach (BalanceUnit unit in users)
                    if (field.Users.All(existing => existing.Id != unit.Id)) field.Users.Add(unit);
                if (field.Users.Count > 1) field.Shared = true;
                return field;
            }
        }

        public static string Auto(string name) => $"<{name}>k__BackingField";

        public static IEnumerable<SerializedProperty> Elements(SerializedProperty array)
        {
            for (int i = 0; i < array.arraySize; i++) yield return array.GetArrayElementAtIndex(i);
        }

        public static IEnumerable<SerializedProperty> Keyed(SerializedObject serialized, string path)
        {
            HashSet<int> keys = new HashSet<int>();
            foreach (SerializedProperty entry in Elements(serialized.FindProperty(path)))
            {
                int id = entry.FindPropertyRelative("key").intValue;
                if (!keys.Add(id)) throw new InvalidOperationException($"Duplicate canonical key in {serialized.targetObject.name}/{path}: {id}");
                yield return entry;
            }
        }

        public static T One<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Settings" });
            if (guids.Length != 1) throw new InvalidOperationException($"Expected one {typeof(T).Name}, found {guids.Length}.");
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public void AutoFields(Object target, string path, string key, string names, string group, string context,
            string owner, bool shared, IEnumerable<BalanceUnit> users)
        {
            using (SerializedObject serialized = new SerializedObject(target))
                foreach (string name in names.Split(' '))
                    Add(target, key + name, path + Auto(name), group, context, owner, shared, users,
                        positive: name == "ShieldRegenerateDelay" || name == "Delay" || name == "HangarLaunchInterval", serializedTarget: serialized);
        }
    }
}
