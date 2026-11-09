"""Recipe validation. Recipes describe decisions; they never execute code."""

import json
import math
from pathlib import Path, PurePosixPath
import re

from jsonschema import Draft202012Validator


class RecipeError(ValueError):
    """The recipe is incomplete, unsupported or internally inconsistent."""


SCHEMA_PATH = Path(__file__).with_name("recipe.schema.json")


def _unique(records, key, label):
    values = [record[key].casefold() if isinstance(record[key], str) else record[key]
              for record in records]
    if len(values) != len(set(values)):
        raise RecipeError(f"{label}: duplicate {key}")


def _safe_path(value, label, asset=False):
    path = PurePosixPath(value)
    if ("\\" in value or ":" in value or path.is_absolute()
            or any(part in ("", ".", "..") for part in value.split("/"))
            or any(ord(char) < 32 for char in value)
            or any(char in value for char in '<>"|?*')):
        raise RecipeError(f"{label}: expected a safe relative path using '/' separators")
    if asset and (len(path.parts) < 2 or path.parts[0] != "Assets"
                  or path.parts[1].casefold() == "addressableassetsdata"):
        raise RecipeError(f"{label}: output must be inside Assets and outside AddressableAssetsData")
    for part in path.parts:
        if part.endswith((".", " ")) or re.fullmatch(r"(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?", part):
            raise RecipeError(f"{label}: Windows-reserved path component {part!r}")


def _finite(value, path="recipe"):
    if isinstance(value, float) and not math.isfinite(value):
        raise RecipeError(f"{path}: non-finite numbers are unsupported")
    if isinstance(value, dict):
        for key, item in value.items():
            _finite(item, f"{path}.{key}")
    elif isinstance(value, list):
        for index, item in enumerate(value):
            _finite(item, f"{path}[{index}]")


def _piece_references(value, piece_ids, path="recipe"):
    if isinstance(value, dict):
        for key, item in value.items():
            if key in ("piece", "parentPiece", "artPiece") and item is not None and item not in piece_ids:
                raise RecipeError(f"{path}.{key}: unknown piece {item}")
            _piece_references(item, piece_ids, f"{path}.{key}")
    elif isinstance(value, list):
        for index, item in enumerate(value):
            _piece_references(item, piece_ids, f"{path}[{index}]")


def validate_recipe(recipe):
    """Validate structure and cross-references; perform no filesystem writes."""
    _finite(recipe)
    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    Draft202012Validator.check_schema(schema)
    errors = sorted(Draft202012Validator(schema).iter_errors(recipe), key=lambda error: str(error.json_path))
    if errors:
        error = errors[0]
        raise RecipeError(f"{error.json_path}: {error.message}")
    _safe_path(recipe["modelId"], "modelId")
    _safe_path(recipe["source"]["alo"], "source.alo")
    for field in ("xml", "textureDirectory"):
        if field in recipe["source"]:
            _safe_path(recipe["source"][field], f"source.{field}")
    for archive in recipe["source"].get("archives", []):
        _safe_path(archive, "source.archives")
    archive_entries = recipe["source"].get("archiveEntries", [])
    _unique(archive_entries, "path", "source.archiveEntries")
    for entry in archive_entries:
        _safe_path(entry["path"], "source.archiveEntries.path")
        _safe_path(entry["archive"], "source.archiveEntries.archive")
        _safe_path(entry["member"].replace("\\", "/"), "source.archiveEntries.member")
    for path in recipe["source"].get("xmlFiles", []):
        _safe_path(path, "source.xmlFiles")
    xml_files = [path.casefold() for path in recipe["source"].get("xmlFiles", [])]
    if len(xml_files) != len(set(xml_files)):
        raise RecipeError("source.xmlFiles: duplicate path")
    definitions = recipe["source"].get("definitionOverrides", [])
    _unique(definitions, "name", "source.definitionOverrides")
    for definition in definitions:
        _safe_path(definition["file"], "source.definitionOverrides.file")
    overrides = recipe["source"].get("textureOverrides", [])
    _unique(overrides, "source", "source.textureOverrides")
    for override in overrides:
        _safe_path(override["source"], "source.textureOverrides.source")
        _safe_path(override["path"], "source.textureOverrides.path")
    pieces = recipe["pieces"]
    _unique(pieces, "id", "pieces")
    piece_ids = {piece["id"] for piece in pieces}
    _piece_references(recipe, piece_ids)
    for piece in pieces:
        _safe_path(piece["alo"], f"pieces.{piece['id']}.alo")
    attachments = recipe["attachments"]
    _unique(attachments, "id", "attachments")
    attachment_ids = {attachment["id"] for attachment in attachments}
    for attachment in attachments:
        if "parentInstance" in attachment and attachment["parentInstance"] not in attachment_ids:
            raise RecipeError(f"attachment {attachment['id']}: unknown parentInstance")
        for key in ("piece", "parentPiece"):
            if attachment[key] not in piece_ids:
                raise RecipeError(f"attachment {attachment['id']}: unknown {key} {attachment[key]}")
        if attachment["piece"] == attachment["parentPiece"]:
            raise RecipeError(f"attachment {attachment['id']}: cannot parent a piece to itself")
    outputs = recipe["integration"]["outputs"]
    _unique(outputs, "role", "outputs")
    _unique(outputs, "path", "outputs")
    for output in outputs:
        _safe_path(output["path"], f"outputs.{output['role']}", asset=True)
        if recipe["operation"] == "update" and output["guid"] is None:
            raise RecipeError("update requires an existing GUID baseline for every output")
    output_guids = [output["guid"] for output in outputs if output["guid"] is not None]
    if len(output_guids) != len(set(output_guids)):
        raise RecipeError("outputs: duplicate GUID baseline")
    output_roles = {output["role"] for output in outputs}
    for key, value in recipe.get("registrations", {}).items():
        if key.endswith("Role") and value not in output_roles:
            raise RecipeError(f"registrations.{key}: unknown output role {value}")
    for binding in recipe["integration"]["bindings"]:
        if binding["component"]["role"] not in output_roles or ("target" in binding and binding["target"]["role"] not in output_roles):
            raise RecipeError("binding: unknown output role")
    generation = recipe["derivatives"].get("generation")
    if generation:
        roles = [generation["sourceRole"], generation["root"]["role"]]
        roles.extend(item["role"] for item in generation["renderers"])
        roles.extend(value for key, value in generation.items() if key in ("placementRole", "wreckRole", "iconRole"))
        roles.extend(item["outputRole"] for item in generation.get("wreckMaterials", []))
        if any(role not in output_roles for role in roles):
            raise RecipeError("derivatives.generation: unknown output role")
        _unique(generation.get("wreckMaterials", []), "sourceGuid", "wreck material mappings")
    for material in recipe["materials"]:
        _unique(material["selector"]["parameters"], "name", "material parameters")
        _unique(material["textures"], "semantic", "material textures")
        _unique(material["livery"]["parameters"], "name", "livery parameters")
        for texture in material["textures"]:
            _safe_path(texture["source"], "material texture source")
            if "targetGuid" in texture and texture["targetGuid"] not in output_guids:
                raise RecipeError("material texture targetGuid must identify a declared output")
    selectors = {}
    for material in recipe["materials"]:
        selector = material["selector"]
        signature = json.dumps({"piece": selector["piece"], "shader": selector["shader"],
                                "parameters": {row["name"]: row["value"] for row in selector["parameters"]}}, sort_keys=True)
        scope = (selector["meshIndex"], selector["slot"]) if "meshIndex" in selector else None
        scopes = selectors.setdefault(signature, [])
        if scope in scopes or (scopes and (scope is None or None in scopes)):
            raise RecipeError("materials: duplicate selector or overlapping scoped selector")
        scopes.append(scope)
    _unique(recipe["gameplay"]["values"], "name", "gameplay values")
    if recipe["gameplay"]["baselineGuid"] is None and not recipe["gameplay"]["values"]:
        raise RecipeError("gameplay requires an existing data baseline or explicit requested values")
    for piece in recipe["derivatives"]["wreckPieces"]:
        if piece not in piece_ids:
            raise RecipeError(f"wreckPieces: unknown piece {piece}")
    _unique(recipe["hardpoints"], "id", "hardpoints")
    _unique(recipe["hangar"], "id", "hangar")
    for hardpoint in recipe["hardpoints"]:
        if "component" in hardpoint and hardpoint["component"]["role"] not in output_roles:
            raise RecipeError(f"hardpoint {hardpoint['id']}: unknown component output role")
        yaw_range = hardpoint["yawRangeDegrees"]
        if yaw_range[0] > yaw_range[1]:
            raise RecipeError(f"hardpoint {hardpoint['id']}: reversed yawRangeDegrees")
        if hardpoint["kind"] == "weapon" and (hardpoint["profileGuid"] is None or "profileKey" not in hardpoint):
            raise RecipeError(f"hardpoint {hardpoint['id']}: weapon requires profileGuid and typed profileKey")
        anchor = hardpoint["anchor"]
        if "piece" in anchor and anchor["piece"] not in piece_ids:
            raise RecipeError(f"hardpoint {hardpoint['id']}: unknown piece {anchor['piece']}")
        if hardpoint["kind"] == "weapon" and not hardpoint["targetable"]:
            approved = any(exception["ruleId"] == "targetable-weapon" and f"hardpoint:{hardpoint['id']}" in exception["sourceIds"]
                           and exception.get("approval") for exception in recipe["exceptions"])
            if not approved:
                raise RecipeError(f"hardpoint {hardpoint['id']}: untargetable weapon requires an approved exception")
    _unique(recipe["decisions"], "key", "decisions")
    if any(decision["status"] == "unresolved" for decision in recipe["decisions"]):
        raise RecipeError("decisions: unresolved choices prevent execution")
    if recipe["kind"] == "ship":
        if sum(piece["purpose"] == "hull" for piece in pieces) != 1:
            raise RecipeError("ship requires exactly one hull piece")
        if "targetDiameter" in recipe["dimensions"]:
            raise RecipeError("ship dimensions forbid targetDiameter")
        if "referenceLevel" in recipe["dimensions"]:
            raise RecipeError("ship dimensions forbid referenceLevel")
    else:
        if "targetLength" in recipe["dimensions"]:
            raise RecipeError("station dimensions forbid targetLength")
        levels = recipe["station"]["levels"]
        numbers = [level["level"] for level in levels]
        if numbers != list(range(1, len(levels) + 1)):
            raise RecipeError("station levels must be ordered and contiguous from 1")
        if "referenceLevel" in recipe["dimensions"] and recipe["dimensions"]["referenceLevel"] not in numbers:
            raise RecipeError("station dimensions referenceLevel is outside declared levels")
        for reference in [recipe["station"].get("viewComponent")]:
            if reference and reference["role"] not in output_roles:
                raise RecipeError("station viewComponent: unknown output role")
        _unique(levels, "piece", "station levels")
        for level in levels:
            references = [level.get(key) for key in ("component", "alignmentPoint", "launchExit")]
            references.extend(level.get("hullRenderers", []))
            if any(reference and reference["role"] not in output_roles for reference in references):
                raise RecipeError("station level: unknown object output role")
            if level["piece"] not in piece_ids:
                raise RecipeError(f"station level {level['level']}: unknown hull piece")
            if next(piece for piece in pieces if piece["id"] == level["piece"])["purpose"] != "level":
                raise RecipeError("station hull pieces must have purpose 'level'")
        hardpoint_ids = {hardpoint["id"] for hardpoint in recipe["hardpoints"]}
        _unique(recipe["station"]["mounts"], "id", "station mounts")
        for mount in recipe["station"]["mounts"]:
            if mount["hardpoint"] not in hardpoint_ids or mount["unlockLevel"] not in numbers:
                raise RecipeError(f"station mount {mount['id']}: invalid hardpoint or unlockLevel")
            if any(anchor["level"] not in numbers or anchor["piece"] not in piece_ids for anchor in mount["anchors"]):
                raise RecipeError(f"station mount {mount['id']}: invalid level anchor")
            for anchor in mount["anchors"]:
                if any(anchor.get(key) and anchor[key]["role"] not in output_roles for key in ("point", "art")):
                    raise RecipeError(f"station mount {mount['id']}: unknown object output role")


def _object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise RecipeError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def load_recipe(path):
    try:
        recipe = json.loads(Path(path).read_text(encoding="utf-8-sig"), object_pairs_hook=_object)
    except (OSError, json.JSONDecodeError) as error:
        raise RecipeError(f"cannot read recipe {path}: {error}") from error
    validate_recipe(recipe)
    return recipe
