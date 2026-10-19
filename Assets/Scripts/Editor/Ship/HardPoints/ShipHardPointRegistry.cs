using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>
    /// Adds and removes ship-view hardpoints while keeping every list that references them in sync:
    /// HealthComponent.ShipUnits, WeaponComponent.hardPoints, EntityBindings.Injectables and sequential IDs.
    /// Every change is one undo step.
    /// </summary>
    public sealed class ShipHardPointRegistry
    {
        private const string SHIP_UNITS_PROPERTY = "<ShipUnits>k__BackingField";
        private const string INJECTABLES_PROPERTY = "<Injectables>k__BackingField";
        private const string WEAPON_HARD_POINTS_PROPERTY = "hardPoints";
        private const string ID_PROPERTY = "<Id>k__BackingField";
        private const string HARD_POINT_TYPE_PROPERTY = "<HardPointType>k__BackingField";
        private const string WEAPON_TYPE_PROPERTY = "<WeaponType>k__BackingField";
        private const string ARC_MIN_PROPERTY = "yAxisRange.<Min>k__BackingField";
        private const string ARC_MAX_PROPERTY = "yAxisRange.<Max>k__BackingField";
        private const string HANGAR_HARD_POINT_PROPERTY = "hangarHardPoint";
        private const string HANGAR_BAYS_PROPERTY = "bayHardPoints";

        private readonly EntityBindings _bindings;
        private readonly HealthComponent _health;

        public ShipHardPointRegistry(EntityBindings bindings)
        {
            _bindings = bindings;
            _health = bindings.Health.Single();
        }

        public Transform Root => _bindings.transform;
        public IReadOnlyList<HardPoint> HardPoints => _health.ShipUnits;
        public IReadOnlyList<WeaponComponent> WeaponComponents => _bindings.Weapons;

        public HardPoint AddClone(HardPoint template, string name, Vector3 position)
        {
            int group = BeginUndo("Add Hardpoint");
            HardPoint hardPoint = Object.Instantiate(template, template.transform.parent);
            hardPoint.name = name;
            hardPoint.transform.position = position;
            Undo.RegisterCreatedObjectUndo(hardPoint.gameObject, "Add Hardpoint");
            Register(hardPoint, template is WeaponHardPoint weapon ? FindOwner(weapon) : null);
            Undo.CollapseUndoOperations(group);
            return hardPoint;
        }

        public HardPoint AddNew(HardPointType type, WeaponType? weaponType, Transform parent, string name,
            Vector3 position)
        {
            int group = BeginUndo("Add Hardpoint");
            GameObject gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Add Hardpoint");
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            HardPoint hardPoint = weaponType.HasValue
                ? gameObject.AddComponent<WeaponHardPoint>()
                : gameObject.AddComponent<HardPoint>();
            SerializedObject serialized = new SerializedObject(hardPoint);
            serialized.FindProperty(HARD_POINT_TYPE_PROPERTY).intValue = (int)type;
            if (weaponType.HasValue)
            {
                serialized.FindProperty(WEAPON_TYPE_PROPERTY).intValue = (int)weaponType.Value;
                serialized.FindProperty(ARC_MIN_PROPERTY).floatValue = -180f;
                serialized.FindProperty(ARC_MAX_PROPERTY).floatValue = 180f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Register(hardPoint, weaponType.HasValue ? _bindings.Weapons.First() : null);
            Undo.CollapseUndoOperations(group);
            return hardPoint;
        }

        /// <summary>Clones the hardpoint onto the other side of the ship and flips its firing arc.</summary>
        public HardPoint AddMirror(HardPoint source, string name)
        {
            Vector3 local = Root.InverseTransformPoint(source.transform.position);
            HardPoint mirror = AddClone(source, name, Root.TransformPoint(new Vector3(-local.x, local.y, local.z)));
            if (mirror is WeaponHardPoint weapon)
            {
                SerializedObject serialized = new SerializedObject(mirror);
                serialized.FindProperty(ARC_MIN_PROPERTY).floatValue = -weapon.MaxYaw;
                serialized.FindProperty(ARC_MAX_PROPERTY).floatValue = -weapon.MinYaw;
                serialized.ApplyModifiedProperties();
            }
            return mirror;
        }

        /// <summary>Adds an existing hierarchy hardpoint to every list it belongs in, then renumbers IDs.</summary>
        public void Register(HardPoint hardPoint) =>
            Register(hardPoint, hardPoint is WeaponHardPoint weapon ? FindOwner(weapon) : null);

        public void Remove(HardPoint hardPoint)
        {
            if (IsHangarReference(hardPoint))
                throw new InvalidOperationException(
                    $"{hardPoint.name} is referenced by the HangarComponent; reassign the hangar hardpoint first.");

            int group = BeginUndo("Remove Hardpoint");
            RemoveFrom(_health, SHIP_UNITS_PROPERTY, hardPoint);
            RemoveFrom(_bindings, INJECTABLES_PROPERTY, hardPoint);
            foreach (WeaponComponent weapons in _bindings.Weapons)
                RemoveFrom(weapons, WEAPON_HARD_POINTS_PROPERTY, hardPoint);
            Undo.DestroyObjectImmediate(hardPoint.gameObject);
            Renumber();
            Undo.CollapseUndoOperations(group);
        }

        /// <summary>Sets each hardpoint's ID to its index in HealthComponent.ShipUnits.</summary>
        public void Renumber()
        {
            for (int index = 0; index < _health.ShipUnits.Count; index++)
            {
                SerializedObject serialized = new SerializedObject(_health.ShipUnits[index]);
                serialized.FindProperty(ID_PROPERTY).intValue = index;
                serialized.ApplyModifiedProperties();
            }
        }

        public bool IsHangarReference(HardPoint hardPoint)
        {
            HangarComponent hangar = _bindings.Hangar;
            if (hangar == null) return false;
            SerializedObject serialized = new SerializedObject(hangar);
            return serialized.FindProperty(HANGAR_HARD_POINT_PROPERTY).objectReferenceValue == hardPoint
                || Contains(serialized.FindProperty(HANGAR_BAYS_PROPERTY), hardPoint);
        }

        public WeaponComponent FindOwner(WeaponHardPoint hardPoint)
        {
            foreach (WeaponComponent weapons in _bindings.Weapons)
            {
                if (Contains(new SerializedObject(weapons).FindProperty(WEAPON_HARD_POINTS_PROPERTY), hardPoint))
                    return weapons;
            }
            return _bindings.Weapons.First();
        }

        public static bool Contains(SerializedProperty list, Object value)
        {
            for (int index = 0; index < list.arraySize; index++)
            {
                if (list.GetArrayElementAtIndex(index).objectReferenceValue == value) return true;
            }
            return false;
        }

        private void Register(HardPoint hardPoint, WeaponComponent weapons)
        {
            AddTo(_health, SHIP_UNITS_PROPERTY, hardPoint);
            AddTo(_bindings, INJECTABLES_PROPERTY, hardPoint);
            if (weapons != null) AddTo(weapons, WEAPON_HARD_POINTS_PROPERTY, hardPoint);
            Renumber();
        }

        private static void AddTo(Object owner, string propertyPath, Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty list = serialized.FindProperty(propertyPath);
            if (Contains(list, value)) return;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void RemoveFrom(Object owner, string propertyPath, Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty list = serialized.FindProperty(propertyPath);
            for (int index = list.arraySize - 1; index >= 0; index--)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue != value) continue;
                element.objectReferenceValue = null;
                list.DeleteArrayElementAtIndex(index);
            }
            serialized.ApplyModifiedProperties();
        }

        private static int BeginUndo(string name)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            return Undo.GetCurrentGroup();
        }
    }
}
