"""Offline parser regressions; execution deferred with the Unity fixtures."""
import json
import tempfile
import unittest
from pathlib import Path
from battle_audit import ALLOCATIONS_TEMPLATE, read_csv, value
from battle_attribution import game_caller, hitch_period, merge_allocations, memory_diff, write_report, archive_run


class BattleAttributionTests(unittest.TestCase):
    def test_fixture_preserves_missing_markers_and_hitch_episode_period(self):
        rows = read_csv(Path(__file__).with_name('fixtures') / 'hitch_synthetic.csv')
        self.assertIsNone(value(rows[0], 'editor_loop_ms'))
        result = hitch_period(rows, value)
        self.assertEqual(3, result['frames_over_200ms'])
        self.assertEqual(2, result['episodes'])
        self.assertEqual([35.0], result['gaps_seconds'])

    def test_hitch_period_ignores_instrument_busy_frames(self):
        rows = [{'frame': '1', 'seconds': '0', 'frame_duration_ms': '300', 'instrument_busy': '0'},
                {'frame': '50', 'seconds': '35', 'frame_duration_ms': '4000', 'instrument_busy': '1'}]
        result = hitch_period(rows, value)
        self.assertEqual(1, result['frames_over_200ms'])
        self.assertEqual(1, result['episodes'])

    def test_game_caller_skips_framework_allocator(self):
        self.assertEqual('EmpireAtWar.Ship.Tick:42', game_caller([
            'System.String.Concat:0', 'EmpireAtWar.Ship.Tick:42']))
        self.assertEqual('<no resolved game caller>', game_caller(['System.String.Concat:0']))

    def test_merge_uses_all_sampled_frames_including_zero_allocation_frames(self):
        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder) / 'allocations.json'
            source.write_text(json.dumps({'frameCount': 4, 'stacks': [
                {'methods': ['System.String.Concat', 'EmpireAtWar.Ship.Tick'], 'bytes': 400, 'calls': 2},
                {'methods': [], 'bytes': 80, 'calls': 1}]}), encoding='utf-8')
            result = merge_allocations(Path(folder) / 'allocation-ranking.json', [source, source])
            self.assertEqual(4, result['sampled_frames'])
            self.assertEqual(100, result['ranking'][0]['bytes_per_frame'])
            self.assertEqual(20, result['ranking'][1]['bytes_per_frame'])
            write_report(folder)
            self.assertIn('No hitch CSV', (Path(folder) / 'analysis.md').read_text())

    def test_legacy_truncated_top_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder) / 'old.json'
            source.write_text('{"frameCount": 40, "top": []}', encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'exhaustive stacks'):
                merge_allocations(Path(folder) / 'out.json', [source])

    def test_memory_diff_keeps_owners_and_freed_types(self):
        with tempfile.TemporaryDirectory() as folder:
            before, after = Path(folder) / 'before.json', Path(folder) / 'after.json'
            before.write_text(json.dumps({'types': [
                {'type': 'Pool', 'owner': 'A', 'count': 1, 'bytes': 100},
                {'type': 'Pool', 'owner': 'B', 'count': 1, 'bytes': 300}]}))
            after.write_text(json.dumps({'types': [
                {'type': 'Pool', 'owner': 'A', 'count': 2, 'bytes': 250}]}))
            result = memory_diff(before, after, Path(folder) / 'diff.json')
            self.assertEqual([150, -300], [row['delta_bytes'] for row in result['types']])

    def test_decoder_keeps_deep_stacks_and_real_frame_denominator(self):
        source = ALLOCATIONS_TEMPLATE.format(first=10, last=69, result='out.json')
        self.assertIn('resolved.Count < 14', source)
        self.assertIn('frameCount = validFrames.Count', source)
        self.assertIn('stackTotal[0] += size', source)

    def test_archive_filters_frames_and_excludes_unfinished_csv_tail(self):
        with tempfile.TemporaryDirectory() as folder:
            source, output = Path(folder) / 'source', Path(folder) / 'run'
            (source / 'Hitches').mkdir(parents=True)
            output.mkdir()
            hitch = source / 'Hitches' / 'hitch_current.csv'
            hitch.write_text('frame,is_hitch\n10,1\n11,0\n12,1', encoding='utf-8')
            archive_run(source, output, 11, 12, '2026-01-01T00:00:00Z')
            self.assertEqual([{'frame': '11', 'is_hitch': '0'}], read_csv(output / hitch.name))
            self.assertTrue((output / 'archive.json').exists())


if __name__ == '__main__':
    unittest.main()
