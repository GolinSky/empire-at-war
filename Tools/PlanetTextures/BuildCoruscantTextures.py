"""Build native-resolution planetary rings plus a small repeating city/light tile.

Run with Python, Pillow and NumPy. No source image is enlarged.
The source tile and its imagegen prompt are stored beside this script.
"""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/Art/Textures/Models/Planets/Coruscant"
WIDTH, HEIGHT, SUPERSAMPLE = 8192, 4096, 2
RNG = np.random.default_rng(47021)

# Longitude, image latitude, angular radius: retain the broad distribution of hubs.
HUBS = [
    (.105, .615, .32), (.565, .223, .27), (.425, .822, .32),
    (.893, .453, .26), (.305, .410, .24), (.525, .416, .24),
    (.445, .597, .20), (.053, .410, .24), (.817, .579, .25),
    (.702, .472, .18), (.355, .198, .16), (.254, .226, .14),
    (.445, .222, .15), (.612, .753, .17), (.249, .611, .13),
    (.682, .685, .14), (.978, .257, .14), (.900, .246, .15),
    (.950, .665, .14), (.980, .436, .14), (.270, .746, .16),
    (.696, .832, .12), (.165, .219, .105), (.764, .225, .10),
    (.048, .260, .10), (.667, .190, .12), (.545, .661, .10),
    (.305, .897, .09), (.117, .805, .075), (.570, .892, .07),
]


def Sphere(u, v):
    longitude, latitude = (u-.5)*math.tau, (.5-v)*math.pi
    return np.array([math.cos(latitude)*math.cos(longitude), math.sin(latitude),
                     math.cos(latitude)*math.sin(longitude)])


def DrawPath(points, brightness, width):
    longitude = np.unwrap(np.arctan2(points[:, 2], points[:, 0]))
    x = (longitude/math.tau+.5) * WIDTH * SUPERSAMPLE
    y = (.5-np.arcsin(np.clip(points[:, 1], -1, 1))/math.pi) * HEIGHT * SUPERSAMPLE
    for shift in (-WIDTH*SUPERSAMPLE, 0, WIDTH*SUPERSAMPLE):
        DRAW.line(list(zip(x+shift, y)), fill=int(brightness),
                  width=max(1, round(width*SUPERSAMPLE)), joint="curve")


def Arc(center, tangent, bitangent, radius, start, end):
    angles = np.linspace(start, end, max(8, int(abs(end-start)*radius*800)))
    return (math.cos(radius)*center + math.sin(radius)*(
        np.cos(angles)[:, None]*tangent + np.sin(angles)[:, None]*bitangent))


# Small satellite districts are unique across the planet, never tiled with the detail map.
for _ in range(46):
    HUBS.append((RNG.uniform(0, 1), RNG.uniform(.13, .88), RNG.uniform(.018, .065)))
CENTERS = np.array([Sphere(u, v) for u, v, _ in HUBS])
MASK = Image.new("L", (WIDTH*SUPERSAMPLE, HEIGHT*SUPERSAMPLE))
DRAW = ImageDraw.Draw(MASK)

# Great-circle transport routes follow the surface and cross longitude zero continuously.
connections = set()
for index, center in enumerate(CENTERS):
    for other in np.argsort(-(CENTERS @ center))[1:4 if index < 30 else 2]:
        pair = tuple(sorted((index, int(other))))
        if pair in connections:
            continue
        connections.add(pair)
        destination = CENTERS[other]
        angle = math.acos(np.clip(center @ destination, -1, 1))
        t = np.linspace(0, 1, max(30, int(angle*700)))[:, None]
        points = (np.sin((1-t)*angle)*center + np.sin(t*angle)*destination)/math.sin(angle)
        DrawPath(points, RNG.uniform(48, 94), 1.6 if index < 30 else .8)

for index, ((_, _, radius), center) in enumerate(zip(HUBS, CENTERS)):
    tangent = np.cross(center, [0, 1, 0])
    tangent /= np.linalg.norm(tangent)
    bitangent = np.cross(center, tangent)
    fractions = (1, .94, .68, .61, .29, .13) if radius > .13 else (1, .74, .33)
    for ring, fraction in enumerate(fractions):
        r = radius*fraction
        width = 3.2 if radius > .13 else 1.5
        brightness = RNG.uniform(100, 152)
        DrawPath(Arc(center, tangent, bitangent, r, 0, math.tau), brightness*.56, width)
        # Short irregular light segments keep the circles from reading as a HUD overlay.
        for start in np.linspace(0, math.tau, 49)[:-1]:
            if RNG.random() < .18:
                continue
            offset = RNG.uniform(.003, .03)
            DrawPath(Arc(center, tangent, bitangent, r, start+offset, start+RNG.uniform(.065, .122)),
                     brightness*RNG.uniform(.7, 1.2), width*RNG.uniform(.65, 1.25))
    for angle in RNG.uniform(0, math.tau, 5 if radius > .13 else 3):
        radii = np.linspace(radius*.12, radius, 60)
        points = (np.cos(radii)[:, None]*center + np.sin(radii)[:, None]*
                  (math.cos(angle)*tangent + math.sin(angle)*bitangent))
        DrawPath(points, RNG.uniform(68, 115), 1.8)

MASK = MASK.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
mask = np.asarray(MASK, dtype=np.float32)/255
albedo = np.empty((HEIGHT, WIDTH, 3), dtype=np.uint8)
emission = np.empty_like(albedo)
longitude = np.linspace(-math.pi, math.pi, WIDTH, endpoint=False, dtype=np.float32)[None, :]
for top in range(0, HEIGHT, 128):
    latitude = (math.pi/2-np.arange(top, min(top+128, HEIGHT), dtype=np.float32)*math.pi/(HEIGHT-1))[:, None]
    x, y, z = np.cos(latitude)*np.cos(longitude), np.sin(latitude), np.cos(latitude)*np.sin(longitude)
    variation = (np.sin(x*13+y*7+z*9)*.45 + np.sin(x*31-y*19+z*27)*.25
                 + np.sin(x*67+y*61-z*49)*.12)
    patch = mask[top:top+128]
    base = np.array([45, 39, 37], dtype=np.float32) + variation[:, :, None]*5
    albedo[top:top+128] = np.clip(base + patch[:, :, None]*[108, 75, 42], 0, 255)
    emission[top:top+128] = np.clip(patch[:, :, None]*[245, 173, 91], 0, 255)
Image.fromarray(albedo).save(OUTPUT / "Coruscant_Surface_Albedo.png")
Image.fromarray(emission).save(OUTPUT / "Coruscant_Surface_Emissive.png")

# RGB is neutral-centered detail modulation (0.5 linear); alpha is aligned city lighting.
source = Image.open(Path(__file__).with_name("Coruscant_CityDetail_Source.png")).convert("RGB")
source = source.resize((1024, 1024), Image.Resampling.LANCZOS)
rgb = np.asarray(source, dtype=np.float32)/255
for axis in (0, 1):
    rgb = np.swapaxes(rgb, 0, axis)
    for offset in range(24):
        weight = .5*(1-offset/24)**2
        first, last = rgb[offset].copy(), rgb[-1-offset].copy()
        rgb[offset] = first*(1-weight)+last*weight
        rgb[-1-offset] = last*(1-weight)+first*weight
    rgb = np.swapaxes(rgb, 0, axis)
luminance = rgb @ np.array([.2126, .7152, .0722], dtype=np.float32)
linear = np.clip(.5+(luminance-luminance.mean())*2.4, .08, .98)
srgb = np.where(linear <= .0031308, linear*12.92, 1.055*linear**(1/2.4)-.055)
packed = np.empty((1024, 1024, 4), dtype=np.uint8)
packed[:, :, :3] = np.round(srgb[:, :, None]*255)
lights = np.clip((rgb[:, :, 0]-rgb[:, :, 2]-.035)*4, 0, 1)*np.clip((luminance-.20)*4, 0, 1)
packed[:, :, 3] = np.round(lights*255)
Image.fromarray(packed).save(OUTPUT / "Coruscant_City_Detail.png")
print(f"Drew {len(HUBS)} spherical hubs and {len(connections)} routes at {WIDTH}x{HEIGHT}; detail tile: 1024x1024 RGBA.")
