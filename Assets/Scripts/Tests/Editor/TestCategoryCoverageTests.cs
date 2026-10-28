using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class TestCategoryCoverageTests
    {
        // Feature runs select fixtures by category; an untagged fixture would only run in the full suite.
        [Test]
        public void EveryFixture_HasAFeatureCategory()
        {
            const BindingFlags METHODS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly;
            string[] untagged = typeof(TestCategoryCoverageTests).Assembly.GetTypes()
                .Where(type => type.GetMethods(METHODS).Any(IsTest))
                .Where(type => !type.GetCustomAttributes(typeof(CategoryAttribute), true).Any())
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(untagged, Is.Empty, "Add [Category(\"<Feature>\")] to these fixtures (see AGENTS.md Test Selection).");
        }

        private static bool IsTest(MethodInfo method)
        {
            return method.GetCustomAttributes(true).Any(attribute => attribute is ISimpleTestBuilder or ITestBuilder);
        }
    }
}
