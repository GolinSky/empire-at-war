using System.Reflection;
using DG.Tweening;
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
        public void Destroy_KillsFillSequence()
        {
            GameObject gameObject = new GameObject(nameof(PipelineView));
            gameObject.SetActive(false);

            try
            {
                Image fillIcon = gameObject.AddComponent<Image>();
                Button skipButton = gameObject.AddComponent<Button>();
                PipelineView view = gameObject.AddComponent<PipelineView>();
                SetField(view, "fillIcon", fillIcon);
                SetField(view, "skipButton", skipButton);
                gameObject.SetActive(true);

                view.Fill(10f, "test-pipeline");
                Sequence sequence = GetFillSequence(view);

                Assert.That(sequence.IsPlaying(), Is.True);

                InvokeOnDestroy(view);

                Assert.That(sequence.IsPlaying(), Is.False);
            }
            finally
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        private static void InvokeOnDestroy(PipelineView view)
        {
            MethodInfo method = typeof(PipelineView).GetMethod(
                "OnDestroy",
                PRIVATE_INSTANCE);
            Assert.That(method, Is.Not.Null);
            method.Invoke(view, null);
        }

        private static Sequence GetFillSequence(PipelineView view)
        {
            FieldInfo field = typeof(PipelineView).GetField(
                "_fillImageSequence",
                PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null);
            return (Sequence)field.GetValue(view);
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
