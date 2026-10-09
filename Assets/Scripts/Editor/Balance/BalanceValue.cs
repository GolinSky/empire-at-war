using System;
using System.Globalization;
using System.Linq;
using UnityEditor;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceValue
    {
        public static string Read(SerializedProperty property, BalanceValueKind kind)
        {
            switch (kind)
            {
                case BalanceValueKind.Float: return property.floatValue.ToString("R", CultureInfo.InvariantCulture);
                case BalanceValueKind.Boolean: return property.boolValue ? "true" : "false";
                case BalanceValueKind.EnumSet:
                    return string.Join(",", Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i).intValue));
                default: return property.intValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static void Write(SerializedProperty property, BalanceValueKind kind, string value)
        {
            switch (kind)
            {
                case BalanceValueKind.Float:
                    property.floatValue = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case BalanceValueKind.Boolean:
                    property.boolValue = bool.Parse(value);
                    break;
                case BalanceValueKind.EnumSet:
                    int[] ids = value.Length == 0 ? Array.Empty<int>() : value.Split(',').Select(int.Parse).ToArray();
                    property.arraySize = ids.Length;
                    for (int i = 0; i < ids.Length; i++) property.GetArrayElementAtIndex(i).intValue = ids[i];
                    break;
                default:
                    property.intValue = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        public static string Bulk(BalanceField field, string current, string operation, double operand)
        {
            if (field.Kind == BalanceValueKind.Enum && operation == "Set")
            {
                string selected = operand.ToString(CultureInfo.InvariantCulture);
                string enumError = field.Validate(selected);
                if (enumError.Length != 0) throw new InvalidOperationException(enumError);
                return selected;
            }
            if (field.Kind != BalanceValueKind.Float && field.Kind != BalanceValueKind.Integer)
                throw new InvalidOperationException("Bulk operations require compatible numeric fields.");
            double number = double.Parse(current, CultureInfo.InvariantCulture);
            double result = operation == "Set" ? operand : operation == "Add" ? number + operand
                : operation == "Multiply" ? number * operand : throw new InvalidOperationException("Unknown bulk operation.");
            if (field.Kind == BalanceValueKind.Integer) result = Math.Round(result, MidpointRounding.AwayFromZero);
            string value = field.Kind == BalanceValueKind.Float ? ((float)result).ToString("R", CultureInfo.InvariantCulture) : result.ToString(CultureInfo.InvariantCulture);
            string error = field.Validate(value);
            if (error.Length != 0) throw new InvalidOperationException($"{field.Label}: {error}");
            return value;
        }
    }
}
