using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Other chats share Pipeline's status file; retain this unit's results separately.
public sealed class AllegianceTestCapture : ICallbacks
{
    private readonly Dictionary<string, object> _results = new Dictionary<string, object>();
    private TestRunnerApi _api;

    public static string Main()
    {
        var capture = new AllegianceTestCapture();
        capture._api = ScriptableObject.CreateInstance<TestRunnerApi>();
        capture._api.RegisterCallbacks(capture);
        return "Capturing AllegianceShipTests results in Temp/AllegianceImport/UnitTests.json.";
    }

    public void RunStarted(ITestAdaptor testsToRun) { }
    public void TestStarted(ITestAdaptor test) { }

    public void TestFinished(ITestResultAdaptor result)
    {
        if (result.Test.IsSuite || !result.Test.FullName.Contains("AllegianceShipTests")) return;
        _results[result.Test.FullName] = new {name = result.Test.FullName, status = result.TestStatus.ToString(), result.Message, result.StackTrace};
    }

    public void RunFinished(ITestResultAdaptor result)
    {
        if (_results.Count == 0) return;
        File.WriteAllText("Temp/AllegianceImport/UnitTests.json", JsonConvert.SerializeObject(_results.Values.ToArray(), Formatting.Indented));
        _api.UnregisterCallbacks(this);
        Object.DestroyImmediate(_api);
    }
}
