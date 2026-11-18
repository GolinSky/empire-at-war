using System;
using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyPendingBuild
    {
        public UnitRequest Request { get; }
        public float TimeLeft { get; private set; }
        internal float DeploymentWait { get; private set; }
        internal float DeploymentRetryIn { get; set; }
        internal bool IsReadyForDeployment => TimeLeft == 0f && DeploymentRetryIn == 0f;

        public EnemyPendingBuild(UnitRequest request, float timeLeft)
        {
            Request = request;
            TimeLeft = timeLeft;
        }

        internal void Advance(float deltaTime) => TimeLeft = Math.Max(0f, TimeLeft - deltaTime);

        internal void WaitForDeployment(float deltaTime) => DeploymentWait += deltaTime;
    }
}
