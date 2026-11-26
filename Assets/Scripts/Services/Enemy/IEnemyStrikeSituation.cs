using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Models.Players;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// What a hit-and-run cycle asks about the battle around its group. Answers refer to the last context the
    /// group's builder built.
    /// </summary>
    public interface IEnemyStrikeSituation
    {
        /// <summary>
        /// The hostile big ship the group fights: <paramref name="previous"/> while it lives and is known, else the
        /// nearest visible heavy cruiser, capital, battlecruiser or dreadnought in the local fight. Its target force
        /// joins the context. Null when there is none.
        /// </summary>
        GameEntity FindStrikeTarget(GameEntity previous);

        UnitCombatProfile GetProfile(GameEntity unit);

        /// <summary>
        /// First valid point of: allied repairer, own station (far side from the threat), main fleet body, held zone.
        /// </summary>
        /// <param name="origin">Where the strike ships stand.</param>
        /// <param name="shipRadius">Largest hull radius among the strike ships.</param>
        bool TryFindStrikeFallbackPoint(Vector3 origin, GameEntity target, float shipRadius, out Vector3 point,
            out EnemyStrikeFallbackKind kind);

        /// <summary>Hull points per second the known hostile repairers in range restore on <paramref name="target"/>.</summary>
        float GetHostileRepairPerSecond(GameEntity target);

        /// <summary>Hostile station or platform guns the <paramref name="group"/> cannot beat cover <paramref name="target"/>.</summary>
        bool IsUnderUnbeatableDefenses(GameEntity target, ForceComposition group);

        /// <summary>A working own or allied repairer covers <paramref name="point"/>.</summary>
        bool IsRepairCovered(Vector3 point);

        /// <summary>The weapons of <paramref name="target"/> reach <paramref name="point"/> from where it stands.</summary>
        bool CanTargetReach(GameEntity target, Vector3 point);

        /// <summary>The player finished the Field Repairs research.</summary>
        bool HasFieldRepairs(PlayerId player);
    }
}
