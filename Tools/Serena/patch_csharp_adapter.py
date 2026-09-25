"""Patch Serena 1.7.0's C# adapter for this Unity checkout.

Run this script with the Python executable from the dedicated Serena virtual
environment. It deliberately refuses to modify a uv cache or a different
Serena release.
"""

from __future__ import annotations

import hashlib
import importlib.metadata
import os
import sys
import sysconfig
import tempfile
from pathlib import Path


EXPECTED_SERENA_VERSION = "1.7.0"
ADAPTER_RELATIVE_PATH = Path("solidlsp") / "language_servers" / "csharp_language_server.py"

ORIGINAL_METHOD = '''    def _open_solution_and_projects(self) -> None:
        """
        Open solution and project files using notifications.
        """
        # Find solution file (.sln or .slnx)
        solution_file = None
        for filename in breadth_first_file_scan(self.repository_root_path):
            if filename.endswith((".sln", ".slnx")):
                solution_file = filename
                break

        # Send solution/open notification if solution file found
        if solution_file:
            solution_uri = PathUtils.path_to_uri(solution_file)
            self.server.notify.send_notification("solution/open", {"solution": solution_uri})
            log.debug(f"Opened solution file: {solution_file}")

        # Find and open project files
        project_files = []
        for filename in breadth_first_file_scan(self.repository_root_path):
            if filename.endswith(".csproj"):
                project_files.append(filename)

        # Send project/open notifications for each project file
        if project_files:
            project_uris = [PathUtils.path_to_uri(project_file) for project_file in project_files]
            self.server.notify.send_notification("project/open", {"projects": project_uris})
            log.debug(f"Opened project files: {project_files}")
'''

PATCHED_METHOD = '''    def _open_solution_and_projects(self) -> None:
        """Open one root solution, or root Unity project files as a fallback."""
        repository_root = Path(self.repository_root_path)
        preferred_solution = repository_root / "empire-at-war.sln"
        solution_candidates = sorted(
            (
                path
                for extension in ("*.sln", "*.slnx")
                for path in repository_root.glob(extension)
                if path.is_file()
            ),
            key=lambda path: path.name.casefold(),
        )
        solution_file = (
            preferred_solution
            if preferred_solution.is_file()
            else next(iter(solution_candidates), None)
        )

        if solution_file:
            solution_uri = PathUtils.path_to_uri(str(solution_file))
            self.server.notify.send_notification("solution/open", {"solution": solution_uri})
            log.debug(f"Opened root solution file: {solution_file}")
            return

        project_files = sorted(
            (path for path in repository_root.glob("*.csproj") if path.is_file()),
            key=lambda path: path.name.casefold(),
        )
        if project_files:
            project_uris = [PathUtils.path_to_uri(str(project_file)) for project_file in project_files]
            self.server.notify.send_notification("project/open", {"projects": project_uris})
            log.debug(f"Opened root project files: {project_files}")
'''

EXPECTED_ORIGINAL_METHOD_SHA256 = "b5b11449057b4eda0f30541707bb5557668355c592147ee41f2857b44cf94f01"


class PatchGuardError(RuntimeError):
    """The target does not match the Serena build this patch was reviewed for."""


def _sha256(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def _is_within(path: Path, parent: Path) -> bool:
    try:
        path.resolve().relative_to(parent.resolve())
    except ValueError:
        return False
    return True


def locate_adapter(target_prefix: Path) -> Path:
    """Return the adapter in this interpreter's environment, never a uv cache."""
    purelib = Path(sysconfig.get_paths()["purelib"]).resolve()
    adapter_path = (purelib / ADAPTER_RELATIVE_PATH).resolve()
    if not adapter_path.is_file():
        raise PatchGuardError(f"C# adapter was not found under this environment: {adapter_path}")
    if not _is_within(adapter_path, target_prefix):
        raise PatchGuardError(f"Refusing to patch an adapter outside sys.prefix: {adapter_path}")
    if any(part.casefold() == "cache" for part in adapter_path.parts) and "uv" in {
        part.casefold() for part in adapter_path.parts
    }:
        raise PatchGuardError(f"Refusing to patch a uv cache: {adapter_path}")
    return adapter_path


def patch_adapter(adapter_path: Path, serena_version: str) -> str:
    """Atomically apply the reviewed patch, returning ``patched`` or ``unchanged``."""
    if serena_version != EXPECTED_SERENA_VERSION:
        raise PatchGuardError(
            f"Expected serena-agent {EXPECTED_SERENA_VERSION}, found {serena_version}."
        )

    source = adapter_path.read_text(encoding="utf-8")
    if PATCHED_METHOD in source:
        if ORIGINAL_METHOD in source:
            raise PatchGuardError("Adapter contains both original and patched method blocks.")
        return "unchanged"

    original_occurrences = source.count(ORIGINAL_METHOD)
    if original_occurrences != 1:
        raise PatchGuardError(
            "The reviewed 1.7.0 method block was not found exactly once; refusing source drift."
        )
    if _sha256(ORIGINAL_METHOD) != EXPECTED_ORIGINAL_METHOD_SHA256:
        raise PatchGuardError("Built-in source guard is inconsistent; refusing to patch.")

    patched_source = source.replace(ORIGINAL_METHOD, PATCHED_METHOD, 1)
    with tempfile.NamedTemporaryFile(
        mode="w", encoding="utf-8", newline="\n", dir=adapter_path.parent, delete=False
    ) as temporary_file:
        temporary_path = Path(temporary_file.name)
        temporary_file.write(patched_source)

    try:
        os.replace(temporary_path, adapter_path)
    except BaseException:
        temporary_path.unlink(missing_ok=True)
        raise
    return "patched"


def main() -> int:
    target_prefix = Path(sys.prefix).resolve()
    if not (target_prefix / "pyvenv.cfg").is_file():
        raise PatchGuardError(
            "Run this with the Python executable from the dedicated Serena virtual environment."
        )
    adapter_path = locate_adapter(target_prefix)
    serena_version = importlib.metadata.version("serena-agent")
    result = patch_adapter(adapter_path, serena_version)
    print(f"{result}: {adapter_path}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except PatchGuardError as error:
        print(f"patch guard failed: {error}", file=sys.stderr)
        raise SystemExit(2) from error
