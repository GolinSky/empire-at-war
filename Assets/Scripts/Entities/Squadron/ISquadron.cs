using System;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons
{
    public interface ISquadron
    {
        event Action Released;
        void Guard(IEntity friendly, Vector3 offset);
    }
}
