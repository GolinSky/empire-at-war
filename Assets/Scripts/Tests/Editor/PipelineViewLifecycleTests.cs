using System.Reflection;
using EmpireAtWar.Views.Factions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PipelineViewLifecycleTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Destroy_RemovesSkipListener()
        {
            GameObject gameObject = new GameObject(nameof(PipelineView));
            gameObject.SetActive(false);

            try
            {
                Button skipButton = gameObject.AddComponent<Button>();
                PipelineView view = gameObject.AddComponent<PipelineView>();
                SetField(view, "skipButton", skipButton);
                gameObject.SetActive(true);
                InvokeLifecycle(view, "Awake");

                int cancelCallCount = 0;
                view.Init(_ => cancelCallCount++);
                skipButton.onClick.Invoke();

                Assert.That(cancelCallCount, Is.EqualTo(1));

                InvokeLifecycle(view, "OnDestroy");
                skipButton.onClick.Invoke();

                Assert.That(cancelCallCount, Is.EqualTo(1));
            }
            finally
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        private static void InvokeLifecycle(PipelineView view, string methodName)
        {
            MethodInfo method = typeof(PipelineView).GetMethod(
                methodName,
                PRIVATE_INSTANCE);
            Assert.That(method, Is.Not.Null);
            method.Invoke(view, null);
        }

        private static void SetField(
            PipelineView view,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(PipelineView).GetField(
                fieldName,
                PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null);
            field.SetValue(view, value);
        }
    }
}
