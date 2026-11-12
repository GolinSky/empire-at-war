"""Rasterize scalable marker geometry and synthesize the two short UI cues."""
from pathlib import Path
from PIL import Image, ImageDraw
import math
import struct
import wave

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Assets/Art/Textures/Ui/TacticalMarkers'
AUDIO = ROOT / 'Assets/Audio/SFX/TacticalMarkers'
ART.mkdir(parents=True, exist_ok=True)
AUDIO.mkdir(parents=True, exist_ok=True)
S = 4
WHITE = (248, 250, 252, 255)
COLORS = ['C8CDD4', 'FF4B4B', 'FFC531', '3F8CFF', '2FE0D0', 'B060FF']
NAMES = ['Look', 'Attack', 'Capture', 'Defend', 'Rally', 'Retreat']

def canvas():
    image = Image.new('RGBA', (256*S, 256*S))
    return image, ImageDraw.Draw(image)

def line(draw, points, width=10, fill=WHITE):
    draw.line([(x*S, y*S) for x,y in points], fill=fill, width=width*S, joint='curve')

def ellipse(draw, box, width=10):
    draw.ellipse(tuple(v*S for v in box), outline=WHITE, width=width*S)

def save(image, name):
    image.resize((256,256), Image.Resampling.LANCZOS).save(ART / (name+'.png'))

for name in NAMES:
    im,d=canvas()
    if name=='Look':
        line(d,[(35,128),(70,91),(100,79),(128,75),(156,79),(186,91),(221,128),(186,165),(156,177),(128,181),(100,177),(70,165),(35,128)])
        ellipse(d,(101,101,155,155),12)
    elif name=='Attack':
        ellipse(d,(63,63,193,193),11)
        for p in [[(128,31),(128,88)],[(128,168),(128,225)],[(31,128),(88,128)],[(168,128),(225,128)]]: line(d,p)
        ellipse(d,(116,116,140,140),12)
    elif name=='Capture':
        line(d,[(66,215),(66,44),(193,44),(170,83),(193,122),(66,122)],11)
        line(d,[(43,215),(100,215)],11)
    elif name=='Defend':
        line(d,[(128,36),(201,66),(194,139),(170,182),(128,218),(86,182),(62,139),(55,66),(128,36)],11)
        line(d,[(93,128),(117,153),(165,103)],12)
    elif name=='Rally':
        ellipse(d,(68,38,188,158),11)
        line(d,[(75,132),(128,219),(181,132)],11)
        ellipse(d,(108,78,148,118),10)
    else:
        d.arc((62*S,49*S,195*S,182*S),180,360,fill=WHITE,width=12*S)
        line(d,[(195,115),(195,210)],12)
        line(d,[(62,113),(62,169)],12)
        line(d,[(32,139),(62,169),(92,139)],12)
    save(im,name)

im,d=canvas()
octagon=[(75,12),(181,12),(244,75),(244,181),(181,244),(75,244),(12,181),(12,75),(75,12)]
d.polygon([(x*S,y*S) for x,y in octagon],fill=(8,12,20,220))
line(d,octagon,4)
line(d,[(111,24),(145,24)],3);line(d,[(111,232),(145,232)],3)
save(im,'Octagon')
im,d=canvas();d.rounded_rectangle((12*S,12*S,244*S,244*S),radius=38*S,fill=(8,12,20,225),outline=WHITE,width=4*S);save(im,'Square')
im,d=canvas();ellipse(d,(13,13,243,243),9);save(im,'Ring')
for name,color in zip(NAMES,COLORS):
    bg=Image.open(ART/'Square.png').convert('RGBA')
    glyph=Image.open(ART/(name+'.png')).convert('RGBA').resize((184,184),Image.Resampling.LANCZOS)
    bg.alpha_composite(glyph,(36,36))
    rgb=tuple(bytes.fromhex(color))
    pixels=bg.load()
    for y in range(256):
        for x in range(256):
            r,g,b,a=pixels[x,y]; pixels[x,y]=(r*rgb[0]//255,g*rgb[1]//255,b*rgb[2]//255,a)
    bg.save(ART/('Map'+name+'.png'))
for name,frequencies in [('Info',(660,990)),('Urgent',(880,660,1100))]:
    rate=44100; duration=0.35 if name=='Info' else 0.48
    values=[]
    for i in range(int(rate*duration)):
        t=i/rate; p=t/duration; index=min(len(frequencies)-1,int(p*len(frequencies)))
        envelope=min(1,t/0.015)*max(0,1-p)**2
        sample=(math.sin(2*math.pi*frequencies[index]*t)+0.18*math.sin(2*math.pi*frequencies[index]*2*t))*envelope*0.28
        values.append(struct.pack('<h',int(sample*32767)))
    with wave.open(str(AUDIO/(name+'.wav')),'wb') as output:
        output.setparams((1,2,rate,0,'NONE','not compressed'));output.writeframes(b''.join(values))
