namespace EmpireAtWar.Services.TacticalMarkers
{
    public static class AllyAiMarkerDecision
    {
        public static TacticalMarkerReplyStatus Evaluate(TacticalMarkerType type, bool hasForce,
            float advantage, bool homeThreatened, bool distant, bool hasLocalShips)
        {
            if (type == TacticalMarkerType.Look) return TacticalMarkerReplyStatus.Accepted;
            if (type == TacticalMarkerType.Retreat)
                return hasLocalShips && advantage < 1f
                    ? TacticalMarkerReplyStatus.Accepted : TacticalMarkerReplyStatus.Declined;
            if (!hasForce) return TacticalMarkerReplyStatus.Delayed;
            if (advantage < 1f) return TacticalMarkerReplyStatus.Declined;
            if (homeThreatened || distant) return TacticalMarkerReplyStatus.Delayed;
            return TacticalMarkerReplyStatus.Accepted;
        }
    }
}
