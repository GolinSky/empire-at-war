using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public interface IBattleMarkerLayer
    {
        IReadOnlyList<IBattleMarker> Markers { get; }
        BattleMarker CreateShipMarker(Sprite icon, Transform anchor);
        void Attach(RectTransform root);
        void Register(IBattleMarker marker);
        void Unregister(IBattleMarker marker);
        void InvalidateMembership(long entityId);
    }
}
