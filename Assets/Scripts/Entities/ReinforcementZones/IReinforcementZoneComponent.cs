using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.Capture;
using UnityEngine;

namespace EmpireAtWar.Views.ReinforcementZones
{
    public interface IReinforcementZoneComponent
    {
        Vector3 Center { get; }
        float Radius { get; }
        PlayerId StartingOwner { get; }
        bool IsCapturable { get; }
        float CaptureDuration { get; }

        void SetVisibility(bool isVisible, bool showCaptureUi);

        void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested,
            OwnerRelation contestingFirst = OwnerRelation.Neutral,
            OwnerRelation contestingSecond = OwnerRelation.Neutral);
    }
}
