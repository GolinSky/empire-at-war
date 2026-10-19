using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>
    /// Finds the ALO hardpoint bones (HP_TL01, HP_ENGINE_02, ...) kept as transforms in converted ship models
    /// and matches them to view hardpoints by name suffix or position.
    /// Helper bones such as HP_TL03_Blast or HP_IC01_EmitDamage are ignored.
    /// </summary>
    public static class HardPointBoneScanner
    {
        private const float MATCH_DISTANCE = 0.01f;
        private static readonly Regex BONE_NAME = new Regex(@"^(HP_[A-Z]+(?:_[A-Z]+)*?)_?(\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        // Damage-effect helpers such as HP_IC_BL_Blast01 also end in a number.
        private static readonly Regex DAMAGE_HELPER = new Regex(@"BLAST|EMIT|DAMAGE|FIRE|SMOKE",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex BONE_SUFFIX = new Regex(@"(HP_[A-Z]+(?:_[A-Z]+)*?_?\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly (string Token, HardPointType Type)[] TYPE_TOKENS =
        {
            ("ENGINE", HardPointType.Engines),
            ("SHIELD", HardPointType.ShieldGenerator),
            ("SPAWN", HardPointType.Hangar),
            ("HANGAR", HardPointType.Hangar),
            ("TRACTOR", HardPointType.TractorBeam),
            ("SENSOR", HardPointType.SensorArray),
            ("COMMAND", HardPointType.CommandArray),
            ("BRIDGE", HardPointType.CommandArray),
        };

        public static bool IsHardPointBone(string name) => BONE_NAME.IsMatch(name) && !DAMAGE_HELPER.IsMatch(name);

        /// <summary>"HP_TL07" → "HP_TL"; "HP_ENGINE_02" → "HP_ENGINE".</summary>
        public static string GetPrefix(string boneName) => BONE_NAME.Match(boneName).Groups[1].Value.ToUpperInvariant();

        public static int GetNumber(string boneName) => int.Parse(BONE_NAME.Match(boneName).Groups[2].Value);

        public static HardPointType GuessType(string boneName)
        {
            string prefix = GetPrefix(boneName);
            foreach ((string token, HardPointType type) in TYPE_TOKENS)
            {
                if (prefix.Contains(token)) return type;
            }
            return HardPointType.Weapon;
        }

        /// <summary>Bone name a hardpoint was placed on: "QuadTurbolaser_HP_TL01" → "HP_TL01"; null when none.</summary>
        public static string GetBoneName(HardPoint hardPoint)
        {
            Match match = BONE_SUFFIX.Match(hardPoint.name);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>Hardpoint bones under the root that are not hardpoints themselves.</summary>
        public static List<Transform> FindBones(Transform root)
        {
            List<Transform> bones = new List<Transform>();
            // Editor-only scan of model bones, which carry no component to bind explicitly.
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (IsHardPointBone(transform.name) && !transform.TryGetComponent(out HardPoint _))
                    bones.Add(transform);
            }
            return bones;
        }

        public static List<Transform> FindFreeBones(Transform root, IReadOnlyList<HardPoint> hardPoints) =>
            FindBones(root).Where(bone => hardPoints.All(hardPoint => !Matches(hardPoint, bone))).ToList();

        public static Transform FindBone(HardPoint hardPoint, IEnumerable<Transform> bones)
        {
            string boneName = GetBoneName(hardPoint);
            return boneName == null
                ? null
                : bones.FirstOrDefault(bone => string.Equals(bone.name, boneName, StringComparison.OrdinalIgnoreCase));
        }

        public static bool Matches(HardPoint hardPoint, Transform bone) =>
            string.Equals(GetBoneName(hardPoint), bone.name, StringComparison.OrdinalIgnoreCase)
            || Vector3.Distance(hardPoint.transform.position, bone.position) < MATCH_DISTANCE;

        /// <summary>
        /// Hardpoint to clone for a bone: same bone prefix and nearest bone number, so HP_TL21 copies the
        /// turbolaser placed on HP_TL20. Null when no hardpoint uses that prefix.
        /// </summary>
        public static HardPoint FindTemplate(string boneName, IEnumerable<HardPoint> hardPoints)
        {
            string prefix = GetPrefix(boneName);
            int number = GetNumber(boneName);
            return hardPoints
                .Select(hardPoint => (HardPoint: hardPoint, Bone: GetBoneName(hardPoint)))
                .Where(entry => entry.Bone != null && GetPrefix(entry.Bone) == prefix)
                .OrderBy(entry => Math.Abs(GetNumber(entry.Bone) - number))
                .Select(entry => entry.HardPoint)
                .FirstOrDefault();
        }
    }
}
