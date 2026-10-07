"""Reject broad hull tint by comparing owned renders with the unowned reference."""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/EmpireStationImport'


def Main():
    results = []
    for level in range(1, 6):
        reference = Image.open(TASK / f'Previews/Level{level}Team-1.png').convert('RGB')
        pixels = reference.get_flattened_data()
        background = reference.getpixel((0, 0))
        visible = [index for index, pixel in enumerate(pixels) if pixel != background]
        for team in range(8):
            colored = Image.open(TASK / f'Previews/Level{level}Team{team}.png').convert('RGB').get_flattened_data()
            changed = sum(max(abs(a - b) for a, b in zip(pixels[index], colored[index])) > 4 for index in visible)
            fraction = changed / len(visible)
            assert 0 < fraction < .1, (level, team, fraction)
            results.append(dict(level=level, team=team, changedFraction=fraction))
    (TASK / 'VerifiedTeamColors.json').write_text(json.dumps(results, indent=2))
    print('All 40 owned renders retain at least 90% of the unowned surface pixels.')
    print('Changed pixel fraction:', min(row['changedFraction'] for row in results), 'to', max(row['changedFraction'] for row in results))


if __name__ == '__main__':
    Main()
