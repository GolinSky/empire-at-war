using System.Threading;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    public interface ISquadronPrewarmer
    {
        Awaitable PrewarmAsync(CancellationToken cancellationToken);
    }
}
