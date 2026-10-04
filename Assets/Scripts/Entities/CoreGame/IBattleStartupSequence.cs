using System.Threading;
using UnityEngine;

namespace EmpireAtWar.Controllers.Game
{
    public interface IBattleStartupSequence
    {
        /// <summary>Loads the map and prepares the battle; completes when the battle can run.</summary>
        Awaitable RunAsync(CancellationToken cancellationToken);
    }
}
