"""Generate the Chimaera rust + gold hull textures from the ISD II hull tiles.

Writes, per hull tile, a weathered albedo and a metallic/smoothness map (R = metallic,
A = smoothness, as Ship Lit expects) into Assets/Art/Textures/Models/EmpireShips/Chimaera/.
Patches are built from periodic noise, so they tile seamlessly like the source textures.

Usage: python Tools/Textures/generate_chimaera_weathering.py [--preview <png>]
"""
import argparse
import os

import numpy as np
from PIL import Image

SOURCE_DIR = "Assets/Art/Textures/Models/EmpireShips/ISDIIReplacement"
OUTPUT_DIR = "Assets/Art/Textures/Models/EmpireShips/Chimaera"

# Strength per tile: (gold coverage, rust coverage). Heavy on kitbash/trench, light on plating.
HEAVY = (0.16, 0.20)
MEDIUM = (0.09, 0.12)
LIGHT = (0.035, 0.06)
TILES = {
    "ISDII_Hull_Material_HighContrast": HEAVY,
    "ISDII_Hull_Kitbash_Pieces_ISD": HEAVY,
    "ISDII_Hull_Kitbash_Trench_ISD": HEAVY,
    "ISDII_Hull_Kitbash_Trench_ISD_Inset": MEDIUM,
    "ISDII_Empire_ISD_Bridge": MEDIUM,
    "ISDII_Hull_16": LIGHT,
    "ISDII_Hull_2_SD": LIGHT,
    "ISDII_Hull_Superstructure_Base": LIGHT,
    "ISDII_Empire_ISD_Superstructure_Base": LIGHT,
}

GOLD = np.array([0.66, 0.53, 0.31])
GOLD_DARK = np.array([0.24, 0.18, 0.10])
RUST = np.array([0.40, 0.25, 0.15])
RUST_DARK = np.array([0.20, 0.13, 0.08])

# Metallic (R) / smoothness (A) per surface; materials use _Metallic = _Smoothness = 1.
PLATE_METAL, PLATE_SMOOTH = 0.10, 0.25
GOLD_METAL, GOLD_SMOOTH = 0.40, 0.45  # low enough that the gold albedo, not dark space reflections, carries the colour
RUST_METAL, RUST_SMOOTH = 0.05, 0.12


def periodic_noise(rng, size, wavelength, aspect=(1.0, 1.0)):
    """Low-pass filtered white noise; periodic because the filter runs in the FFT domain."""
    white = rng.standard_normal((size, size))
    fy = np.fft.fftfreq(size)[:, None] * size * aspect[0]
    fx = np.fft.fftfreq(size)[None, :] * size * aspect[1]
    cutoff = size / wavelength
    spectrum = np.fft.fft2(white) * np.exp(-(fx * fx + fy * fy) / (2.0 * cutoff * cutoff))
    noise = np.real(np.fft.ifft2(spectrum))
    return (noise - noise.mean()) / (noise.std() + 1e-8)


def block_noise(rng, size, blocks):
    """Chunky rectangular noise (nearest upsample) so patches read as missing plates."""
    coarse = rng.random((blocks, blocks))
    return np.kron(coarse, np.ones((size // blocks, size // blocks)))


def coverage_mask(field, coverage, softness=0.04):
    threshold = np.quantile(field, 1.0 - coverage)
    return np.clip((field - threshold) / softness + 0.5, 0.0, 1.0)


def streaks(source, rng, size, length):
    """Rust running down the texture's V axis from a source mask, broken up per column."""
    acc = np.zeros_like(source)
    decay = 1.0
    for step in range(0, length, 2):
        acc = np.maximum(acc, np.roll(source, step, axis=0) * decay)
        decay *= 0.965
    columns = periodic_noise(rng, size, size / 40.0, aspect=(0.08, 1.0))
    return acc * np.clip(columns * 0.6 + 0.55, 0.0, 1.0)


def weather(name, gold_cov, rust_cov, seed):
    src = Image.open(os.path.join(SOURCE_DIR, name + ".png")).convert("RGBA")
    rgba = np.asarray(src).astype(np.float64) / 255.0
    rgb, alpha = rgba[..., :3], rgba[..., 3]
    size = rgb.shape[0]
    rng = np.random.default_rng(seed)
    lum = rgb @ np.array([0.299, 0.587, 0.114])
    detail = np.clip((lum - lum.mean()) * 1.6 + 0.5, 0.0, 1.0)

    # Regions: most of the tile stays clean; weathering clusters where this is high.
    regions = periodic_noise(rng, size, size / 2.5)
    blobs = periodic_noise(rng, size, size / 7.0)
    chunks = block_noise(rng, size, 32)
    fine = periodic_noise(rng, size, size / 90.0)
    gold = coverage_mask(regions * 1.3 + blobs * 0.55 + (chunks - 0.5) * 1.4 + fine * 0.2, gold_cov)

    rust_seed = streaks(gold, rng, size, int(size * 0.10))
    rust_blobs = coverage_mask(regions * 1.2 + periodic_noise(rng, size, size / 12.0) + fine * 0.4, rust_cov * 0.5, 0.25)
    rust = np.clip(np.maximum(rust_seed * 0.85, rust_blobs * 0.7) - gold, 0.0, 1.0)
    rust *= np.clip(fine * 0.35 + 0.8, 0.0, 1.0)

    gold_rgb = GOLD_DARK + (GOLD - GOLD_DARK) * detail[..., None] ** 1.3
    rust_rgb = RUST_DARK + (RUST - RUST_DARK) * np.clip(detail + fine[..., None][..., 0] * 0.15, 0, 1)[..., None]
    out = rgb * (1.0 - rust[..., None]) + rust_rgb * rust[..., None]
    out = out * (1.0 - gold[..., None]) + gold_rgb * gold[..., None]

    metal = PLATE_METAL + (RUST_METAL - PLATE_METAL) * rust + (GOLD_METAL - PLATE_METAL) * gold
    smooth = PLATE_SMOOTH + (RUST_SMOOTH - PLATE_SMOOTH) * rust + (GOLD_SMOOTH - PLATE_SMOOTH) * gold
    zeros = np.zeros_like(metal)
    mask = np.stack([metal, zeros, zeros, smooth], axis=-1)

    albedo = np.concatenate([out, alpha[..., None]], axis=-1)
    return to_image(albedo), to_image(mask)


def to_image(values):
    return Image.fromarray((np.clip(values, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8), "RGBA")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", help="Write a contact sheet of the albedo results here.")
    args = parser.parse_args()
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    previews = []
    for seed, (name, (gold_cov, rust_cov)) in enumerate(TILES.items()):
        albedo, mask = weather(name, gold_cov, rust_cov, seed + 1)
        out_name = name.replace("ISDII_", "Chimaera_")
        albedo.save(os.path.join(OUTPUT_DIR, out_name + ".png"))
        mask.save(os.path.join(OUTPUT_DIR, out_name + "_MetallicSmoothness.png"))
        previews.append(albedo.convert("RGB").resize((256, 256)))
        print("wrote", out_name)
    if args.preview:
        sheet = Image.new("RGB", (256 * 5, 256 * 2))
        for i, image in enumerate(previews):
            sheet.paste(image, ((i % 5) * 256, (i // 5) * 256))
        sheet.save(args.preview)


if __name__ == "__main__":
    main()
