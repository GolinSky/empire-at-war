using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.TeamContacts
{
    /// <summary>Hostile units the viewer's team currently sees, beyond any single ship's radar.</summary>
    public interface ITeamContacts
    {
        /// <summary>Appends the visible hostiles within <paramref name="radius"/> of the point.</summary>
        void FillVisibleHostiles(PlayerId viewer, Vector3 center, float radius, List<IEntity> results);
    }
}
