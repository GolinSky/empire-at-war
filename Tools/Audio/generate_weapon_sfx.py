"""Generate original, deterministic mono weapon cues with Python's standard library.

Run from any directory: python Tools/Audio/generate_weapon_sfx.py
No recordings, third-party samples, or network services are used.
"""

import array
import math
from pathlib import Path
import random
import sys
import wave


SAMPLE_RATE = 44100
OUTPUT = Path(__file__).resolve().parents[2] / "Assets/Audio/SFX/Weapons"
TAU = math.tau


def pulse(duration, start_hz, end_hz, decay, seed, noise_level=0.12, weight=0.0):
    rng = random.Random(seed)
    result = []
    phase = 0.0
    low_noise = 0.0
    for i in range(round(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        frequency = end_hz + (start_hz - end_hz) * math.exp(-t * 30.0)
        phase += TAU * frequency / SAMPLE_RATE
        low_noise += 0.32 * (rng.uniform(-1.0, 1.0) - low_noise)
        envelope = (1.0 - math.exp(-t * 1200.0)) * math.exp(-t * decay)
        tail = min(1.0, (duration - t) / 0.025)
        carrier = math.sin(phase) + 0.24 * math.sin(phase * 2.01)
        ring = 0.1 * math.sin(phase * 4.37) * math.exp(-t * 35.0)
        body = weight * math.sin(TAU * (80.0 * t + 2.0 * (1.0 - math.exp(-t * 25.0))))
        signal = carrier * 0.55 + ring + body + low_noise * noise_level
        result.append(math.tanh(signal * 1.3) * envelope * tail)
    return result


def loop_salvo(period, interval, start_hz, end_hz, seed, noise_level=0.12):
    count = round(period * SAMPLE_RATE)
    result = [0.0] * count
    for shot in range(round(period / interval)):
        variation = 1.0 + (shot % 3 - 1) * 0.025
        signal = pulse(0.19, start_hz * variation, end_hz * variation,
                       24.0, seed + shot, noise_level)
        offset = round(shot * interval * SAMPLE_RATE)
        # Wrap tails into the head so sample boundaries retain continuous energy.
        for i, value in enumerate(signal):
            result[(offset + i) % count] += value * (0.9 + 0.05 * (shot % 2))
    return result


def launcher(duration, seed, heavy):
    rng = random.Random(seed)
    result = pulse(duration, 210.0 if heavy else 400.0, 65.0, 25.0, seed, 0.3, 0.6)
    low_noise = 0.0
    slow_noise = 0.0
    for i in range(len(result)):
        t = i / SAMPLE_RATE
        low_noise += 0.14 * (rng.uniform(-1.0, 1.0) - low_noise)
        slow_noise += 0.014 * (low_noise - slow_noise)
        envelope = (1.0 - math.exp(-t * 90.0)) * math.exp(-t * (9.0 if heavy else 15.0))
        envelope *= min(1.0, (duration - t) / 0.045)
        turbine = math.sin(TAU * (440.0 * t + 950.0 * t * t)) * 0.035
        result[i] += ((low_noise - slow_noise) * 2.4 + turbine) * envelope
    return result


def beam():
    duration = 0.8  # Matches BeamShot's growth + hold time.
    rng = random.Random(91)
    result = []
    low_noise = 0.0
    for i in range(round(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        envelope = min(1.0, t / 0.025, (duration - t) / 0.12)
        low_noise += 0.12 * (rng.uniform(-1.0, 1.0) - low_noise)
        phase = TAU * 155.0 * t + 1.4 * math.sin(TAU * 47.0 * t)
        signal = 0.4 * math.sin(phase) + 0.18 * math.sin(phase * 2.0)
        signal += low_noise * 0.4 + math.sin(TAU * 1600.0 * t) * math.exp(-t * 40.0) * 0.15
        result.append(signal * envelope * (0.85 + 0.15 * math.sin(TAU * 9.0 * t)))
    return result


def write_clip(name, samples, peak=0.7):
    mean = sum(samples) / len(samples)
    samples = [x - mean for x in samples]
    scale = peak / max(abs(x) for x in samples)
    pcm = array.array("h", (round(x * scale * 32767) for x in samples))
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm.tobytes())
    rms = math.sqrt(sum((x * scale) ** 2 for x in samples) / len(samples))
    seam = abs(samples[0] - samples[-1]) * scale
    print(f"{name}: {len(samples) / SAMPLE_RATE:.2f}s, peak {peak:.2f}, RMS {rms:.3f}, seam {seam:.4f}")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    clips = {
        "LaserSalvo": loop_salvo(0.4, 0.1, 1500.0, 320.0, 11),
        "PointDefenseSalvo": loop_salvo(0.32, 0.08, 2100.0, 620.0, 21, 0.45),
        "TurbolaserSalvo": loop_salvo(0.6, 0.2, 850.0, 170.0, 31),
        "HeavyTurbolaser": pulse(0.27, 580.0, 85.0, 15.0, 41, 0.32, 0.45),
        "IonSalvo": loop_salvo(0.4, 0.2, 420.0, 105.0, 51, 0.9),
        "ProtonTorpedoLaunch": launcher(0.36, 61, True),
        "ConcussionMissileLaunch": launcher(0.22, 71, False),
        "LaserBeam": beam(),
    }
    for name, samples in clips.items():
        write_clip(name, samples)


if __name__ == "__main__":
    main()
