"""Automatic updates and freshness-checked Graphify MCP, without Unity access."""

import argparse
import contextlib
import importlib.metadata
import json
import logging
from logging.handlers import RotatingFileHandler
import os
from pathlib import Path
import subprocess
import sys
import time

from freshness import Freshness, file_lock, write_json


class NoWindowPopen(subprocess.Popen):
    # The watcher runs under pythonw, which has no console, so each console child
    # (for example Graphify's `git ls-files`) would otherwise open a visible window.
    def __init__(self, *args, creationflags=0, **kwargs):
        super().__init__(*args, creationflags=creationflags | subprocess.CREATE_NO_WINDOW, **kwargs)


def watch(runtime):
    if os.name == "nt":
        subprocess.Popen = NoWindowPopen
    try:
        with file_lock(runtime.state / "watcher.lock", timeout=0):
            handler = RotatingFileHandler(runtime.state / "watcher.log", maxBytes=2_000_000, backupCount=2, encoding="utf-8")
            handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(message)s"))
            logger = logging.getLogger("graphify-service")
            logger.setLevel(logging.INFO)
            logger.addHandler(handler)
            logger.info("Watcher started: pid=%s root=%s", os.getpid(), runtime.root)
            previous = None
            while True:
                try:
                    sources = runtime.snapshot()
                    # First pass catches up immediately; subsequent passes debounce saves.
                    if (previous is None or sources == previous) and not runtime.is_current(sources):
                        with (runtime.state / "update.log").open("w", encoding="utf-8") as log:
                            with contextlib.redirect_stdout(log), contextlib.redirect_stderr(log):
                                updated = runtime.ensure()
                        if updated:
                            logger.info("Graph updated successfully")
                    previous = sources
                except Exception:
                    logger.exception("Automatic update failed; retrying on the next scan")
                    write_json(runtime.state / "status.json", {"state": "error", "pid": os.getpid(), "time": time.time()})
                    previous = None
                time.sleep(3)
    except TimeoutError:
        return  # Another watcher already owns this checkout.


def serve(runtime):
    import graphify.serve as server_module

    original_load = server_module._GraphContextCache.load
    original_build = server_module._build_server
    ready = False

    def load(cache, resolved_path, *, pinned=False):
        if ready:
            if Path(resolved_path).resolve() != runtime.graph:
                raise server_module.ToolError("This Graphify server belongs to one checkout; use that project's server.")
            try:
                runtime.ensure()
            except Exception as error:
                raise server_module.ToolError(f"Graphify freshness check failed: {error}") from error
        return original_load(cache, resolved_path, pinned=pinned)

    def build(path):
        nonlocal ready
        server = original_build(path)
        # Keep MCP initialize/list_tools fast; refresh immediately before graph access.
        ready = True
        return server

    server_module._GraphContextCache.load = load
    server_module._build_server = build
    server_module.serve(str(runtime.graph))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["watch", "mcp", "update", "status"])
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    if importlib.metadata.version("graphifyy") != "0.9.54":
        raise RuntimeError("This integration requires graphifyy==0.9.54; review it before upgrading.")
    args.root = args.root.resolve()
    os.chdir(args.root)
    os.environ.setdefault("GRAPHIFY_MAX_WORKERS", "2")
    os.environ.setdefault("GRAPHIFY_QUERY_LOG_DISABLE", "1")
    runtime = Freshness(args.root)
    if args.mode == "watch":
        watch(runtime)
    elif args.mode == "mcp":
        serve(runtime)
    elif args.mode == "update":
        print(json.dumps({"updated": runtime.ensure(), "current": True}))
    else:
        print(json.dumps({"current": runtime.is_current(runtime.snapshot()), "graph": str(runtime.graph)}))


if __name__ == "__main__":
    main()
