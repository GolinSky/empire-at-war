using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    public interface ISquadronRegistry : IService
    {
        /// <summary>Adds every living squadron inside the area to the capture tally with the given weight.</summary>
        void AddSquadronStrength(Func<Vector3, bool> contains, float weight, CaptureStrengthBuilder tally);

        /// <summary>True when a living squadron whose owner matches the filter is inside the area.</summary>
        bool HasSquadronInside(Func<Vector3, bool> contains, Predicate<PlayerId> isOwnerIncluded);
    }
}
