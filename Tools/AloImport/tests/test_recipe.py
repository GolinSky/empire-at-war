"""Contract tests use synthetic decisions, never stand in for a reviewed model."""

import copy
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from recipe import RecipeError, load_recipe, validate_recipe


def valid_recipe():
    return {
        "schemaVersion": 1, "modelId": "Fixture", "kind": "ship", "operation": "update",
        "source": {"root": "fixture", "alo": "Models/Hull.ALO"},
        "pieces": [{"id": "hull", "alo": "Models/Hull.ALO", "purpose": "hull"}],
        "attachments": [], "identity": {"faction": "Republic", "unit": {"enum": "ShipType", "name": "Fixture", "value": 9999}},
        "integration": {"templateGuid": "a" * 32, "bindingProfile": "ship-v1", "retainedDependencyGuids": [],
                        "outputs": [{"role": "model", "path": "Assets/Art/Models/Fixture.fbx", "guid": "b" * 32}], "bindings": []},
        "dimensions": {"targetLength": 160, "sizeRule": "visible-length", "orientationEulerDegrees": [0, 0, 0], "pivot": "source-root", "bankingDegrees": 5, "margins": []},
        "materials": [{"selector": {"piece": "hull", "shader": "FixtureShader", "parameters": []}, "surface": "opaque", "textures": [], "normalConvention": "none", "livery": {"policy": "none", "parameters": []}}],
        "hardpoints": [], "hangar": [], "gameplay": {"baselineGuid": "c" * 32, "values": [], "abilityGuids": []},
        "derivatives": {"placement": True, "wreckPieces": ["hull"], "icon": {"resolution": 512, "transparent": True, "framingMargin": 0.1}, "helperPolicy": "retain", "helperExclusions": [], "shieldPolicy": "retain-baseline"},
        "decisions": [{"key": "fixture", "value": "synthetic", "provenance": "unit-test", "status": "resolved"}], "exceptions": [],
        "verification": {"profile": "static-source-fidelity-v1", "overrides": [], "renders": ["model"], "checkGroups": ["source"]},
    }


class RecipeTests(unittest.TestCase):
    def test_complete_synthetic_recipe(self):
        validate_recipe(valid_recipe())

    def test_unknown_nested_field_and_unsupported_version(self):
        for section, field, value in [("source", "script", "evil.py"), ("dimensions", "units", "meters")]:
            recipe = valid_recipe()
            recipe[section][field] = value
            with self.assertRaises(RecipeError):
                validate_recipe(recipe)
        recipe = valid_recipe()
        recipe["schemaVersion"] = 2
        with self.assertRaises(RecipeError):
            validate_recipe(recipe)

    def test_missing_decisions_and_unsupported_kind_fail(self):
        for field in ("identity", "materials", "dimensions", "gameplay", "verification"):
            recipe = valid_recipe()
            recipe.pop(field)
            with self.subTest(field=field), self.assertRaises(RecipeError):
                validate_recipe(recipe)
        recipe = valid_recipe()
        recipe["kind"] = "squadron"
        with self.assertRaises(RecipeError):
            validate_recipe(recipe)

    def test_material_signatures_do_not_collapse_shader_or_parameters(self):
        recipe = valid_recipe()
        second = copy.deepcopy(recipe["materials"][0])
        second["selector"]["shader"] = "AdditiveShader"
        second["surface"] = "additive"
        recipe["materials"].append(second)
        validate_recipe(recipe)
        recipe["materials"][1] = copy.deepcopy(recipe["materials"][0])
        with self.assertRaisesRegex(RecipeError, "duplicate selector"):
            validate_recipe(recipe)

    def test_no_guessed_choices_or_missing_output_baseline(self):
        for section, field, value in [("decisions", "status", "unresolved"), ("integration", "guid", None)]:
            recipe = valid_recipe()
            target = recipe["decisions"][0] if section == "decisions" else recipe["integration"]["outputs"][0]
            target[field] = value
            with self.assertRaises(RecipeError):
                validate_recipe(recipe)

    def test_paths_are_portable_and_confined(self):
        for path in ["../Hull.ALO", "D:/Hull.ALO", "Models\\Hull.ALO", "Models//Hull.ALO", "Models/NUL.ALO", "Models/file. "]:
            recipe = valid_recipe()
            recipe["source"]["alo"] = path
            with self.subTest(path=path), self.assertRaises(RecipeError):
                validate_recipe(recipe)
        for path in ["Temp/model.fbx", "Assets/../model.fbx", "Assets/AddressableAssetsData/model.asset"]:
            recipe = valid_recipe()
            recipe["integration"]["outputs"][0]["path"] = path
            with self.subTest(path=path), self.assertRaises(RecipeError):
                validate_recipe(recipe)

    def test_attachment_indices_and_piece_identity(self):
        recipe = valid_recipe()
        recipe["attachments"] = [{"id": "turret1", "piece": "missing", "parentPiece": "hull", "boneIndex": 0}]
        with self.assertRaisesRegex(RecipeError, "unknown piece"):
            validate_recipe(recipe)
        recipe["pieces"].append(copy.deepcopy(recipe["pieces"][0]))
        with self.assertRaisesRegex(RecipeError, "duplicate id"):
            validate_recipe(recipe)

    def test_duplicate_json_keys_and_nonfinite_values(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "recipe.json"
            path.write_text('{"schemaVersion":1,"schemaVersion":2}', encoding="utf-8")
            with self.assertRaisesRegex(RecipeError, "duplicate JSON key"):
                load_recipe(path)
        recipe = valid_recipe()
        recipe["dimensions"]["targetLength"] = float("nan")
        with self.assertRaisesRegex(RecipeError, "non-finite"):
            validate_recipe(recipe)

    def test_station_requires_ordered_unique_hulls(self):
        recipe = valid_recipe()
        recipe["kind"] = "station"
        recipe["pieces"][0]["purpose"] = "level"
        recipe["dimensions"].pop("targetLength")
        recipe["dimensions"].update(targetDiameter=100, sizeRule="common-diameter")
        recipe["station"] = {"levels": [{"level": 1, "piece": "hull", "gameplayDataGuid": "a" * 32}], "alignmentAnchor": {"piece": "hull", "boneIndex": 0}, "mounts": []}
        validate_recipe(recipe)
        recipe["station"]["levels"][0]["level"] = 2
        with self.assertRaisesRegex(RecipeError, "contiguous"):
            validate_recipe(recipe)

    def test_targetable_weapon_exception_requires_approval(self):
        recipe = valid_recipe()
        recipe["hardpoints"] = [{"id": 9, "kind": "weapon", "profileGuid": "a" * 32, "profileKey": 9, "anchor": {"piece": "hull", "boneIndex": 0}, "yawRangeDegrees": [-90, 90], "muzzles": [], "health": 20, "targetable": False}]
        with self.assertRaisesRegex(RecipeError, "approved exception"):
            validate_recipe(recipe)
        recipe["exceptions"] = [{"ruleId": "targetable-weapon", "sourceIds": ["hardpoint:9"], "reason": "test", "evidence": ["test"], "approval": "test-user-approval"}]
        validate_recipe(recipe)

    def test_internal_binding_identity_and_weapon_catalog_key(self):
        recipe = valid_recipe()
        recipe["integration"]["bindings"] = [{"component": {"role": "model", "fileId": 123}, "property": "launchPoint", "target": {"role": "model", "fileId": 456}}]
        validate_recipe(recipe)
        recipe["integration"]["bindings"][0]["target"]["role"] = "missing"
        with self.assertRaisesRegex(RecipeError, "unknown output role"):
            validate_recipe(recipe)
        recipe["integration"]["bindings"] = []
        recipe["hardpoints"] = [{"id": 21, "kind": "weapon", "profileGuid": "a" * 32, "anchor": {"piece": "hull", "boneIndex": 0}, "yawRangeDegrees": [-175, 20], "muzzles": [], "health": 400, "targetable": True}]
        with self.assertRaisesRegex(RecipeError, "typed profileKey"):
            validate_recipe(recipe)
        recipe["hardpoints"][0]["profileKey"] = 121
        validate_recipe(recipe)
        recipe["hardpoints"][0]["yawRangeDegrees"] = [20, -175]
        with self.assertRaisesRegex(RecipeError, "reversed"):
            validate_recipe(recipe)

    def test_texture_override_requires_explicit_safe_unique_source(self):
        recipe = valid_recipe()
        recipe["source"]["textureOverrides"] = [{"source": "Hull.tga", "path": "Textures/Hull.dds"}]
        validate_recipe(recipe)
        recipe["source"]["textureOverrides"].append({"source": "hull.TGA", "path": "Textures/Other.dds"})
        with self.assertRaisesRegex(RecipeError, "duplicate source"):
            validate_recipe(recipe)
        recipe["source"]["textureOverrides"] = [{"source": "Hull.tga", "path": "../Textures/Hull.dds"}]
        with self.assertRaisesRegex(RecipeError, "safe relative path"):
            validate_recipe(recipe)


if __name__ == "__main__":
    unittest.main()
