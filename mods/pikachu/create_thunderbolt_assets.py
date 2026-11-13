#!/usr/bin/env python3
"""Create an original pixel-art weapon icon and synthetic electric-zap sound."""
from pathlib import Path
from PIL import Image, ImageDraw
import math, random, struct, wave
root=Path(__file__).resolve().parent
im=Image.new('RGBA',(32,32)); d=ImageDraw.Draw(im)
d.ellipse((3,3,28,28),fill=(13,35,66,255),outline=(8,19,36,255),width=2)
d.ellipse((6,6,25,25),outline=(30,105,151,255),width=1)
bolt=[(18,3),(9,17),(15,17),(12,28),(24,12),(18,12),(22,3)]
d.polygon(bolt,fill=(12,29,51,255),outline=(7,16,30,255),width=3)
d.polygon([(18,5),(11,16),(17,16),(14,25),(22,13),(17,13),(20,5)],fill=(52,171,239,255))
d.polygon([(18,6),(13,14),(17,14),(16,19),(21,12),(17,12),(19,6)],fill=(193,246,255,255))
d.rectangle((5,9,6,10),fill=(150,241,255,255));d.rectangle((25,19,26,20),fill=(150,241,255,255))
im.resize((128,128),Image.Resampling.NEAREST).save(root/'content/thunderbolt-icon.png')
rate=44100; rng=random.Random(1009); samples=[]; phase=0.0
for n in range(int(rate*.29)):
 t=n/rate; envelope=(1-math.exp(-t*650))*math.exp(-t*13); freq=2200*math.exp(-t*12)+370
 phase+=2*math.pi*freq/rate
 crackle=(rng.random()*2-1)*math.exp(-t*23)
 sample=max(-1,min(1,.44*envelope*math.sin(phase)+.22*crackle))
 samples.append(struct.pack('<h',round(sample*32767)))
out=root/'content/thunderbolt-zap.wav'
with wave.open(str(out),'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate); w.writeframes(b''.join(samples))
print('created',out)
