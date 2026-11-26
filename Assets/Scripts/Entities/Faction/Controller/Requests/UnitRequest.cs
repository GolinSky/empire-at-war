using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public abstract class UnitRequest
    {
        public FactionData FactionData { get; }
        public abstract string Id { get;  }

        protected UnitRequest(FactionData factionData)
        {
            FactionData = factionData;
        }
    }

    public abstract class UnitRequest<TKey>:UnitRequest
    {
        public TKey Key { get; }
        // Cached: the faction UI compares Ids for every card every frame, and ToString allocated each time.
        public sealed override string Id { get; }

        protected UnitRequest(FactionData factionData, TKey key):base(factionData)
        {
            Key = key;
            Id = key.ToString();
        }
    }
}