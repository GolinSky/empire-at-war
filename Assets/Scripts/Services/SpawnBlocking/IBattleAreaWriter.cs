using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.SpawnBlocking
{
    public interface IBattleAreaWriter
    {
        void Register(BattleAreaLayer layer, PlayerId owner, Transform source, float radius);

        void Unregister(BattleAreaLayer layer, Transform source);
    }
}
