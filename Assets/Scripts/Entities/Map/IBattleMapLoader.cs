using System.Threading;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public interface IBattleMapLoader
    {
        /// <summary>Generates and builds the battlefield, then publishes it as <see cref="BattleMap"/>.</summary>
        Awaitable LoadAsync(CancellationToken cancellationToken);
    }
}
