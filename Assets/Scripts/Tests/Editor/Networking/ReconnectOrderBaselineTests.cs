using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class ReconnectOrderBaselineTests
    {
        [TestCase(UnitOrderType.None, UnitOrderType.Move)]
        [TestCase(UnitOrderType.Move, UnitOrderType.Attack)]
        [TestCase(UnitOrderType.Move, UnitOrderType.None)]
        public void Baseline_UsesCurrentOrderBeforeNextReplicationTick(UnitOrderType previous, UnitOrderType current)
        {
            var sender = new HostUnitOrderSender(null, null, null);
            var orders = (Dictionary<long, (IUnitOrderReplicaFacade Orders, UnitOrderType Order, int Revision)>)
                typeof(HostUnitOrderSender).GetField("_sent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(sender);
            orders.Add(42, (new OrderFacade { CurrentOrder = current }, previous, 0));
            var messages = new List<UnitOrderStateMessage>();
            void Capture(NetworkDiagnostics.MessageInfo info)
            {
                if (info.message is UnitOrderStateMessage message) messages.Add(message);
            }

            Transport previousTransport = Transport.active;
            var transportObject = new GameObject("Reconnect baseline transport test");
            var connection = new NetworkConnectionToClient(71);
            NetworkDiagnostics.OutMessageEvent += Capture;
            try
            {
                var transport = transportObject.AddComponent<kcp2k.KcpTransport>();
                typeof(kcp2k.KcpTransport).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(transport, new kcp2k.KcpConfig());
                Transport.active = transport;
                sender.SendBaseline(connection);
                if (current == UnitOrderType.None) Assert.That(messages, Is.Empty);
                else
                {
                    Assert.That(messages.Count, Is.EqualTo(1));
                    Assert.That(messages[0].EntityId, Is.EqualTo(42));
                    Assert.That(messages[0].Order, Is.EqualTo(current));
                    Assert.That(messages[0].IsNewOrder, Is.False);
                }
            }
            finally
            {
                NetworkDiagnostics.OutMessageEvent -= Capture;
                connection.Cleanup();
                Transport.active = previousTransport;
                Object.DestroyImmediate(transportObject);
            }
        }

        private sealed class OrderFacade : IUnitOrderReplicaFacade
        {
            public UnitOrderType CurrentOrder { get; set; }
            public int OrderRevision => 1;
            public void ApplyReplicaOrder(UnitOrderType order, bool isNewOrder) => CurrentOrder = order;
        }
    }
}
