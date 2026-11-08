using System.Threading;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Last startup step before the loading cover fades: online battles wait for every PC here.</summary>
    public interface IBattleStartGate
    {
        Awaitable WaitAsync(CancellationToken cancellationToken);
    }
}
