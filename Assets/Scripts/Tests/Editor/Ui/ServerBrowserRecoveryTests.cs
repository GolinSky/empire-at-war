using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmpireAtWar.Entities.MainMenu.ServerBrowser;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Ui.Base;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class ServerBrowserRecoveryTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/ServerBrowserUi.prefab";
        private string _directory;
        private ReconnectSessionStore _store;
        private OnlineMatchService _online;
        private UiCancelRouter _cancelRouter;
        private PrefabUiService _ui;
        private ServerBrowserRouteUiController _browser;
        private GameObject _network;

        [SetUp]
        public void SetUp()
        {
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False, "Requires no running network session.");
            _directory = Path.Combine(Path.GetTempPath(), "ServerBrowserRecoveryTests-" + Guid.NewGuid().ToString("N"));
            _store = new ReconnectSessionStore(_directory);
            _online = new OnlineMatchService(null, new OnlineRecoveryProgress());
            Set("_recoveryStore", _store);
            _network = new GameObject("Server browser test manager");
            _network.SetActive(false);
            Set("_manager", _network.AddComponent<OnlineNetworkManager>());
            _cancelRouter = new UiCancelRouter(new CancelInput());
            _cancelRouter.Initialize();
            _ui = new PrefabUiService();
            _browser = new ServerBrowserRouteUiController(_ui, _cancelRouter, _online);
            _browser.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _browser.LateDispose();
            _cancelRouter.Dispose();
            _ui.Dispose();
            UnityEngine.Object.DestroyImmediate(_network);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BackWhileConnecting_KeepsSavedBattle(bool rejoiningSavedBattle)
        {
            _browser.Open();
            RememberSession();
            Set("<Role>k__BackingField", OnlineRole.Client);
            Set("_recovering", rejoiningSavedBattle);
            Assert.That(_online.IsReconnecting, Is.EqualTo(rejoiningSavedBattle));

            _browser.Close();

            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.IsReconnecting, Is.False);
            Assert.That(_online.HasRecovery, Is.True);
            Assert.That(_store.TryLoad(out ReconnectSessionRecord restored, out _), Is.True);
            Assert.That(restored.Credential, Is.EqualTo("private-credential"));
        }

        private void RememberSession()
        {
            var record = new ReconnectSessionRecord { Transport = ReconnectTransport.Kcp, Host = "127.0.0.1",
                Port = 7777, MatchId = "match", Credential = "private-credential", SlotIndex = 1 };
            _store.Save(record);
            Set("_recovery", record);
        }

        private void Set<T>(string field, T value) => typeof(OnlineMatchService)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_online, value);

        private sealed class CancelInput : ICancelInput
        {
            public event Action CancelPressed;
            public void Press() => CancelPressed?.Invoke();
        }

        private sealed class PrefabUiService : IUiService, IDisposable
        {
            private readonly List<GameObject> _roots = new List<GameObject>();

            public TUi CreateUi<TUi>() where TUi : BaseUi
            {
                GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
                _roots.Add(root);
                return root.GetComponent<TUi>();
            }

            public void SetHudVisible(bool isVisible) { }
            public void SetContentLayout(bool isFaction, bool isGroup) { }
            public void SetContentVisible(bool isVisible) { }

            public void Dispose()
            {
                foreach (GameObject root in _roots) PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
