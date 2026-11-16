using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Vision
{
    public readonly struct VisionReveal
    {
        public VisionReveal(PlayerId viewer, long entityId)
        {
            Viewer = viewer;
            EntityId = entityId;
        }

        public PlayerId Viewer { get; }
        public long EntityId { get; }
    }
}
