using System.Reflection;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Squadrons;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronOrderTimingTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float INTERVAL = 0.15f;
        private static readonly MethodInfo TICK_ORDER = typeof(Squadron).GetMethod("TickOrder", PRIVATE_INSTANCE);
        private static readonly FieldInfo ORDER_TIMER = typeof(Squadron).GetField("_orderTimer", PRIVATE_INSTANCE);
        private static readonly FieldInfo ORDER_ELAPSED = typeof(Squadron).GetField("_orderElapsed", PRIVATE_INSTANCE);
        private static readonly FieldInfo SEARCH_TIMER = typeof(Squadron).GetField("_targetSearchTimer", PRIVATE_INSTANCE);

        [TestCase(0.16f)]
        [TestCase(0.6f)]
        [TestCase(2.01f)]
        public void Hitch_PreservesPhasesAndDoesNotReplayMissedOrderChecks(float hitch)
        {
            GameObject root = new GameObject("SquadronOrderTiming");
            try
            {
                Squadron[] squadrons = new Squadron[129];
                for (int i = 0; i < squadrons.Length; i++)
                {
                    Squadron squadron = root.AddComponent<Squadron>();
                    squadrons[i] = squadron;
                    typeof(Squadron).GetField("_orders", PRIVATE_INSTANCE).SetValue(squadron, new UnitOrderModel());
                    ORDER_TIMER.SetValue(squadron, (i + 0.5f) * INTERVAL / squadrons.Length);
                    SEARCH_TIMER.SetValue(squadron, 100f);
                    TICK_ORDER.Invoke(squadron, new object[] { hitch });
                    Assert.That((float)ORDER_TIMER.GetValue(squadron), Is.InRange(0f, INTERVAL));
                    Assert.That((float)SEARCH_TIMER.GetValue(squadron), Is.EqualTo(100f - hitch).Within(0.00001f));
                }

                for (int frame = 0; frame < 10; frame++)
                {
                    int updates = 0;
                    foreach (Squadron squadron in squadrons)
                    {
                        TICK_ORDER.Invoke(squadron, new object[] { 0.03f });
                        if ((float)ORDER_ELAPSED.GetValue(squadron) == 0f) updates++;
                    }
                    Assert.That(updates, Is.InRange(25, 27), "Order work must stay spread after the hitch.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
