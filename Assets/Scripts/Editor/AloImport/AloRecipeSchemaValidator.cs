using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloRecipeSchemaValidator
    {
        public static JObject Load(string path)
        {
            var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
            JObject recipe = JObject.Parse(File.ReadAllText(path), settings);
            Validate(recipe);
            return recipe;
        }

        public static void Validate(JObject recipe)
        {
            JObject schema = JObject.Parse(File.ReadAllText("Tools/AloImport/recipe.schema.json"));
            Check(recipe, schema, "$recipe");
            AloRecipeSemanticValidator.Validate(recipe);
        }

        private static void Check(JToken value, JObject schema, string path)
        {
            if (schema["const"] != null && !JToken.DeepEquals(value, schema["const"])) Fail(path, "constant differs");
            if (schema["enum"] is JArray options && !options.Any(option => JToken.DeepEquals(value, option))) Fail(path, "unsupported value");
            if (schema["type"] != null)
            {
                string[] types = schema["type"] is JArray typeList ? typeList.Values<string>().ToArray() : new[] { (string)schema["type"] };
                if (!types.Any(type => IsType(value, type))) Fail(path, "invalid type");
            }
            if (value.Type == JTokenType.Float && (double.IsInfinity((double)value) || double.IsNaN((double)value))) Fail(path, "non-finite number");
            if (value is JObject obj)
            {
                if (schema["required"] is JArray required)
                    foreach (string key in required.Values<string>())
                        if (obj.Property(key) == null) Fail(path, "missing " + key);
                JObject properties = schema["properties"] as JObject;
                foreach (JProperty property in obj.Properties())
                {
                    if (properties != null && properties[property.Name] is JObject child) Check(property.Value, child, path + "." + property.Name);
                    else if (schema["additionalProperties"] != null && schema["additionalProperties"].Type == JTokenType.Boolean && !(bool)schema["additionalProperties"]) Fail(path, "unknown " + property.Name);
                }
            }
            if (value is JArray array)
            {
                if (schema["minItems"] != null && array.Count < (int)schema["minItems"]) Fail(path, "too few items");
                if (schema["maxItems"] != null && array.Count > (int)schema["maxItems"]) Fail(path, "too many items");
                if (schema["items"] is JObject item)
                    for (int i = 0; i < array.Count; i++) Check(array[i], item, path + "[" + i + "]");
            }
            if (value.Type == JTokenType.String)
            {
                string text = (string)value;
                if (schema["minLength"] != null && text.Length < (int)schema["minLength"]) Fail(path, "string too short");
                if (schema["pattern"] != null && !Regex.IsMatch(text, (string)schema["pattern"])) Fail(path, "pattern mismatch");
            }
            if (value.Type == JTokenType.Integer || value.Type == JTokenType.Float)
            {
                if (schema["minimum"] != null && (double)value < (double)schema["minimum"]) Fail(path, "below minimum");
                if (schema["exclusiveMinimum"] != null && (double)value <= (double)schema["exclusiveMinimum"]) Fail(path, "must exceed minimum");
            }
            if (schema["allOf"] is JArray all)
                foreach (JObject child in all) Check(value, child, path);
            if (schema["oneOf"] is JArray one && one.Count(child => Matches(value, (JObject)child)) != 1) Fail(path, "expected exactly one alternative");
            if (schema["anyOf"] is JArray any && !any.Any(child => Matches(value, (JObject)child))) Fail(path, "no alternative matches");
            if (schema["not"] is JObject excluded && Matches(value, excluded)) Fail(path, "forbidden value");
            if (schema["if"] is JObject condition)
            {
                string branch = Matches(value, condition) ? "then" : "else";
                if (schema[branch] is JObject selected) Check(value, selected, path);
            }
        }

        private static bool Matches(JToken value, JObject schema)
        {
            try { Check(value, schema, "$alternative"); return true; }
            catch (JsonException) { return false; }
        }

        private static bool IsType(JToken value, string type) => type switch
        {
            "object" => value.Type == JTokenType.Object,
            "array" => value.Type == JTokenType.Array,
            "string" => value.Type == JTokenType.String,
            "integer" => value.Type == JTokenType.Integer,
            "number" => value.Type == JTokenType.Integer || value.Type == JTokenType.Float,
            "boolean" => value.Type == JTokenType.Boolean,
            "null" => value.Type == JTokenType.Null,
            _ => throw new JsonException("Unsupported schema type " + type)
        };

        private static void Fail(string path, string reason) => throw new JsonException(path + ": " + reason);
    }
}
