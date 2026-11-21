using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>When the wing commander re-sends an order and which order it sends back to a hold.</summary>
    public static class EnemyWingOrderRules
    {
        /// <summary>
        /// A wing fighting within its idle leash of its point keeps the fight: a new order would disengage it, and
        /// its own leash brings it back to the post afterwards.
        /// </summary>
        public static bool KeepsFight(bool isEngaging, Vector3 wingPosition, Vector3 point, float idleLeash)
        {
            if (!isEngaging) return false;
            float x = wingPosition.x - point.x;
            float z = wingPosition.z - point.z;
            return x * x + z * z <= idleLeash * idleLeash;
        }

        /// <summary>
        /// A wing that reached an uncontested hold and was pulled out of its entry radius flies back by a move, so it
        /// cannot leave again on an unleashed attack-move. A wing still on its way attack-moves there.
        /// </summary>
        public static bool ReturnsByMove(bool reachedHold, bool insideEntry) => insideEntry || reachedHold;
    }
}
