using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using EmpireAtWar.Editor.AloImport;
using Newtonsoft.Json.Linq;

public static class AloUnityStage
{
    public static JObject Main(string jobId, string operationId, string inputHash, string stage, string jobDirectory)
    {
        string root = Path.GetFullPath(AloImportLayout.JobsRoot) + Path.DirectorySeparatorChar;
        string directory = Path.GetFullPath(jobDirectory).TrimEnd(Path.DirectorySeparatorChar);
        if (!directory.StartsWith(root, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(directory) != Guid.Parse(jobId).ToString("D"))
            throw new InvalidOperationException("OUTPUT_CONFLICT: job directory does not belong to this job.");
        var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
        JObject fingerprint = JObject.Parse(File.ReadAllText(Path.Combine(directory, "InputHash.json")), settings);
        string inputPath = Path.Combine(directory, "Input.json");
        using var algorithm = SHA256.Create();
        string fileHash = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(inputPath))).Replace("-", "").ToLowerInvariant();
        if ((string)fingerprint["sha256"] != inputHash || (string)fingerprint["fileSha256"] != fileHash)
            throw new InvalidOperationException("INPUT_CHANGED: frozen job input differs.");
        JObject input = JObject.Parse(File.ReadAllText(inputPath), settings);
        JObject recipe = (JObject)input["recipe"];
        AloRecipeSchemaValidator.Validate(recipe);
        if (stage != "verify" && stage != "apply" && stage != "recover") throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: shared " + stage + " adapter has not passed migration acceptance.");
        if (stage == "apply" && ((string)recipe["operation"] != "update" || (string)recipe["kind"] != "ship" || (string)recipe["integration"]["bindingProfile"] != "existing-assets-v1"))
            throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: publication supports existing-assets-v1 ship updates only.");
        JObject record = stage == "recover"
            ? AloEditorOperationGate.Recover(jobId, operationId, inputHash, () => AloNoWriteRecovery.Reconcile(directory, jobId))
            : AloEditorOperationGate.Execute(jobId, operationId, inputHash, () => stage == "apply" ? Apply(recipe, jobId, directory) : VerifyJob(recipe, jobId, directory));
        string receipt = Path.Combine(directory, "OperationResult.json");
        string temporary = receipt + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(record.ToString());
            writer.Flush();
            stream.Flush(true);
        }
        if (File.Exists(receipt)) File.Replace(temporary, receipt, null);
        else File.Move(temporary, receipt);
        return record;
    }

    private static JObject Verify(JObject recipe)
    {
        var result = new JObject { ["savedAssets"] = AloSavedAssetVerifier.Verify(recipe) };
        if ((string)recipe["integration"]["bindingProfile"] == "existing-assets-v1" && (string)recipe["kind"] == "ship")
        {
            result["ship"] = AloExistingShipBuilder.Verify(recipe);
            result["registrations"] = AloRegistrationWriter.Verify(recipe);
        }
        return result;
    }

    private static JObject VerifyJob(JObject recipe, string jobId, string directory)
    {
        JObject result = Verify(recipe);
        if (!AloInvocationPolicy.AllowsAssetWrites(directory))
        {
            result["candidateIdentity"] = new JObject { ["state"] = "not-run-policy", ["reason"] = "This invocation forbids candidate asset imports." };
            return result;
        }
        string conversionPath = Path.Combine(directory, "ConversionManifest.json");
        if (!File.Exists(conversionPath))
        {
            result["candidateIdentity"] = new JObject { ["state"] = "not-run", ["reason"] = "No completed conversion manifest" };
            return result;
        }
        if (File.Exists(Path.Combine(directory, "AppliedState.json")))
        {
            ValidateAppliedSnapshot(directory, jobId);
            result["candidateIdentity"] = new JObject { ["state"] = "not-run", ["reason"] = "Applied-state verification uses persisted output fingerprints" };
            return result;
        }
        var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
        JObject conversion = JObject.Parse(File.ReadAllText(conversionPath), settings);
        if ((string)conversion["state"] != "succeeded") throw new InvalidOperationException("STAGE_REQUIRED: candidate identity requires completed conversion.");
        string staging = Path.Combine(directory, "staging");
        string publicationPath = Path.Combine(staging, "PublicationManifest.json");
        JToken publication = conversion["outputs"].Single(output => (string)output["path"] == "staging/PublicationManifest.json");
        using var algorithm = SHA256.Create();
        string hash = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(publicationPath))).Replace("-", "").ToLowerInvariant();
        if (hash != (string)publication["sha256"]) throw new InvalidOperationException("INPUT_CHANGED: publication manifest differs from completed conversion.");
        JObject manifest = JObject.Parse(File.ReadAllText(publicationPath), settings);
        JObject input = JObject.Parse(File.ReadAllText(Path.Combine(directory, "InputHash.json")), settings);
        JObject plan = JObject.Parse(File.ReadAllText(Path.Combine(directory, "BuildPlan.json")), settings);
        if ((string)manifest["jobId"] != jobId || (string)manifest["inputHash"] != (string)input["sha256"] || (string)manifest["planHash"] != (string)plan["planHash"])
            throw new InvalidOperationException("INPUT_CHANGED: candidate publication identity differs from the frozen job/plan.");
        AloUnityArtBuilder.Preflight(recipe, manifest, staging);
        result["candidateIdentity"] = AloModelCandidateImporter.Verify(recipe, manifest, staging, jobId, directory);
        return result;
    }

    private static void ValidateAppliedSnapshot(string directory, string jobId)
    {
        var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
        JObject applied = JObject.Parse(File.ReadAllText(Path.Combine(directory, "AppliedState.json")), settings);
        JObject input = JObject.Parse(File.ReadAllText(Path.Combine(directory, "InputHash.json")), settings);
        JObject plan = JObject.Parse(File.ReadAllText(Path.Combine(directory, "BuildPlan.json")), settings);
        if ((string)applied["jobId"] != jobId || (string)applied["inputHash"] != (string)input["sha256"] || (string)applied["planHash"] != (string)plan["planHash"])
            throw new InvalidOperationException("INPUT_CHANGED: applied-state identity differs.");
        foreach (JProperty baseline in ((JObject)applied["baseline"]).Properties())
        {
            AloRecipeSemanticValidator.SafePath(baseline.Name, false);
            string current = null;
            if (File.Exists(baseline.Name))
            {
                using var algorithm = SHA256.Create();
                current = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(baseline.Name))).Replace("-", "").ToLowerInvariant();
            }
            if (current != (string)baseline.Value) throw new InvalidOperationException("INPUT_CHANGED: applied-state output differs: " + baseline.Name);
        }
    }

    private static JObject Apply(JObject recipe, string jobId, string directory)
    {
        AloInvocationPolicy.RequireAssetWrite(directory, recipe["integration"]["outputs"].Select(output => (string)output["path"]));
        AloExistingShipBuilder.Preflight(recipe);
        AloSavedAssetVerifier.Verify(recipe);
        AloRegistrationWriter.Preflight(recipe);
        string staging = Path.Combine(directory, "staging");
        JObject manifest = JObject.Parse(File.ReadAllText(Path.Combine(staging, "PublicationManifest.json")),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        string[] outputPaths = recipe["integration"]["outputs"].Select(output => (string)output["path"]).ToArray();
        AloPublicationJournal.Prepare(directory, outputPaths);
        JObject art = AloUnityArtBuilder.PublishFiles(recipe, manifest, staging, jobId, directory);
        JObject bindings = AloExistingShipBuilder.ApplyBindings(recipe, directory);
        JObject registrations = AloRegistrationWriter.Apply(recipe, directory);
        JObject verified = Verify(recipe);
        AloPublicationJournal.Checkpoint(directory, "savedVerificationComplete", outputPaths);
        return new JObject { ["art"] = art, ["bindings"] = bindings, ["registrations"] = registrations, ["verification"] = verified };
    }
}
