using EmpireAtWar.Models.Players;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Entities.BaseEntity
{
    public class ViewEntity : MonoBehaviour, IViewEntity
    {
        public long Id { get; private set; }
        public PlayerId Owner { get; private set; }

        [Inject]
        private void Construct(long id, PlayerId owner)
        {
            Id = id;
            Owner = owner;
        }
    }
}
