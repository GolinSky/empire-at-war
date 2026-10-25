using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    /// <summary>
    /// Derives weapon profiles as they would be after applying a draft: the draft's weapon-rule edits are written
    /// into an in-memory copy of WeaponsData, never into the asset.
    /// </summary>
    public sealed class BalanceWeaponPreview : IDisposable
    {
        private readonly WeaponsData _source;
        private readonly BalanceField[] _fields;
        private WeaponsData _copy;
        private string _appliedSignature;

        public BalanceWeaponPreview(BalanceRegistration registry)
        {
            _source = BalanceRegistration.One<WeaponsData>();
            _fields = registry.FieldsFor(_source).ToArray();
        }

        public bool TryGetProfile(WeaponType weaponType, BalanceDraft draft, out WeaponProfile profile)
        {
            WeaponsData data = Apply(draft);
            if (data.Weapons.All(definition => definition.WeaponType != weaponType))
            {
                profile = null;
                return false;
            }

            profile = data.GetProfile(weaponType);
            return true;
        }

        public IReadOnlyDictionary<WeaponType, WeaponProfile> CaptureProfiles(BalanceDraft draft)
        {
            WeaponsData data = Apply(draft);
            return data.Weapons.ToDictionary(definition => definition.WeaponType,
                definition => data.GetProfile(definition.WeaponType));
        }

        public void Dispose()
        {
            if (_copy != null) Object.DestroyImmediate(_copy);
            _copy = null;
        }

        private WeaponsData Apply(BalanceDraft draft)
        {
            // Only staged fields can differ from the asset; reading all of WeaponsData per lookup made every redraw slow.
            HashSet<string> staged = draft.Changes.Select(change => change.Key).ToHashSet();
            List<(BalanceField field, string value)> edits = _fields
                .Where(field => staged.Contains(field.Key) || field.InheritsSource && staged.Contains(field.SourceKey))
                .Select(field => (field, value: field.DraftValue(draft)))
                .Where(edit => edit.value != edit.field.Read()).ToList();
            string signature = string.Join("|", edits.Select(edit => edit.field.Key + "=" + edit.value));
            if (_copy != null && signature == _appliedSignature) return _copy;

            Dispose();
            _copy = Object.Instantiate(_source);
            using (SerializedObject serialized = new SerializedObject(_copy))
            {
                foreach ((BalanceField field, string value) in edits)
                    BalanceValue.Write(field.Property(serialized), field.Kind, value);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            _appliedSignature = signature;
            return _copy;
        }
    }
}
