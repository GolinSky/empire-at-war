using System;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons
{
    public interface ISquadron : IEntityFacade
    {
        event Action Released;

        long EntityId { get; }

        Vector3 WorldPosition { get; }

        /// <summary>The wing is fighting a living, visible target.</summary>
        bool IsEngaging { get; }

        /// <summary>An idle wing drops a target chased farther than this from its post.</summary>
        float IdleLeashRadius { get; }

        void Guard(IEntity friendly, Vector3 offset);

        void AttackMoveTo(Vector3 worldPosition);

        void Hunt();
    }
}
