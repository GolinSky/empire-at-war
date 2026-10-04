using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>Holds the generated layout; readable once the battle map has loaded.</summary>
    public sealed class MapModel : IMapModelObserver
    {
        private MapLayout _layout;

        public Vector2Range SizeRange => _layout.SizeRange;

        public void SetLayout(MapLayout layout)
        {
            _layout = layout;
        }

        public Vector3 GetStationPosition(PlayerId owner)
        {
            return _layout.GetStationPosition(owner);
        }
    }
}
