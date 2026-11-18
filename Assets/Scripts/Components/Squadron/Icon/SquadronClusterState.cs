using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>Per-cluster frame accumulation and placement memory, reused across frames.</summary>
    internal sealed class SquadronClusterState
    {
        public long Id;
        public int Stamp;
        public int ActiveIndex;
        public int Count;
        public int Present;
        public float Rise;
        public Vector2 Sum;
        public long Hash;
        public bool IsNew;
        public int Candidate;
        public ScreenSpring Placement;
        public Vector2 Centroid;
        public Vector2 Center;
        public Vector2 Size;

        public void BeginFrame(int stamp, int count, float rise)
        {
            Stamp = stamp;
            Count = count;
            Present = 0;
            Rise = rise;
            Sum = Vector2.zero;
            Hash = 0;
        }

        public void Add(BattleMarker marker, Vector2 anchor, int rank, int count)
        {
            Sum += anchor;
            Present++;
            Count = Mathf.Max(Count, Mathf.Max(count, rank + 1));
            Hash = unchecked(Hash + (marker.EntityId + 1) * 2654435761L);
        }

        public long LayoutKey(int rank) => unchecked(Hash + Id * 1000003L + rank);
    }
}
