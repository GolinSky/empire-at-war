"""Regression coverage for sampled squadron population (not pooled starts)."""
import csv
import pathlib
import subprocess
import sys
import tempfile
import unittest


class SquadronSummaryTests(unittest.TestCase):
    def summarize(self, events):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            (root / "editor_telemetry").mkdir()
            with (root / "editor_telemetry/Loading_squadrons.csv").open("w", newline="") as stream:
                writer = csv.DictWriter(stream, fieldnames=[
                    "time", "kind", "player", "entityId", "unit", "order", "x", "z", "targetDistance"])
                writer.writeheader()
                for time, kind, player, entity in events:
                    writer.writerow(dict(time=time, kind=kind, player=player, entityId=entity,
                                         unit="Vulture", order="Guard", x=0, z=0, targetDistance=-1))
            subprocess.run([sys.executable, str(pathlib.Path(__file__).with_name("squadron_summary.py")),
                            str(root)], check=True, capture_output=True, text=True)
            return (root / "squadron_summary.txt").read_text(encoding="utf-8")

    def test_precreated_and_launch_only_entities_do_not_count_as_sampled_population(self):
        summary = self.summarize([
            (0, "start", "Player0", 1), (0, "start", "Player0", 2),
            (1, "launch", "Player0", 2), (3, "sample", "Player0", 1),
            (4, "sample", "Player0", 1), (61, "sample", "Player1", 3)])
        self.assertIn("minute 0: sampled squadrons 1 by player {'Player0': 1}", summary)
        self.assertIn("minute 1: sampled squadrons 1 by player {'Player1': 1}", summary)
        self.assertIn("not an instantaneous peak", summary)

    def test_starts_without_samples_report_population_unavailable(self):
        summary = self.summarize([(0, "start", "Player0", 1), (1, "destroyed", "Player0", 1)])
        self.assertIn("live population unavailable: no sample rows", summary)
        self.assertIn("median lifetime 0s", summary)


if __name__ == "__main__":
    unittest.main()
