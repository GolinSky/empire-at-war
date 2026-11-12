"""Stage original EaW/FoC planet artwork without modifying Unity assets.

Run from the repository root with Pillow installed. Patch2 planet definitions
take precedence; Alderaan uses its intact original-game definition. High-detail
artwork is preferred to the galactic model's low-detail texture where available.
"""
import hashlib
import io
import json
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image

GAME = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War')
OUTPUT = Path('Temp/PlanetImport')
EXTRA_MODELS = {
    'Balmorra': 'w_planet_balmorra_high.alo',
    'Ralltiir': 'w_planet_ralltiir_high.alo',
    'Hapes': 'w_planet_hapes_low.alo',
    'Lwhekk': 'w_planet_lwhekk01.alo',
}


def Index(path):
    with path.open('rb') as stream:
        name_count, file_count = struct.unpack('<II', stream.read(8))
        names = [stream.read(struct.unpack('<H', stream.read(2))[0]).decode('ascii')
                 for _ in range(name_count)]
        rows = [struct.unpack('<5I', stream.read(20)) for _ in range(file_count)]
    return {names[row[4]].split('\\')[-1].lower(): (path, row[3], row[2]) for row in rows}


def Read(row):
    with row[0].open('rb') as stream:
        stream.seek(row[1])
        return stream.read(row[2])


def Chunks(data):
    offset = 0
    while offset < len(data):
        kind, size = struct.unpack_from('<II', data, offset)
        size &= 0x7fffffff
        yield kind, data[offset + 8:offset + 8 + size]
        offset += 8 + size
    assert offset == len(data)


def MicroChunks(data):
    offset = 0
    while offset < len(data):
        kind, size = data[offset:offset + 2]
        yield kind, data[offset + 2:offset + 2 + size]
        offset += 2 + size


def Materials(data):
    result = []
    for kind, mesh in Chunks(data):
        if kind != 0x400:
            continue
        name = next(value.rstrip(b'\0').decode('ascii') for key, value in Chunks(mesh) if key == 0x401)
        for key, material in Chunks(mesh):
            if key != 0x10100:
                continue
            fields = dict(Chunks(material))
            properties = {}
            for parameter, value in Chunks(material):
                if parameter != 0x10105:
                    continue
                values = list(MicroChunks(value))
                properties[values[0][1].rstrip(b'\0').decode('ascii')] = values[1][1].rstrip(b'\0').decode('ascii')
            result.append(dict(mesh=name, shader=fields[0x10101].rstrip(b'\0').decode('ascii'), properties=properties))
    return result


def RingGeometry(data, wanted):
    result = []
    for kind, mesh in Chunks(data):
        if kind != 0x400:
            continue
        name = next(value.rstrip(b'\0').decode('ascii') for key, value in Chunks(mesh) if key == 0x401)
        if name not in wanted:
            continue
        for key, geometry in Chunks(mesh):
            if key != 0x10000:
                continue
            fields = dict(Chunks(geometry))
            count, triangles = struct.unpack_from('<II', fields[0x10001])
            raw = fields[0x10007] if 0x10007 in fields else fields[0x10005]
            stride = len(raw) // count
            rows = [struct.unpack_from('<8f', raw, i * stride) for i in range(count)]
            indices = list(struct.unpack('<' + 'H' * triangles * 3, fields[0x10004]))
            # Source Z-up/right-handed -> Unity Y-up/left-handed, radius 1.
            radius = 4.921259880065918
            result.append(dict(name=name,
                               vertices=[[r[0] / radius, r[2] / radius, r[1] / radius] for r in rows],
                               normals=[[r[3], r[5], r[4]] for r in rows],
                               uv=[[r[6], 1 - r[7]] for r in rows],
                               triangles=[indices[i + j] for i in range(0, len(indices), 3) for j in (0, 2, 1)]))
    return result


def Main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT / 'Textures').mkdir(exist_ok=True)
    (OUTPUT / 'Previews').mkdir(exist_ok=True)
    indices = {}
    definitions = {}
    for game in ['GameData', 'corruption']:
        files = {}
        for archive in ['config.meg', 'models.meg', 'textures.meg', 'patch.meg', 'patch2.meg', '64Patch.meg']:
            files.update(Index(GAME / game / 'Data' / archive))
        indices[game] = files
        xml = Read(files['planets.xml'])
        (OUTPUT / (game + '-Planets.xml')).write_bytes(xml)
        definitions[game] = {p.get('Name'): p for p in ET.fromstring(xml) if p.get('Name')}
    files = dict(indices['GameData'])
    files.update(indices['corruption'])
    # Shipped named planet artwork absent from both games' XML catalogs.
    for name, model in EXTRA_MODELS.items():
        planet = ET.Element('Planet', Name=name)
        ET.SubElement(planet, 'Galactic_Model_Name').text = model
        definitions['corruption'][name] = planet
    textures = {}

    def Texture(reference, role='surface'):
        name = str(Path(reference.lower()).with_suffix('.dds'))
        if name not in textures:
            row = files[name]
            raw = Read(row)
            source = Image.open(io.BytesIO(raw)).convert('RGBA')
            destination = OUTPUT / 'Textures' / (Path(name).stem + '.png')
            source.save(destination)
            assert Image.open(destination).convert('RGBA').tobytes() == source.tobytes()
            textures[name] = dict(source_archive=str(row[0]), source_file=name,
                                  source_sha256=hashlib.sha256(raw).hexdigest(),
                                  png=str(destination).replace('\\', '/'), size=list(source.size),
                                  alpha=list(source.getchannel('A').getextrema()), role=role)
        return name

    planets = []
    skipped = []
    for source_name, planet in definitions['corruption'].items():
        if any(token in source_name for token in ['Galaxy_Core', '_Story', '_Prologue', '_Tutorial', '_E3', 'Vergesso', 'The_Maw']):
            skipped.append(source_name)
            continue
        name = {'Abregado_Rae': 'AbregadoRae', 'MonCalimari': 'MonCalamari'}.get(source_name, source_name)
        if source_name == 'Alderaan':
            planet = definitions['GameData'][source_name]
        galactic = planet.findtext('Galactic_Model_Name').strip().lower()
        model = galactic
        high = model.replace('_low.alo', '_high.alo')
        if high in files:
            model = high
        if name == 'Mustafar':
            model = 'w_planet_mustafar_high.alo'
        materials = Materials(Read(files[model]))
        candidates = [m for m in materials if m['shader'].lower() == 'planet.fx' and
                      'moon_low' not in m['properties']['BaseTexture'].lower()]
        candidates.sort(key=lambda m: '_low' in m['properties']['BaseTexture'].lower())
        surface = candidates[0] if candidates else None
        if name == 'Kessel':
            surface = next(m for m in materials if m['properties'].get('BaseTexture', '').lower() == 'w_asteroid00.tga')
        if name == 'NalHutta':
            surface = next(m for m in materials if m['properties'].get('BaseTexture', '').lower() == 'w_planet_swamp.tga')
        if surface is None:
            raise ValueError((name, model, materials))
        props = surface['properties']
        surface_name = Texture(props['BaseTexture'])
        cloud_ref = props.get('CloudTexture', '')
        cloud = Texture(cloud_ref, 'cloud') if cloud_ref and 'none' not in cloud_ref.lower() else ''
        rings = []
        for material in materials:
            ref = material['properties'].get('BaseTexture', '')
            if ref.lower() in ('w_planet_rings.tga', 'w_ring_mesh.tga', 'eb_station.tga'):
                rings.append(dict(mesh=material['mesh'], texture=Texture(ref, 'ring'), shader=material['shader']))
        if rings:
            geometry = RingGeometry(Read(files[model]), {r['mesh'] for r in rings})
            (OUTPUT / (name + '-Rings.json')).write_text(json.dumps(geometry, separators=(',', ':')))
        preview_names = [f'_space_planet_{source_name.replace("_", "")}_01.dds'.lower()]
        if name == 'AbregadoRae':
            preview_names.insert(0, '_space_planet_abregadorae_01.dds')
        if name == 'MonCalamari':
            preview_names.insert(0, '_space_planet_moncalamari_01.dds')
        preview = next((candidate for candidate in preview_names if candidate in files), '')
        preview_path = ''
        if preview:
            image = Image.open(io.BytesIO(Read(files[preview]))).convert('RGB')
            preview_path = OUTPUT / 'Previews' / (name + '.png')
            image.save(preview_path)
        elif name in EXTRA_MODELS:
            preview_path = OUTPUT / 'Previews' / (name + '.png')
            Image.open(textures[surface_name]['png']).convert('RGB').save(preview_path)
        planets.append(dict(name=name, source_name=source_name, galactic_model=galactic,
                            artwork_model=model, model_archive=str(files[model][0]),
                            surface=surface_name, cloud=cloud, rings=rings,
                            preserve_existing=name in ('Coruscant', 'Kamino'),
                            preview=str(preview_path).replace('\\', '/'),
                            preview_source=preview, materials=materials))
        if name in EXTRA_MODELS:
            planets[-1]['generated_preview'] = True
            planets[-1]['preview_source'] = 'source-albedo-placeholder'
            planets[-1]['catalog_source'] = 'shipped-model-only'
    result = dict(planets=sorted(planets, key=lambda p: p['name']), textures=textures, skipped=skipped,
                  decisions=['Alderaan uses intact original-game artwork.',
                             'Mustafar uses its dedicated FoC high-detail artwork instead of its generic galactic model.',
                             'Kessel retains its asteroid surface texture on the requested shared sphere.',
                             'Kuat has an artificial shipyard ring; Geonosis has natural rings.',
                             'Coruscant and Kamino existing Unity artwork is preserved.',
                             'Balmorra, Ralltiir, Hapes and Lwhekk are shipped named models absent from the XML catalog.',
                             'Their staged source-albedo preview placeholders must be replaced with rendered prefab thumbnails.'])
    (OUTPUT / 'manifest.json').write_text(json.dumps(result, indent=2) + '\n')
    sources = dict(
        planets=[{key: value for key, value in planet.items()
                  if key not in ('model_archive', 'preview', 'materials')} for planet in result['planets']],
        textures={name: {key: value for key, value in texture.items()
                         if key not in ('source_archive', 'png')} for name, texture in textures.items()},
        decisions=result['decisions'])
    Path(__file__).with_name('OriginalPlanetSources.json').write_text(json.dumps(sources, indent=2) + '\n')
    print(f'{len(planets)} planets; {len(textures)} unique textures; {sum(bool(p["preview"]) for p in planets)} previews')
    print('\n'.join(f'{n}: {v["size"]}, alpha {v["alpha"]}' for n, v in textures.items()))


if __name__ == '__main__':
    Main()
