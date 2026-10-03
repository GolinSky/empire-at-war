using System;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons
{
    public interface ISquadron
    {
        event Action Released;

        Vector3 WorldPosition { get; }

        void Guard(IEntity friendly, Vector3 offset);

        void AttackMoveTo(Vector3 worldPosition);

        void Hunt();
    }
}
