"""Remove orphan importer datablocks so editable blends need no external DDS files."""
import bpy
import json
from pathlib import Path

TASK = Path('F:/Private/empire-at-war/Temp/AotrEmpressStationImport')
reports = json.loads((TASK / 'ConversionReport.json').read_text())
for report in reports:
    path = TASK / 'Output' / (report['name'] + '.blend')
    bpy.ops.wm.open_mainfile(filepath=str(path))
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)
    for image in list(bpy.data.images):
        if image.users == 0:
            bpy.data.images.remove(image)
        elif image.filepath and image.packed_file is None:
            image.pack()
    assert all(image.packed_file is not None for image in bpy.data.images if image.filepath)
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
print('Packed',len(reports),'editable blends')
