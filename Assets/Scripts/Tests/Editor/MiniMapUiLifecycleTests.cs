using System;
using System.Reflection;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class MiniMapUiLifecycleTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MiniMap/MiniMapUi.prefab";

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitInitializationSubscribesOnceAndBothTeardownOrdersUnsubscribe(bool destroyFirst)
        {
            MiniMapData model = ScriptableObject.CreateInstance<MiniMapData>();
            model.MapRange = new Vector2Range();
            typeof(Vector2Range).BaseType.GetField("<Min>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model.MapRange, Vector2.zero);
            typeof(Vector2Range).BaseType.GetField("<Max>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model.MapRange, Vector2.one * 100f);
            MiniMapUi prefab = AssetDatabase.LoadAssetAtPath<MiniMapUi>(PREFAB_PATH);
            MiniMapUi ui = UnityEngine.Object.Instantiate(prefab);
            try
            {
                ui.AssignPresenter(new PresenterStub());
                ui.SetData(model, new ColorsStub());
                ui.Initialize();
                ui.Initialize();
                foreach (string eventName in new[] { "OnMarkAdded", "OnMarkerAdded", "OnMarkerRemoved" })
                    Assert.That(SubscriberCount(model, eventName), Is.EqualTo(1), eventName);

                if (destroyFirst) UnityEngine.Object.DestroyImmediate(ui.gameObject);
                ui.Dispose();
                ui.Dispose();

                foreach (string eventName in new[] { "OnMarkAdded", "OnMarkerAdded", "OnMarkerRemoved" })
                    Assert.That(SubscriberCount(model, eventName), Is.Zero, eventName);
            }
            finally
            {
                if (ui != null) UnityEngine.Object.DestroyImmediate(ui.gameObject);
                UnityEngine.Object.DestroyImmediate(model);
            }
        }

        private static int SubscriberCount(MiniMapData model, string eventName)
        {
            var handlers = (Delegate)typeof(MiniMapData)
                .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);
            return handlers == null ? 0 : handlers.GetInvocationList().Length;
        }

        private sealed class PresenterStub : IMiniMapPresenter
        {
            public void MoveCamera(Vector3 point) { }
            public void OrderMove(Vector3 point) { }
        }

        private sealed class ColorsStub : IPlayerColors
        {
            public Color GetColor(PlayerId owner) => Color.white;
        }
    }
}
