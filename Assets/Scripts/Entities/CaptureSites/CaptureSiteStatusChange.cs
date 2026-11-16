using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public readonly struct CaptureSiteStatusChange
    {
        public ICaptureSite Site { get; }
        public Vector3 Position { get; }
        public PlayerId PreviousOwner { get; }
        public PlayerId Owner { get; }
        public CaptureSiteStatusTransition Transition { get; }

        public CaptureSiteStatusChange(ICaptureSite site, Vector3 position, PlayerId previousOwner,
            PlayerId owner, CaptureSiteStatusTransition transition)
        {
            Site = site;
            Position = position;
            PreviousOwner = previousOwner;
            Owner = owner;
            Transition = transition;
        }
    }
}
