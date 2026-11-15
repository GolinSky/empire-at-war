using System;
using System.IO;
using EmpireAtWar.Services.Networking.Online;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class ReconnectSessionStoreTests
    {
        private string _directory;
        private string RecordPath => Path.Combine(_directory, "reconnect-session.dat");

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "ReconnectTests-" + Guid.NewGuid());

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [TestCase(ReconnectTransport.Kcp)]
        [TestCase(ReconnectTransport.Steam)]
        public void Save_RestoresAfterAbruptProcessLossWithoutDisconnectCallback(ReconnectTransport transport)
        {
            ReconnectSessionRecord original = CreateRecord(transport);
            new ReconnectSessionStore(_directory).Save(original);

            Assert.That(new ReconnectSessionStore(_directory).TryLoad(out var restored, out string error), Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(restored.Transport, Is.EqualTo(transport));
            Assert.That(restored.Host, Is.EqualTo(original.Host));
            Assert.That(restored.Port, Is.EqualTo(original.Port));
            Assert.That(restored.LobbyId, Is.EqualTo(original.LobbyId));
            Assert.That(restored.SteamId, Is.EqualTo(original.SteamId));
            Assert.That(restored.MatchId, Is.EqualTo(original.MatchId));
            Assert.That(restored.Credential, Is.EqualTo(original.Credential));
            Assert.That(restored.SlotIndex, Is.EqualTo(original.SlotIndex));
        }

        [Test]
        public void InterruptedReplacement_KeepsCommittedSessionAcrossRepeatedRestarts()
        {
            var store = new ReconnectSessionStore(_directory);
            store.Save(CreateRecord());
            File.WriteAllText(RecordPath + ".tmp", "incomplete replacement");

            for (int restart = 0; restart < 3; restart++)
            {
                Assert.That(new ReconnectSessionStore(_directory).TryLoad(out var restored, out _), Is.True);
                Assert.That(restored.MatchId, Is.EqualTo("original-match"));
                Assert.That(restored.Credential, Is.EqualTo("original-credential"));
            }
        }

        [Test]
        public void Save_ReplacesPreviousSessionAndInterruptedTemporaryWrite()
        {
            var store = new ReconnectSessionStore(_directory);
            store.Save(CreateRecord());
            File.WriteAllText(RecordPath + ".tmp", "incomplete replacement");
            ReconnectSessionRecord replacement = CreateRecord();
            replacement.MatchId = "next-match";
            replacement.Credential = "next-credential";
            store.Save(replacement);

            Assert.That(store.TryLoad(out var restored, out _), Is.True);
            Assert.That(restored.MatchId, Is.EqualTo("next-match"));
            Assert.That(restored.Credential, Is.EqualTo("next-credential"));
            Assert.That(File.Exists(RecordPath + ".tmp"), Is.False);
        }

        [Test]
        public void UncommittedFirstWrite_DoesNotOfferIncompleteSession()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(RecordPath + ".tmp", "incomplete first write");
            Assert.That(new ReconnectSessionStore(_directory).TryLoad(out var restored, out string error), Is.False);
            Assert.That(restored, Is.Null);
            Assert.That(error, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(999)]
        public void CorruptOrUnsupportedRecord_IsDiscardedWithSafeMessage(int version)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllBytes(RecordPath, BitConverter.GetBytes(version));
            Assert.That(new ReconnectSessionStore(_directory).TryLoad(out _, out string error), Is.False);
            Assert.That(error, Does.Contain("invalid or unsupported"));
            Assert.That(File.Exists(RecordPath), Is.False);
        }

        [Test]
        public void InvalidReplacement_PreservesCommittedRecord()
        {
            var store = new ReconnectSessionStore(_directory);
            store.Save(CreateRecord());
            ReconnectSessionRecord invalid = CreateRecord();
            invalid.Credential = string.Empty;
            Assert.Throws<InvalidDataException>(() => store.Save(invalid));
            Assert.That(store.TryLoad(out var restored, out _), Is.True);
            Assert.That(restored.Credential, Is.EqualTo("original-credential"));
        }

        [Test]
        public void MalformedStringLength_IsDiscardedWithoutExposingCredentials()
        {
            Directory.CreateDirectory(_directory);
            using (var writer = new BinaryWriter(File.Create(RecordPath)))
            {
                writer.Write(1);
                writer.Write((int)ReconnectTransport.Kcp);
                writer.Write(new byte[] { 255, 255, 255, 255, 15 });
            }
            Assert.That(new ReconnectSessionStore(_directory).TryLoad(out _, out string error), Is.False);
            Assert.That(error, Does.Contain("invalid or unsupported"));
            Assert.That(File.Exists(RecordPath), Is.False);
        }

        [Test]
        public void OversizedRecord_IsDiscarded()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllBytes(RecordPath, new byte[16385]);
            Assert.That(new ReconnectSessionStore(_directory).TryLoad(out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(File.Exists(RecordPath), Is.False);
        }

        [Test]
        public void Clear_RemovesCommittedAndInterruptedRecords()
        {
            var store = new ReconnectSessionStore(_directory);
            store.Save(CreateRecord());
            File.WriteAllText(RecordPath + ".tmp", "incomplete replacement");
            store.Clear();
            Assert.That(store.TryLoad(out _, out string error), Is.False);
            Assert.That(error, Is.Empty);
            Assert.That(File.Exists(RecordPath + ".tmp"), Is.False);
        }

        private static ReconnectSessionRecord CreateRecord(ReconnectTransport transport = ReconnectTransport.Kcp) =>
            new ReconnectSessionRecord
            {
                Transport = transport,
                Host = transport == ReconnectTransport.Kcp ? "127.0.0.1" : "76561198000000001",
                Port = 7777,
                LobbyId = 109775241000000001,
                SteamId = 76561198000000002,
                MatchId = "original-match",
                Credential = "original-credential",
                SlotIndex = 1
            };
    }
}
