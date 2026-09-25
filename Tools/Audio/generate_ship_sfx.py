"""Generate original RTS sci-fi ability and sublight engine SFX (standard library only).

No sampled game audio is used. Run: python Tools/Audio/generate_ship_sfx.py
"""

import array
import math
from pathlib import Path
import random
import sys
import wave


SAMPLE_RATE = 44100
OUTPUT = Path(__file__).resolve().parents[2] / "Assets/Audio/SFX/Ships"
TAU = math.tau
# Distinct reactor, shield, turbine, weapon and targeting signatures.
ABILITIES = (
    ("ProtonBeam", 92, 1.7, 17),
    ("Invulnerability", 196, 0.45, 3),
    ("BoostShieldPower", 148, 0.7, 5),
    ("BoostEnginePower", 64, 0.8, 11),
    ("BoostWeaponPower", 112, 1.3, 13),
    ("Assault", 78, 1.6, 7),
    ("ConcentrateFire", 256, 0.35, 4),
)


def tone(duration, base, grit, pulse_rate, seed, stage):
    rng = random.Random(seed)
    result = []
    phase = low_noise = 0.0
    count = round(duration * SAMPLE_RATE)
    for i in range(count):
        t = i / SAMPLE_RATE
        u = t / duration
        # Execution tones complete an integer number of cycles for seamless loops.
        if stage == "Execution":
            frequency = base
            envelope = 0.75 + 0.25 * math.sin(TAU * pulse_rate * t) ** 2
        elif stage == "Start":
            frequency = base * (0.55 + 1.25 * (1.0 - math.exp(-u * 5)))
            envelope = min(1.0, t / 0.018) * (1 - u) ** 0.65
        elif stage == "End":
            frequency = base * (1.35 - u)
            envelope = min(1.0, t / 0.012) * (1 - u) ** 1.7
        else:
            # Short upward paired radio chirps indicate restored/ready status.
            frequency = base * (3.0 if u < 0.45 else 4.0)
            local = u / 0.45 if u < 0.45 else (u - 0.45) / 0.55
            envelope = math.sin(math.pi * local) ** 2 * (1 - u * 0.5)
        phase += TAU * frequency / SAMPLE_RATE
        low_noise += 0.035 * (rng.uniform(-1.0, 1.0) - low_noise)
        modulator = math.sin(TAU * pulse_rate * t)
        carrier = math.sin(phase + grit * modulator)
        body = 0.24 * math.sin(phase * 0.5) + 0.13 * math.sin(phase * 2.0)
        signal = carrier * 0.43 + body + low_noise * grit * 0.6
        result.append(math.tanh(signal * 1.2) * envelope)
    if stage == "Execution":
        # Periodic crossfade removes noise discontinuity at the wrap point.
        result = seamless(result)
    else:
        fade_edges(result)
    return result


def seamless(samples):
    overlap = round(0.08 * SAMPLE_RATE)
    for i in range(overlap):
        mix = i / overlap
        samples[i] = samples[len(samples) - overlap + i] * (1 - mix) + samples[i] * mix
    return samples[:-overlap]


def fade_edges(samples):
    fade = round(0.012 * SAMPLE_RATE)
    for i in range(fade):
        samples[i] *= i / fade
        samples[-1 - i] *= i / fade


def engine(acceleration):
    rng = random.Random(812)
    duration = 1.4 if acceleration else 3.08
    phase = low_noise = slow_noise = 0.0
    samples = []
    for i in range(round(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        u = t / duration
        frequency = 42 + (85 * u if acceleration else 0)
        phase += TAU * frequency / SAMPLE_RATE
        low_noise += 0.055 * (rng.uniform(-1.0, 1.0) - low_noise)
        slow_noise += 0.004 * (low_noise - slow_noise)
        rumble = 0.35 * math.sin(phase) + 0.18 * math.sin(phase * 2)
        turbine = 0.055 * math.sin(phase * 12 + 0.7 * math.sin(TAU * 7 * t))
        turbulence = 2.4 * (low_noise - slow_noise)
        envelope = math.sin(math.pi * u) ** 0.7 if acceleration else 1.0
        samples.append(math.tanh(rumble + turbine + turbulence) * envelope)
    if acceleration:
        fade_edges(samples)
        return samples
    return seamless(samples)


def write_clip(name, samples, peak):
    mean = sum(samples) / len(samples)
    samples = [sample - mean for sample in samples]
    scale = peak / max(abs(sample) for sample in samples)
    pcm = array.array("h", (round(sample * scale * 32767) for sample in samples))
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(OUTPUT / f"{name}.wav"), "wb") as clip:
        clip.setnchannels(1)
        clip.setsampwidth(2)
        clip.setframerate(SAMPLE_RATE)
        clip.writeframes(pcm.tobytes())


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    stages = (("Start", 0.65, 0.72), ("Execution", 2.08, 0.55),
              ("End", 0.5, 0.65), ("Restore", 0.36, 0.55))
    for index, (name, base, grit, pulse_rate) in enumerate(ABILITIES):
        for stage, duration, peak in stages:
            write_clip(name + stage, tone(duration, base, grit, pulse_rate, index * 10, stage), peak)
    write_clip("EngineCruise", engine(False), 0.6)
    write_clip("EngineAcceleration", engine(True), 0.72)
    print(f"Generated 30 mono PCM clips in {OUTPUT}")


if __name__ == "__main__":
    main()
