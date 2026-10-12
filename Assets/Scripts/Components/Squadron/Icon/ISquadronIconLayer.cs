using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>One screen canvas that draws every squadron marker, so moving markers rebuilds one batch per frame.</summary>
    public interface ISquadronIconLayer
    {
        /// <summary>Moves <paramref name="icon"/> under the layer; its anchored position is then in screen pixels.</summary>
        void Attach(RectTransform icon);
    }
}
