using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.TeamContacts;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Team vision that never sees anything; ship tests rely on the ship's own radar only.</summary>
    public sealed class NoTeamContacts : ITeamContacts
    {
        public void FillVisibleHostiles(PlayerId viewer, Vector3 center, float radius, List<IEntity> results)
        {
        }
    }
}
