using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceSharedMountAdapter
    {
        public static void Register(BalanceRegistration registry)
        {
            BalanceField[] mounts = registry.Fields.Values.Where(field => field.Owner == "Prefab Override").ToArray();
            foreach (BalanceField mount in mounts)
            {
                Object source = PrefabUtility.GetCorrespondingObjectFromSource(mount.Target);
                if (source == null || AssetDatabase.GetAssetPath(source) == mount.AssetPath) continue;
                using (SerializedObject serialized = new SerializedObject(mount.Target))
                {
                    // Only inheriting consumers are affected by this source field. Explicit overrides stay local.
                    mount.InheritsSource = !serialized.FindProperty(mount.Path).prefabOverride;
                    var users = mount.InheritsSource ? mount.Users : Enumerable.Empty<BalanceUnit>();
                    using (SerializedObject sourceData = new SerializedObject(source))
                        // Generic mount templates can have an unassigned enum sentinel overridden by every unit.
                        // It has no runtime consumers and is not a canonical balance assignment.
                        if (!users.Any() && mount.Kind == BalanceValueKind.Enum && !System.Enum.IsDefined(mount.EnumType, sourceData.FindProperty(mount.Path).intValue)) continue;
                    BalanceField shared = registry.Add(source, mount.Stat, mount.Path, "Hardpoints",
                        "Shared mount source / " + source.name, "Shared Profile", true, users, mount.EnumType,
                        mount.Minimum, mount.Maximum, mount.Positive, dependency: "source:" + GlobalObjectId.GetGlobalObjectIdSlow(source));
                    shared.SharedMountSource = true;
                    mount.SourceKey = shared.Key;
                }
            }
            foreach (BalanceField field in registry.Fields.Values.Where(field => field.SharedMountSource))
                foreach (BalanceUnit unit in field.Users) unit.Fields["sharedMount/" + field.Key] = field.Key;
        }
    }
}
