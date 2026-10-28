using System;
using System.IO;
using EmpireAtWar.Editor.AloImport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class AloEditorOperationTests
    {
        private string _directory;
        private string _job;
        private string _operation;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "AloGateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _job = Guid.NewGuid().ToString("D");
            _operation = Guid.NewGuid().ToString("D");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_directory, true);

        [Test]
        public void DuplicateSubmission_DoesNotReplayAndChangedInputFails()
        {
            var store = new AloEditorOperationStore(_directory, "session");
            int calls = 0;
            store.Execute(_job, _operation, "hash", () => { calls++; return new JObject(); });
            store.Execute(_job, _operation, "hash", () => { calls++; return new JObject(); });
            Assert.That(calls, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => store.Execute(_job, _operation, "changed", () => new JObject()));
        }

        [Test]
        public void CallbackFailure_BlocksOtherWriterUntilRecovery()
        {
            var store = new AloEditorOperationStore(_directory, "session");
            Assert.Throws<IOException>(() => store.Execute(_job, _operation, "hash", () => throw new IOException("injected")));
            Assert.That((string)store.Read()["state"], Is.EqualTo("needs-recovery"));
            Assert.Throws<InvalidOperationException>(() => store.Execute(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"), "hash", () => new JObject()));
            store.Recover(_job, _operation, "hash", () => new JObject { ["verified"] = true });
            Assert.That((string)store.Read()["state"], Is.EqualTo("idle"));
        }

        [Test]
        public void NestedWriter_CannotAcquireEditorLock()
        {
            var first = new AloEditorOperationStore(_directory, "first");
            var second = new AloEditorOperationStore(_directory, "second");
            first.Execute(_job, _operation, "hash", () =>
            {
                Assert.Throws<IOException>(() => second.Execute(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"), "hash", () => new JObject()));
                return new JObject();
            });
        }

        [Test]
        public void NestedFailure_NeverReceivesCompletionMarker()
        {
            var store = new AloEditorOperationStore(_directory, "session");
            Assert.Throws<InvalidOperationException>(() => store.Execute(_job, _operation, "hash", () =>
                new JObject { ["result"] = new JObject { ["success"] = false } }));
            Assert.That((string)store.Read()["state"], Is.EqualTo("needs-recovery"));
            Assert.That(Directory.Exists(Path.Combine(_directory, "Operations")), Is.False);
        }

        [Test]
        public void ReloadedSession_DoesNotTreatOrphanRecordAsSuccess()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "EditorOperation.json"), new JObject
            {
                ["jobId"] = _job, ["operationId"] = _operation, ["inputHash"] = "hash",
                ["sessionNonce"] = "old", ["state"] = "running"
            }.ToString());
            var current = new AloEditorOperationStore(_directory, "new");
            Assert.Throws<InvalidOperationException>(() => current.Execute(_job, _operation, "hash", () => new JObject()));
            current.Recover(_job, _operation, "hash", () => new JObject { ["verifiedQuiescent"] = true });
            Assert.That((string)current.Read()["state"], Is.EqualTo("idle"));
        }

        [Test]
        public void RecipeValidator_RejectsUnknownFieldsAndUnsafePaths()
        {
            JObject recipe = AloRecipeSchemaValidator.Load("Tools/AloImport/tests/fixtures/ValidRecipe.json");
            recipe["source"]["script"] = "unsafe.py";
            Assert.Throws<JsonException>(() => AloRecipeSchemaValidator.Validate(recipe));
            Assert.Throws<JsonException>(() => AloRecipeSemanticValidator.SafePath("Assets/../Other.asset", true));
        }
    }
}
