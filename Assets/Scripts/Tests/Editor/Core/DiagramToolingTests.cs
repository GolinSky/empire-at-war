using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Editor.Diagrams;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class DiagramToolingTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "DiagramToolingTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "Folder", "Nested"));
            File.WriteAllText(Path.Combine(_root, "Folder", "A.cs"), "class A {}");
            File.WriteAllText(Path.Combine(_root, "Folder", "Nested", "B.cs"), "class B {}");
            File.WriteAllText(Path.Combine(_root, "Folder", "Notes.txt"), "text");
            File.WriteAllText(Path.Combine(_root, "C.cs"), "class C {}");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_root, true);

        [Test]
        public void Collect_ExpandsFoldersRecursivelyAndSkipsNonScripts()
        {
            IReadOnlyList<string> scripts = DiagramSourceCollector.Collect(new[] { Path.Combine(_root, "Folder") });

            Assert.That(scripts.Count, Is.EqualTo(2));
            Assert.That(scripts, Has.All.EndsWith(".cs"));
            Assert.That(scripts, Has.None.Contains("\\"));
        }

        [Test]
        public void Collect_MergesFilesAndFoldersWithoutDuplicates()
        {
            string folder = Path.Combine(_root, "Folder");
            IReadOnlyList<string> scripts = DiagramSourceCollector.Collect(new[]
            {
                folder, Path.Combine(folder, "A.cs"), Path.Combine(_root, "C.cs")
            });

            Assert.That(scripts.Count, Is.EqualTo(3));
        }

        [Test]
        public void Collect_MissingPathThrows() =>
            Assert.Throws<FileNotFoundException>(() => DiagramSourceCollector.Collect(new[] { Path.Combine(_root, "Missing") }));

        [Test]
        public void Collect_NonScriptFileThrows() =>
            Assert.Throws<ArgumentException>(() => DiagramSourceCollector.Collect(new[] { Path.Combine(_root, "Folder", "Notes.txt") }));

        [Test]
        public void Collect_FolderWithoutScriptsThrows()
        {
            string empty = Path.Combine(_root, "Empty");
            Directory.CreateDirectory(empty);

            Assert.Throws<ArgumentException>(() => DiagramSourceCollector.Collect(new[] { empty }));
        }

        [Test]
        public void Split_AcceptsCommaAndSemicolon() =>
            Assert.That(DiagramSourceCollector.Split("Assets/A,Assets/B;Assets/C"), Is.EqualTo(new[] { "Assets/A", "Assets/B", "Assets/C" }));

        [Test]
        public void OutputPaths_StayUnderLogsDiagrams()
        {
            Assert.That(DiagramOutputPaths.Puml("enemy_ai"), Is.EqualTo("Logs/Diagrams/enemy_ai/enemy_ai.puml"));
            Assert.That(DiagramOutputPaths.Svg("enemy_ai"), Is.EqualTo("Logs/Diagrams/enemy_ai/enemy_ai.svg"));
            Assert.That(DiagramOutputPaths.Png("enemy_ai"), Is.EqualTo("Logs/Diagrams/enemy_ai/enemy_ai.png"));
        }

        [TestCase("")]
        [TestCase("../escape")]
        [TestCase("a/b")]
        [TestCase("with space")]
        public void OutputPaths_RejectUnsafeNames(string name) =>
            Assert.Throws<ArgumentException>(() => DiagramOutputPaths.Folder(name));

        [TestCase("none", DiagramOpenTarget.None)]
        [TestCase("Rider", DiagramOpenTarget.Rider)]
        [TestCase("editor", DiagramOpenTarget.Editor)]
        [TestCase("BROWSER", DiagramOpenTarget.Browser)]
        public void Parse_AcceptsTargetsCaseInsensitively(string text, DiagramOpenTarget expected) =>
            Assert.That(DiagramOpener.Parse(text), Is.EqualTo(expected));

        [TestCase("vscode")]
        [TestCase("7")]
        public void Parse_RejectsUnknownTargets(string text) =>
            Assert.Throws<ArgumentException>(() => DiagramOpener.Parse(text));
    }
}
