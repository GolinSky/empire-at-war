"""Converts the four source DDS textures to PNG and extracts the colorize alpha as a team mask."""
import sys
from pathlib import Path

from PIL import Image

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/Art/TEXTURES')
output = Path(sys.argv[1])
output.mkdir(parents=True, exist_ok=True)
for stem in ('W_Sattalite_Base', 'W_Sattalite_Base_n', 'W_Sattalite_Utility', 'W_Sattalite_Utility_n'):
    image = Image.open(SOURCE / (stem + '.dds')).convert('RGBA')
    name = 'RepairSatellite_' + stem.replace('W_Sattalite_', '')
    image.save(output / (name + '.png'))
    if not stem.endswith('_n'):
        image.getchannel('A').save(output / (name + '_TeamMask.png'))
    print(name, image.size)
