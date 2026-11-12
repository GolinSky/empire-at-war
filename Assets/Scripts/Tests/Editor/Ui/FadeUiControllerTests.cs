using System;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Services.Fade;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class FadeUiControllerTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/Fade/FadeUi.prefab";

        [Test]
        public void ServiceCanCoverBeforeInitializeAndReusesOneView()
        {
            FadeUi prefab = AssetDatabase.LoadAssetAtPath<FadeUi>(PREFAB_PATH);
            FadeUi ui = UnityEngine.Object.Instantiate(prefab);
            try
            {
                ui.Hide();
                var factory = new UiServiceStub(ui);
                var controller = new FadeUiController(factory, null);
                var service = new FadeService(controller);
                Assert.That(factory.CreationCount, Is.Zero);
                service.Cover();
                service.Cover();
                Assert.That(factory.CreationCount, Is.EqualTo(1));
                Assert.That(ui.IsVisible, Is.True);
                Assert.That(ui.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(ui.gameObject); }
        }

        [Test]
        public void ReplacementCancelsPreviousAwaitableAndCanCompleteNewFade()
        {
            FadeUi ui = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<FadeUi>(PREFAB_PATH));
            try
            {
                Task first = Observe(ui.FadeInAsync(10f, CancellationToken.None));
                Task second = Observe(ui.FadeOutAsync(10f, CancellationToken.None));
                Assert.That(first.IsCanceled, Is.True);
                DOTween.Complete(new SerializedObject(ui).FindProperty("fadeImage").objectReferenceValue);
                Assert.That(second.IsCompletedSuccessfully, Is.True);
                Assert.That(ui.IsVisible, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(ui.gameObject); }
        }

        [Test]
        public void CoverCancelsAnOutstandingFade()
        {
            FadeUi ui = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<FadeUi>(PREFAB_PATH));
            try
            {
                Task operation = Observe(ui.FadeInAsync(10f, CancellationToken.None));
                ui.Cover();
                Assert.That(operation.IsCanceled, Is.True);
                Assert.That(ui.IsVisible, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(ui.gameObject); }
        }

        [Test]
        public void PreCanceledTokenDoesNotShowUiOrStartTween()
        {
            FadeUi ui = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<FadeUi>(PREFAB_PATH));
            try
            {
                ui.Hide();
                Task operation = Observe(ui.FadeInAsync(10f, new CancellationToken(true)));
                Assert.That(operation.IsCanceled, Is.True);
                Assert.That(ui.IsVisible, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(ui.gameObject); }
        }

        [Test]
        public void TokenCancellationCompletesTheAwaitableAndStopsItsTween()
        {
            FadeUi ui = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<FadeUi>(PREFAB_PATH));
            using var cancellation = new CancellationTokenSource();
            try
            {
                Task operation = Observe(ui.FadeInAsync(10f, cancellation.Token));
                cancellation.Cancel();
                Assert.That(operation.IsCanceled, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(ui.gameObject); }
        }

        [Test]
        public void ControllerCancelsFadeAfterInactiveViewWasDestroyed()
        {
            FadeUi ui = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<FadeUi>(PREFAB_PATH));
            var controller = new FadeUiController(new UiServiceStub(ui), null);
            Task operation = Observe(controller.FadeInAsync(10f, CancellationToken.None));
            UnityEngine.Object.DestroyImmediate(ui.gameObject);
            controller.LateDispose();
            Assert.That(operation.IsCanceled, Is.True);
        }

        private static async Task Observe(Awaitable operation) => await operation;

        private sealed class UiServiceStub : IUiService
        {
            private readonly FadeUi _ui;
            public int CreationCount { get; private set; }
            public UiServiceStub(FadeUi ui) { _ui = ui; }
            public TUi CreateUi<TUi>() where TUi : BaseUi
            {
                CreationCount++;
                return (TUi)(BaseUi)_ui;
            }
            public void SetContentLayout(bool isFaction, bool isGroup) => throw new NotSupportedException();
            public void SetContentVisible(bool isVisible) => throw new NotSupportedException();
            public void SetHudVisible(bool isVisible) => throw new NotSupportedException();
        }
    }
}
