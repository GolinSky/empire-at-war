"""Freshness and Windows locking around Graphify 0.9.54's update engine."""

import contextlib
import hashlib
import json
import msvcrt
import os
import sys
import time
from pathlib import Path

import graphify.cache as graphify_cache
from graphify.detect import CODE_EXTENSIONS
from graphify.watch import _rebuild_code

# Restores can preserve BOTH size and mtime. Keep content-addressed AST caching,
# but always read the bytes used to choose its key (also in spawned workers).
graphify_cache._stat_sig_fresh = lambda *_args, **_kwargs: False


@contextlib.contextmanager
def file_lock(path: Path, timeout: float = 120):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a+b") as stream:
        if path.stat().st_size == 0:
            stream.write(b"0")
            stream.flush()
        deadline = time.monotonic() + timeout
        while True:
            stream.seek(0)
            try:
                msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
                break
            except OSError:
                if time.monotonic() >= deadline:
                    raise TimeoutError(f"Graphify lock is busy: {path}")
                time.sleep(0.1)
        try:
            yield
        finally:
            stream.seek(0)
            msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)


def write_json(path: Path, value: dict):
    temporary = path.with_suffix(".tmp")
    temporary.write_text(json.dumps(value, indent=2), encoding="utf-8")
    temporary.replace(path)


class Freshness:
    """Monitor the project's declared code roots, including unsaved-to-Git edits."""

    def __init__(self, root: Path):
        self.root = root.resolve()
        self.out = self.root / "graphify-out"
        self.state = self.out / "service"
        self.state.mkdir(parents=True, exist_ok=True)
        self.graph = self.out / "graph.json"
        self.receipt = self.state / "indexed.json"

    def snapshot(self):
        config_path = self.root / "Tools/Graphify/watch.json"
        config = json.loads(config_path.read_text(encoding="utf-8"))
        paths = {config_path}
        for name in config["roots"]:
            directory = (self.root / name).resolve()
            if not directory.is_relative_to(self.root) or not directory.is_dir():
                raise ValueError(f"Invalid Graphify watch root: {directory}")
            # Hash source contents, not mtimes: checkout/restore can preserve timestamps.
            for folder, dirs, names in os.walk(directory, onerror=self.scan_error):
                dirs[:] = [d for d in dirs if not (Path(folder) / d).is_symlink()]
                for name in names:
                    path = Path(folder) / name
                    if path.suffix.lower() in CODE_EXTENSIONS or name in (".gitignore", ".graphifyignore"):
                        if path.is_symlink():
                            raise ValueError(f"Symlinked source needs an explicit scan policy: {path}")
                        paths.add(path)
        for parent in (self.root, *self.root.parents):
            for name in (".gitignore", ".graphifyignore"):
                path = parent / name
                if path.is_file():
                    paths.add(path)
        for path in (self.root / ".git/info/exclude", self.out / ".graphify_build.json"):
            if path.is_file():
                paths.add(path)
        hashes = {str(p): hashlib.md5(p.read_bytes()).hexdigest() for p in sorted(paths)}
        return hashes

    @staticmethod
    def scan_error(error):
        raise error

    def graph_stamp(self):
        if not self.graph.exists():
            return None
        stat = self.graph.stat()
        return [stat.st_mtime_ns, stat.st_size]

    def is_current(self, sources):
        if not self.receipt.exists():
            return False
        receipt = json.loads(self.receipt.read_text(encoding="utf-8"))
        return receipt.get("schema") == 2 and receipt["sources"] == sources and receipt["graph"] == self.graph_stamp()

    def ensure(self):
        with file_lock(self.state / "update.lock"):
            for attempt in range(3):
                before = self.snapshot()
                if self.is_current(before):
                    return False
                write_json(self.state / "status.json", {"state": "updating", "pid": os.getpid(), "time": time.time()})
                # Graphify prints progress to stdout; preserve MCP's JSON-only stdout.
                with contextlib.redirect_stdout(sys.stderr):
                    if not _rebuild_code(self.root, acquire_lock=False):
                        raise RuntimeError("Graphify rebuild failed; refusing to serve an older graph.")
                after = self.snapshot()
                if before != after:
                    continue
                # Verify Graphify stamped each included code source successfully.
                from graphify.detect import ignored_predicate, load_manifest
                from graphify.watch import _read_build_excludes, _read_build_gitignore
                ignored = ignored_predicate(self.root, extra_excludes=_read_build_excludes(self.out),
                                            gitignore=_read_build_gitignore(self.out))
                manifest = load_manifest(str(self.out / "manifest.json"), root=self.root)
                for name, digest in after.items():
                    path = Path(name)
                    if path.suffix.lower() in CODE_EXTENSIONS and not ignored(path):
                        if manifest.get(name, {}).get("ast_hash") != digest:
                            raise RuntimeError(f"Graphify did not successfully index {path}; refusing stale results.")
                write_json(self.receipt, {"schema": 2, "sources": after, "graph": self.graph_stamp(), "indexed_at": time.time()})
                write_json(self.state / "status.json", {"state": "current", "pid": os.getpid(), "time": time.time()})
                return True
            raise RuntimeError("Code kept changing during indexing; retry after the current write completes.")
