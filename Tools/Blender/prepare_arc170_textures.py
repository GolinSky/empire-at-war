"""Stage lossless ARC-170 source texture conversion before the Blender exporter."""
from pathlib import Path
from PIL import Image

SOURCE_DIRECTORY = Path('F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Textures')
OUTPUT_DIRECTORY = Path('F:/Private/empire-at-war/Temp/ARC170Import/Output/Textures')
TEXTURES = {
    'ReV_arc170.dds': 'ARC170_Hull_Albedo',
    'ReV_arc170_gloss.dds': 'ARC170_Hull_GlossSource',
    'nv_r2d2.dds': 'ARC170_Astromech_Albedo',
    'W_laser_small_blue.dds': 'ARC170_MuzzleFlash_Albedo',
}

OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)
for source_name, target_name in TEXTURES.items():
    with Image.open(SOURCE_DIRECTORY / source_name) as image:
        image.save(OUTPUT_DIRECTORY / (target_name + '.png'))
        if source_name == 'ReV_arc170.dds':
            image.getchannel('A').save(OUTPUT_DIRECTORY / 'ARC170_Hull_TeamMask.png')
print('Staged four PNG textures and the authored hull alpha team mask.')
