using EmpireAtWar.Services.Networking.Online;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

/// <summary>
/// Keeps the real Steam API off for every test run (Editor UI, CLI and Pipeline runs alike). Tests build project
/// containers and enter Play Mode repeatedly; Steam cannot be started and stopped reliably inside one Editor
/// process. Re-registered after each domain reload so the run-finished callback still clears the flag.
/// </summary>
[InitializeOnLoad]
public sealed class SteamTestIsolationGuard : ICallbacks
{
    static SteamTestIsolationGuard() => TestRunnerApi.RegisterTestCallback(new SteamTestIsolationGuard());

    public void RunStarted(ITestAdaptor testsToRun) => SteamTestIsolation.SetActive(true);

    public void RunFinished(ITestResultAdaptor result) => SteamTestIsolation.SetActive(false);

    public void TestStarted(ITestAdaptor test)
    {
    }

    public void TestFinished(ITestResultAdaptor result)
    {
    }
}
