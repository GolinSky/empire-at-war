from __future__ import annotations

import ast
import importlib.util
import tempfile
import textwrap
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).parents[1] / "patch_csharp_adapter.py"
SCRIPT_SPEC = importlib.util.spec_from_file_location("patch_csharp_adapter", SCRIPT_PATH)
assert SCRIPT_SPEC is not None
assert SCRIPT_SPEC.loader is not None
patch_csharp_adapter = importlib.util.module_from_spec(SCRIPT_SPEC)
SCRIPT_SPEC.loader.exec_module(patch_csharp_adapter)


class PathUtilsStub:
    @staticmethod
    def path_to_uri(path: str) -> str:
        return Path(path).as_uri()


class NotificationRecorder:
    def __init__(self) -> None:
        self.notifications: list[tuple[str, dict[str, object]]] = []

    def send_notification(self, method: str, parameters: dict[str, object]) -> None:
        self.notifications.append((method, parameters))


class ServerStub:
    def __init__(self) -> None:
        self.notify = NotificationRecorder()


def patched_method():
    """Compile only the patched method with stubs; importing Serena would start no test value."""
    parsed = ast.parse(textwrap.dedent(patch_csharp_adapter.PATCHED_METHOD))
    method_node = parsed.body[0]
    assert isinstance(method_node, ast.FunctionDef)
    module = ast.Module(body=[method_node], type_ignores=[])
    ast.fix_missing_locations(module)
    namespace = {"Path": Path, "PathUtils": PathUtilsStub, "log": type("Log", (), {"debug": staticmethod(lambda _: None)})}
    exec(compile(module, "<patched-method>", "exec"), namespace)
    return namespace["_open_solution_and_projects"]


class AdapterMethodTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory(prefix="serena-csharp-adapter-")
        self.root = Path(self.temporary_directory.name)
        self.method = patched_method()
        self.server = ServerStub()
        self.subject = type("Subject", (), {"repository_root_path": str(self.root), "server": self.server})()

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def invoke(self) -> list[tuple[str, dict[str, object]]]:
        self.method(self.subject)
        return self.server.notify.notifications

    def test_preferred_root_solution_opens_once_and_skips_projects(self) -> None:
        (self.root / "empire-at-war.sln").write_text("", encoding="utf-8")
        (self.root / "Other.sln").write_text("", encoding="utf-8")
        (self.root / "Assembly-CSharp.csproj").write_text("", encoding="utf-8")
        notifications = self.invoke()
        self.assertEqual(1, len(notifications))
        self.assertEqual("solution/open", notifications[0][0])
        self.assertEqual((self.root / "empire-at-war.sln").as_uri(), notifications[0][1]["solution"])

    def test_root_projects_fallback_ignores_nested_library_and_obj_projects(self) -> None:
        (self.root / "Assembly-CSharp.csproj").write_text("", encoding="utf-8")
        (self.root / "Generated.csproj").write_text("", encoding="utf-8")
        (self.root / "Library").mkdir()
        (self.root / "Library" / "Ignored.csproj").write_text("", encoding="utf-8")
        (self.root / "obj").mkdir()
        (self.root / "obj" / "Ignored.csproj").write_text("", encoding="utf-8")
        notifications = self.invoke()
        self.assertEqual(["project/open"], [method for method, _ in notifications])
        projects = notifications[0][1]["projects"]
        self.assertEqual(
            [(self.root / "Assembly-CSharp.csproj").as_uri(), (self.root / "Generated.csproj").as_uri()],
            projects,
        )

    def test_root_untracked_unity_generated_projects_are_not_filtered_by_gitignore(self) -> None:
        (self.root / ".gitignore").write_text("*.csproj\n", encoding="utf-8")
        (self.root / "Assembly-CSharp-Editor.csproj").write_text("", encoding="utf-8")
        notifications = self.invoke()
        self.assertEqual("project/open", notifications[0][0])
        self.assertEqual([(self.root / "Assembly-CSharp-Editor.csproj").as_uri()], notifications[0][1]["projects"])


class PatchGuardTests(unittest.TestCase):
    def test_patch_is_idempotent_and_rejects_drift(self) -> None:
        with tempfile.TemporaryDirectory(prefix="serena-csharp-patch-") as temporary_directory:
            adapter_path = Path(temporary_directory) / "csharp_language_server.py"
            adapter_path.write_text("before\n" + patch_csharp_adapter.ORIGINAL_METHOD + "after\n", encoding="utf-8")
            self.assertEqual("patched", patch_csharp_adapter.patch_adapter(adapter_path, "1.7.0"))
            self.assertEqual("unchanged", patch_csharp_adapter.patch_adapter(adapter_path, "1.7.0"))
            adapter_path.write_text("unreviewed source", encoding="utf-8")
            with self.assertRaises(patch_csharp_adapter.PatchGuardError):
                patch_csharp_adapter.patch_adapter(adapter_path, "1.7.0")


if __name__ == "__main__":
    unittest.main()
