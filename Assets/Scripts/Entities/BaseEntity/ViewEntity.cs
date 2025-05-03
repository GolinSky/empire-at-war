using EmpireAtWar.Models.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IViewEntity
    {
        long Id { get; }
        PlayerType PlayerType { get; }
    }

    /// <summary>
    /// will be added dynamically via installer to every entity unit GO
    /// </summary>
    public class ViewEntity : MonoBehaviour, IViewEntity
    {
        [Inject]
        public long Id { get; }
        
        [Inject]
        public PlayerType PlayerType { get;  }
    }
}