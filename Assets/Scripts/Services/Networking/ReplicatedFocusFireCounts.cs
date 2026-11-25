using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.UnitOrders;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Online client: the local team's holder counts as the host last sent them.</summary>
    public sealed class ReplicatedFocusFireCounts : IFocusFireCounts
    {
        private readonly Dictionary<long, (int Ships, int Squadrons)> _counts =
            new Dictionary<long, (int, int)>();

        public int CountHolders(IEntity target, bool isSquadron)
        {
            if (!_counts.TryGetValue(target.Id, out (int Ships, int Squadrons) counts)) return 0;
            return isSquadron ? counts.Squadrons : counts.Ships;
        }

        public void Apply(long targetId, int ships, int squadrons)
        {
            if (ships == 0 && squadrons == 0) _counts.Remove(targetId);
            else _counts[targetId] = (ships, squadrons);
        }

        public void Remove(long targetId) => _counts.Remove(targetId);

        public void Clear() => _counts.Clear();
    }
}
