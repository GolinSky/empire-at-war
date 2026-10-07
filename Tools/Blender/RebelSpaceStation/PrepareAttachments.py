"""Resolve the original station hardpoint artwork from STARBASES/HARDPOINTS XML."""
import hashlib
import json
import struct
import xml.etree.ElementTree as ET
from pathlib import Path
from Prepare import Audit, OUTPUT

GAME = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data')


def Extract(archive, names, destination):
    with archive.open('rb') as stream:
        name_count, file_count = struct.unpack('<II', stream.read(8))
        members = [stream.read(struct.unpack('<H', stream.read(2))[0]).decode('ascii') for _ in range(name_count)]
        entries = [struct.unpack('<5I', stream.read(20)) for _ in range(file_count)]
        for filename in names:
            row = next(row for row in entries if Path(members[row[4]].replace('\\', '/')).name.upper() == filename.upper())
            stream.seek(row[3])
            data = stream.read(row[2])
            assert len(data) == row[2]
            (destination / filename).write_bytes(data)


def Main():
    source = OUTPUT / 'Attachments'
    source.mkdir(parents=True, exist_ok=True)
    Extract(GAME / 'config.meg', ['STARBASES.XML', 'HARDPOINTS.XML'], source)
    bases = ET.parse(source / 'STARBASES.XML').getroot()
    hardpoints = {row.attrib['Name']: row for row in ET.parse(source / 'HARDPOINTS.XML').getroot() if 'Name' in row.attrib}
    levels = {}
    for level in range(1, 6):
        base = next(row for row in bases if row.attrib.get('Name') == f'Rebel_Star_Base_{level}')
        rows = []
        for name in base.findtext('HardPoints').split(','):
            hp = hardpoints[name.strip()]
            model = hp.findtext('Model_To_Attach')
            if model:
                rows.append(dict(model=Path(model.strip()).stem, bone=hp.findtext('Attachment_Bone').strip()))
        levels[str(level)] = rows
    names = sorted({row['model'] for rows in levels.values() for row in rows})
    Extract(GAME / 'models.meg', [name + '.alo' for name in names], source)
    audit = dict(models={}, textures=json.loads((OUTPUT / 'SourceAudit.json').read_text())['textures'], levels=levels)
    for name in names:
        model = Audit(source / (name + '.alo'))
        model['level'] = int(name.split('_')[2])
        audit['models'][name] = model
        print(name, 'bones', len(model['bones']), [(m['name'], m['hidden'], [s['shader'] for s in m['materials']]) for m in model['meshes']])
        for mesh in model['meshes']:
            for material in mesh['materials']:
                for value in material['properties'].values():
                    if isinstance(value, str) and value.lower().endswith(('.tga', '.dds')):
                        assert value.lower() in audit['textures'], 'Missing attachment texture: ' + value
    audit['xml_hashes'] = {name: hashlib.sha256((source / name).read_bytes()).hexdigest() for name in ['STARBASES.XML', 'HARDPOINTS.XML']}
    (OUTPUT / 'AttachmentAudit.json').write_text(json.dumps(audit, indent=2))
    Path(__file__).with_name('Attachments.json').write_text(json.dumps(levels, indent=2) + '\n')
    for name, model in audit['models'].items():
        conversion = dict(models={name: model}, textures=audit['textures'])
        code = 'AUDIT = ' + repr(conversion) + '\n' + Path(__file__).with_name('Convert.py').read_text()
        request = dict(tool='execute_blender_code', arguments=dict(code=code,
            user_prompt='albedo is little bid broken - fix it . issue is reproduced on lvl stations - maybe material or mesh missing'))
        (source / (name + '.json')).write_text(json.dumps(request))
    print('Attachments per level:', {level: len(rows) for level, rows in levels.items()})


if __name__ == '__main__':
    Main()
