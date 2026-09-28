"""Integration checks in a disposable non-Unity project; no game files edited."""
import asyncio
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

REPO = Path(__file__).resolve().parents[2]
RUN = REPO / "Tools/Graphify/run.py"
FIXTURE = Path(tempfile.mkdtemp(prefix="graphify-flow-"))
SOURCE = FIXTURE / "Assets/Scripts/Probe.cs"
GRAPH = FIXTURE / "graphify-out/graph.json"
ENV = {**os.environ, "GRAPHIFY_MAX_WORKERS": "2", "GRAPHIFY_QUERY_LOG_DISABLE": "1"}
RESULTS = []


def record(name):
    RESULTS.append(name)
    print("PASS " + name, flush=True)


def labels():
    if not GRAPH.exists():
        return set()
    return {n.get("label", "") for n in json.loads(GRAPH.read_text(encoding="utf-8"))["nodes"]}


def wait_for(predicate, name):
    deadline = time.monotonic() + 55
    while time.monotonic() < deadline:
        if predicate():
            record(name)
            return
        time.sleep(0.5)
    raise AssertionError(name + " timed out")


def start_watcher():
    return subprocess.Popen([sys.executable, str(RUN), "watch", "--root", str(FIXTURE)],
                            env=ENV, creationflags=subprocess.CREATE_NO_WINDOW)


async def query(name):
    params = StdioServerParameters(command=sys.executable,
                                  args=[str(RUN), "mcp", "--root", str(FIXTURE)], env=ENV)
    async with stdio_client(params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            return await session.call_tool("query_graph", {"question": name, "token_budget": 500})


async def check_query():
    SOURCE.write_text("public class ImmediateQueryProbe {}\n", encoding="utf-8")
    results = await asyncio.gather(query("ImmediateQueryProbe"), query("ImmediateQueryProbe"))
    assert all(not r.is_error and "ImmediateQueryProbe" in str(r.content) for r in results), results
    record("two simultaneous MCP queries catch up automatically under the Windows lock")
    for index in range(70):
        (SOURCE.parent / f"Parallel{index}.cs").write_text(f"public class ParallelProbe{index} {{}}\n", encoding="utf-8")
    result = await query("ParallelProbe69")
    assert not result.is_error and "ParallelProbe69" in str(result.content), result
    record("parallel AST extraction preserves the MCP protocol during a bulk code update")
    saved = GRAPH.read_bytes()
    GRAPH.write_text("invalid graph JSON", encoding="utf-8")
    result = await query("ImmediateQueryProbe")
    assert result.is_error and "freshness check failed" in str(result.content), result
    record("failed rebuild returns an MCP error instead of old graph results")
    GRAPH.write_bytes(saved)


def main():
    SOURCE.parent.mkdir(parents=True)
    (FIXTURE / "Tools/Graphify").mkdir(parents=True)
    shutil.copyfile(REPO / "Tools/Graphify/watch.json", FIXTURE / "Tools/Graphify/watch.json")
    (FIXTURE / ".graphifyignore").write_text("*\n!Assets/\n!Assets/Scripts/\n!Assets/Scripts/**\n", encoding="utf-8")
    subprocess.run(["git", "init", "--quiet", str(FIXTURE)], check=True)
    SOURCE.write_text("public class BeforeGraphifyProbe {}\n", encoding="utf-8")
    watcher = start_watcher()
    try:
        wait_for(lambda: "BeforeGraphifyProbe" in labels(), "startup builds an absent graph")
        duplicate = start_watcher()
        assert duplicate.wait(timeout=10) == 0
        record("second watcher exits without a second writer")
        old = SOURCE.stat()
        SOURCE.write_text("public class AfterxGraphifyProbe {}\n", encoding="utf-8")
        os.utime(SOURCE, ns=(old.st_atime_ns, old.st_mtime_ns))
        wait_for(lambda: "AfterxGraphifyProbe" in labels() and "BeforeGraphifyProbe" not in labels(),
                 "same-size edit with original timestamp refreshes automatically")
        added = SOURCE.parent / "Added.cs"
        added.write_text("public class AddedGraphifyProbe {}\n", encoding="utf-8")
        wait_for(lambda: "AddedGraphifyProbe" in labels(), "new source appears automatically")
        renamed = SOURCE.parent / "Renamed.cs"
        added.rename(renamed)
        wait_for(lambda: any(n.get("label") == "AddedGraphifyProbe" and n.get("source_file", "").endswith("Renamed.cs")
                            for n in json.loads(GRAPH.read_text(encoding="utf-8"))["nodes"]),
                 "file rename replaces the old source path")
        renamed.unlink()
        wait_for(lambda: "AddedGraphifyProbe" not in labels(), "deleted source disappears automatically")
    finally:
        watcher.terminate()
        watcher.wait(timeout=10)
    SOURCE.write_text("public class OfflineGraphifyProbe {}\n", encoding="utf-8")
    watcher = start_watcher()
    try:
        wait_for(lambda: "OfflineGraphifyProbe" in labels(), "restart catches edits made while watcher was stopped")
    finally:
        watcher.terminate()
        watcher.wait(timeout=10)
    asyncio.run(check_query())
    report = {"passed": RESULTS, "fixture": str(FIXTURE), "time": time.time()}
    (FIXTURE / "verification.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report), flush=True)


if __name__ == "__main__":
    main()
