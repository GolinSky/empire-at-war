using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Mvc;
using UnityEngine;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Entities.Ship.EntityCommands
{
    public sealed class EnemyAttackShipCommand :
        Command<EmpireAtWar.Ship.Ship>,
        IAttackCommand
    {
        public Vector3 WorldPosition => Controller.WorldPosition;
        public float NavigationRadius => Controller.NavigationRadius;

        public EnemyAttackShipCommand(EmpireAtWar.Ship.Ship ship)
            : base(ship)
        {
        }

        public void Attack(IEntity target, Vector3 formationOffset)
        {
            Controller.AssignAttackTarget(target);
        }
    }
}
