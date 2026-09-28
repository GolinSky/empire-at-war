using EmpireAtWar.Models.Factions;
using UnityEngine;
using EmpireAtWar.Models.Players;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IViewEntity
    {
        long Id { get; }
        PlayerId Owner { get; }
    }

    /// <summary>
    /// will be added dynamically via installer to every entity unit GO
    /// </summary>
    public class ViewEntity : MonoBehaviour, IViewEntity
    {
        [Inject]
        public long Id { get; }
        
        [Inject]
        public PlayerId Owner { get;  }
    }
}