using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>
    /// Hardpoint groups and names: "QuadTurbolaser_HP_TL01" belongs to group "QuadTurbolaser";
    /// "HP_ENGINE_01" belongs to group "HP_ENGINE".
    /// </summary>
    public static class HardPointNaming
    {
        private static readonly char[] NUMBER_SUFFIX = "0123456789_ ()".ToCharArray();

        public static string GetGroup(HardPoint hardPoint)
        {
            string bone = HardPointBoneScanner.GetBoneName(hardPoint);
            if (bone == null) return hardPoint.name.TrimEnd(NUMBER_SUFFIX);
            string prefix = hardPoint.name.Substring(0, hardPoint.name.Length - bone.Length).TrimEnd('_');
            return prefix.Length > 0 ? prefix : HardPointBoneScanner.GetPrefix(bone);
        }

        /// <summary>One hardpoint per group, in ShipUnits order, to clone from.</summary>
        public static List<HardPoint> GetTemplates(IEnumerable<HardPoint> hardPoints) =>
            hardPoints.Where(hardPoint => hardPoint != null).GroupBy(GetGroup).Select(group => group.First()).ToList();

        /// <summary>"QuadTurbolaser" + HP_TL21 → "QuadTurbolaser_HP_TL21"; bone-named groups keep the bone name.</summary>
        public static string ForBone(HardPoint template, Transform bone)
        {
            if (template == null) return bone.name;
            string group = GetGroup(template);
            return HardPointBoneScanner.GetBoneName(template) == template.name ? bone.name : $"{group}_{bone.name}";
        }

        /// <summary>First free "&lt;group&gt;_NN" name.</summary>
        public static string GetNextName(string group, IEnumerable<HardPoint> hardPoints)
        {
            HashSet<string> names = new HashSet<string>(hardPoints.Where(hardPoint => hardPoint != null)
                .Select(hardPoint => hardPoint.name));
            int number = 1;
            while (names.Contains($"{group}_{number:00}")) number++;
            return $"{group}_{number:00}";
        }
    }
}
