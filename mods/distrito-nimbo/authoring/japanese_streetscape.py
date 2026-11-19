"""Original Nimbo streetscape study. Blender: -b --python this.py -- OUTPUT_ROOT.

Metres, Z up in authoring, front -Y; exported Unity coordinates (-x,z,-y), preserving front-facing sign reading.
All display glass is opaque tinted backing with real recessed product geometry.
Sources are editable named mesh parts; no runtime generator or game data edits.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(sys.argv[sys.argv.index('--')+1])
ROOT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
PALETTE = {
 'ink':(.055,.095,.14,1), 'roof':(.09,.20,.28,1), 'cream':(.91,.84,.65,1),
 'white':(.92,.94,.87,1), 'coral':(.80,.12,.075,1), 'jade':(.08,.42,.32,1),
 'gold':(1,.63,.12,1), 'glass':(.19,.52,.64,1), 'wood':(.34,.19,.105,1),
 'leaf':(.22,.43,.24,1), 'leaflight':(.43,.61,.27,1), 'pink':(.88,.42,.50,1),
 'stone':(.47,.52,.51,1), 'silver':(.62,.69,.69,1), 'paper':(.98,.91,.74,1),
}
MATS={}
for n,c in PALETTE.items():
 m=bpy.data.materials.new(n);m.diffuse_color=c;m.use_nodes=True
 m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=c
 m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.79
 MATS[n]=m
FONT=None
font_paths=list(Path('/nix/store').glob('*noto-fonts-cjk-sans-*/share/fonts/opentype/noto-cjk/*.ttc'))
if font_paths:
 try: FONT=bpy.data.fonts.load(str(font_paths[0]))
 except Exception: pass
CURRENT=None
PARTS={}

def finish(name,mat):
 o=bpy.context.object;o.name=name;o.data.materials.append(MATS[mat]);o.parent=CURRENT
 PARTS[CURRENT.name].append(o)
 return o

def box(name,pos,size,mat,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1, location=pos);o=finish(name,mat);o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Small manufactured edge','BEVEL');mod.width=bevel;mod.segments=1
 return o

def cyl(name,pos,radius,depth,mat,axis='Z',vertices=8,radius2=None):
 if radius2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=pos)
 else: bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=radius,radius2=radius2,depth=depth,location=pos)
 o=finish(name,mat)
 if axis=='Y':o.rotation_euler.x=math.pi/2
 if axis=='X':o.rotation_euler.y=math.pi/2
 return o

def sphere(name,pos,scale,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=4,location=pos);o=finish(name,mat);o.scale=scale;return o

def beam(name,a,b,width,mat):
 d=Vector(b)-Vector(a);o=box(name,(Vector(a)+Vector(b))*.5,(width,width,d.length),mat)
 o.rotation_mode='QUATERNION';o.rotation_quaternion=d.to_track_quat('Z','Y');return o

def text(word,pos,size,mat,maxwidth=None):
 cu=bpy.data.curves.new('Lettering '+word,'FONT');cu.body=word;cu.align_x='CENTER';cu.align_y='CENTER';cu.size=size;cu.resolution_u=2;cu.extrude=.003
 if FONT:cu.font=FONT
 o=bpy.data.objects.new('Lettering '+word,cu);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler.x=math.pi/2;o.data.materials.append(MATS[mat]);o.parent=CURRENT
 bpy.context.view_layer.update()
 if o.dimensions.z > .0001:o.scale *= size/o.dimensions.z
 bpy.context.view_layer.update()
 if maxwidth and o.dimensions.x>maxwidth:o.scale*=maxwidth/o.dimensions.x
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o.select_set(False)
 PARTS[CURRENT.name].append(o);return o

def sign(word,x,y,z,w,h,back='coral',fg='paper'):
 box('Sign border',(x,y,z),(w,.15,h),'ink',.025)
 box('Sign enamel',(x,y-.085,z),(w-.1,.028,h-.1),back)
 text(word,(x,y-.109,z),h*.67,fg,w-.2)

def roof(name,x,y,z,w,d,h,mat='roof'):
 # Thick, explicitly closed gable with deep eaves and a clear ridgeline.
 v=[(x-w/2,y-d/2,z),(x+w/2,y-d/2,z),(x+w/2,y+d/2,z),(x-w/2,y+d/2,z),
    (x-w/2,y,z+h),(x+w/2,y,z+h)]
 faces=[(0,3,2,1),(0,1,5,4),(3,4,5,2),(0,4,3),(1,2,5)]
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.parent=CURRENT;o.data.materials.append(MATS[mat]);PARTS[CURRENT.name].append(o)
 beam('Ridge cap',(x-w/2-.06,y,z+h+.06),(x+w/2+.06,y,z+h+.06),.15,'ink')
 for side in (-1,1):
  beam('Eave lip',(x-w/2-.05,y+side*d/2,z),(x+w/2+.05,y+side*d/2,z),.12,'ink')
 for i in range(int(w/.4)+1):
  xx=x-w/2+i*w/int(w/.4)
  for side in (-1,1):beam('Roof tile seam',(xx,y,z+h+.02),(xx,y+side*d/2,z+.02),.035,mat)

def planter(x,y,w=1.1):
 box('Planter stone',(x,y,.32),(w,.72,.64),'stone',.055)
 box('Recessed soil',(x,y,.65),(w-.15,.58,.035),'wood')
 for i in range(3):
  xx=x+(i-1)*w*.24
  sphere('Clipped green shrub',(xx,y,.92+(.12 if i==1 else 0)),(w*.3,.35,.4),'leaf' if i%2 else 'leaflight')

def bottle(x,y,z,col):
 cyl('Drink body',(x,y,z),.065,.25,col,vertices=6)
 cyl('Drink shoulder',(x,y,z+.145),.065,.045,col,vertices=6,radius2=.025)
 cyl('Bottle cap',(x,y,z+.18),.027,.035,'white',vertices=6)
 box('Drink label',(x,y-.065,z),(.09,.009,.06),'paper')

def vending(x,y,col='coral'):
 # Geometry deliberately models recesses, not a texture pretending to be products.
 box('Vending cabinet',(x,y,1.20),(1.25,.82,2.30),col,.055)
 box('Display inset',(x-.09,y-.423,1.51),(.96,.035,1.35),'ink')
 box('Blue display backing',(x-.09,y-.45,1.54),(.84,.018,1.20),'glass')
 for row in range(3):
  zz=1.13+row*.37
  box('Product shelf',(x-.10,y-.57,zz-.17),(.88,.23,.045),'white')
  box('Selection strip',(x-.10,y-.697,zz-.18),(.88,.035,.09),'silver')
  for k in range(5):
   xx=x-.45+k*.175
   bottle(xx,y-.55,zz,('gold','jade','coral','white','glass')[(k+row)%5])
   cyl('Blue purchase button',(xx,y-.724,zz-.18),.031,.018,'glass',axis='Y',vertices=6)
 box('Right payment strip',(x+.485,y-.447,1.09),(.16,.055,.69),'ink')
 cyl('Coin slot rim',(x+.485,y-.49,1.24),.055,.025,'silver',axis='Y')
 box('Coin slot',(x+.485,y-.506,1.24),(.013,.012,.072),'ink')
 box('Card reader',(x+.485,y-.5,.97),(.105,.03,.11),'jade')
 box('Retrieval black recess',(x-.11,y-.435,.40),(.84,.05,.25),'ink')
 box('Retrieval lower lip',(x-.11,y-.51,.28),(.89,.15,.045),'silver')
 box('Cabinet bottom grill',(x,y-.43,.15),(1.07,.025,.08),'ink')
 for i in range(7):box('Grill slit',(x-.45+i*.15,y-.448,.15),(.02,.015,.065),'silver')
 for xx in (-.43,.43):box('Leveling feet',(x+xx,y,.035),(.13,.55,.07),'ink')
 sign('のむ',x,y-.43,.72,.73,.25,col)

def lantern(x,y,z,col='coral'):
 sphere('Paper lantern',(x,y,z),(.25,.24,.38),col)
 for zz in (-.23,0,.23):cyl('Lantern rib',(x,y,z+zz),.245 if zz==0 else .2,.025,'wood',vertices=12)
 cyl('Lantern cap',(x,y,z+.36),.12,.06,'ink')
 cyl('Lantern foot',(x,y,z-.36),.12,.06,'ink')
 beam('Lantern suspension',(x,y,z+.4),(x,y,z+.65),.035,'ink')

def root(name):
 global CURRENT
 CURRENT=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(CURRENT);PARTS[name]=[];return CURRENT

def vending_corner():
 root('NimboVendingCorner')
 box('Concrete service pad',(0,0,.045),(4.8,2.15,.09),'stone')
 for x,col in ((-.94,'coral'),(.49,'white')):vending(x,.17,col)
 for x in (-2.15,2.15):box('Canopy posts',(x,.65,1.47),(.12,.12,2.85),'ink')
 box('Canopy roof',(0,.03,2.88),(4.7,1.90,.15),'jade')
 sign('ニンボ通り',0,-.94,2.67,2.3,.42,'jade')
 box('Bottle return bin',(1.65,-.2,.62),(.52,.64,1.1),'silver',.04)
 for xx in (1.52,1.77):cyl('Recycling opening',(xx,-.535,.91),.078,.015,'ink',axis='Y')
 planter(-1.82,-.42,.65)

def gacha(x,y,z,col):
 box('Capsule machine',(x,y,z+.48),(.65,.65,.91),'white',.025)
 box('Capsule front display',(x,y-.337,z+.65),(.50,.028,.41),'glass')
 for ix,iz in ((-.12,.57),(.10,.62),(-.02,.77)):
  sphere('Visible capsule',(x+ix,y-.365,z+iz),(.085,.04,.085),col if ix!=.1 else 'gold')
 box('Capsule header',(x,y-.355,z+.91),(.65,.08,.13),col)
 cyl('Crank backing',(x,y-.354,z+.30),.125,.04,'silver',axis='Y',vertices=10)
 box('Turn crank',(x,y-.391,z+.30),(.18,.045,.047),'ink')
 box('Dispensing door',(x,y-.35,z+.12),(.36,.04,.14),'ink')
 box('Dispensing lip',(x,y-.395,z+.05),(.40,.12,.025),col)
 box('Coin slot',(x+.235,y-.355,z+.34),(.014,.025,.09),'ink')

def gacha_corner():
 root('NimboGachaCorner')
 box('Capsule kiosk base',(0,0,.07),(4.2,1.8,.14),'stone')
 for x,col in ((-1.05,'coral'),(-.32,'jade'),(.41,'gold')):
  for z in (.14,1.09):gacha(x,.12,z,col)
 box('Empty capsule return',(1.25,.15,.64),(.68,.67,1.05),'coral',.05)
 cyl('Capsule return aperture',(1.25,-.2,.85),.18,.03,'ink',axis='Y',vertices=12)
 for x in (-1.82,1.82):box('Kiosk upright',(x,.48,1.42),(.13,.15,2.7),'wood')
 roof('Kiosk roof',0,.15,2.58,4.1,1.8,.38)
 sign('ガチャポン',0,-.79,2.35,2.75,.47,'coral')

def ramen():
 root('NimboRamenFront')
 box('Shop foundation',(0,.4,.11),(5.8,2.8,.22),'stone')
 box('Shop back wall',(0,1.54,1.57),(5.5,.18,2.7),'cream')
 for x in (-2.66,2.66):box('Shop return wall',(x,.38,1.55),(.18,2.5,2.65),'cream')
 # Left sliding doorway, right serving window with meaningful inset.
 box('Closed entrance shadow',(-1.75,-.84,1.30),(1.30,.08,2.2),'ink')
 for x in (-2.36,-1.74,-1.12):box('Door mullion',(x,-.9,1.3),(.055,.06,2.1),'wood')
 for z in (.38,1.35,2.26):box('Door horizontal rail',(-1.74,-.92,z),(1.3,.07,.055),'wood')
 box('Lower frontage',(1,-.79,.67),(2.85,.16,1.0),'wood')
 box('Shop counter',(1,-1.03,1.19),(3.0,.68,.10),'cream')
 box('Dark serving recess',(1,1.40,1.68),(2.8,.04,.92),'ink')
 for x in (-.4,.99,2.38):box('Front timber post',(x,-.83,1.65),(.10,.12,2.65),'wood')
 for x in (-.13,.57,1.27,1.97):
  box('Noren curtain panel',(x,-1.06,2.27),(.65,.035,.56),'jade')
 text('ラーメン',(.92,-1.088,2.28),.42,'paper',2.5)
 roof('Ramen tiled roof',0,.35,2.89,6.15,3.25,.72)
 sign('麺',-2.66,-1.10,2.37,.55,.69,'coral')
 lantern(2.73,-1.19,2.23)
 for xx in (.18,1.5):
  cyl('Counter stool seat',(xx,-1.42,.64),.25,.1,'coral',vertices=10)
  cyl('Stool leg',(xx,-1.42,.36),.07,.49,'ink')
 planter(-2.55,-1.38,.55)

def lantern_gate():
 root('NimboLanternGate')
 for x in (-2,2):
  box('Gate footing',(x,0,.17),(.60,.65,.34),'stone')
  box('Gate post',(x,0,2.13),(.20,.21,3.95),'wood')
 box('Gate crossbeam',(0,0,3.78),(4.75,.25,.20),'wood')
 roof('Gate tiled cap',0,.1,4.02,4.95,1.15,.37)
 sign('商店街',0,-.17,3.42,2.08,.60,'jade')
 for x in (-1.42,-.72,.72,1.42):lantern(x,-.04,2.64,'coral' if abs(x)>1 else 'paper')
 # No slab or collision across the passage: the arch remains traversable.

def poster(x,y,z,col,label):
 box('Poster frame',(x,y,z),(1.12,.09,1.52),'ink')
 box('Poster paper',(x,y-.055,z),(1.02,.02,1.42),'paper')
 box('Poster header',(x,y-.072,z+.51),(.94,.015,.28),col)
 text(label,(x,y-.086,z+.51),.22,'paper',.88)
 cyl('Poster sun',(x+.19,y-.085,z+.10),.22,.012,col,axis='Y',vertices=10)
 # Stylized mountain/town graphic built as actual thin triangles.
 me=bpy.data.meshes.new('Poster mountain');me.from_pydata([(x-.43,y-.09,z-.32),(x-.08,y-.09,z+.14),(x+.41,y-.09,z-.32)],[],[(0,1,2)]);me.update()
 o=bpy.data.objects.new('Poster mountain',me);bpy.context.collection.objects.link(o);o.parent=CURRENT;o.data.materials.append(MATS['roof']);PARTS[CURRENT.name].append(o)
 for zz,ww in ((-.48,.73),(-.56,.59)):box('Poster copy rule',(x,y-.09,z+zz),(ww,.012,.025),'ink')

def poster_garden():
 root('NimboPosterGarden')
 box('Garden planter wall',(0,.38,.40),(5.1,.85,.8),'stone',.05)
 box('Community notice backing',(0,.64,1.73),(3.9,.22,2.06),'wood')
 for x,col,label in ((-1.22,'coral','まつり'),(0,'jade','ニンボ'),(1.22,'gold','さんぽ')):poster(x,.49,1.78,col,label)
 roof('Notice board cap',0,.63,2.86,4.15,.93,.26)
 for x in (-2.1,2.1):planter(x,.04,.7)
 for xx in (-.7,.7):box('Bench support',(xx,-.65,.31),(.13,.5,.56),'ink')
 for yy in (-.89,-.70,-.51):box('Bench wooden slat',(0,yy,.59),(2.5,.16,.085),'wood')
 box('Bench backrest',(0,-.37,1.00),(2.5,.09,.42),'wood')

def shrine():
 root('NimboShrineCorner')
 box('Shrine lowest step',(0,0,.13),(4.7,3.3,.26),'stone')
 box('Shrine upper step',(0,.22,.36),(4.0,2.7,.2),'stone')
 box('Shrine rear wall',(0,1.2,1.56),(3.25,.18,2.15),'cream')
 for x in (-1.55,1.55):box('Shrine side wall',(x,.43,1.51),(.16,1.6,2.12),'cream')
 box('Recessed shrine door',(0,.78,1.55),(2.55,.08,1.9),'wood')
 for x in (-1.26,-.84,-.42,0,.42,.84,1.26):box('Shrine door slat',(x,.71,1.56),(.06,.08,1.9),'gold')
 for x in (-1.58,1.58):box('Shrine vermilion post',(x,-.44,1.64),(.22,.22,2.38),'coral')
 box('Shrine lintel',(0,-.44,2.74),(3.6,.25,.24),'coral')
 roof('Shrine deep roof',0,.3,2.95,4.5,2.95,.92)
 box('Offering box',(0,-.69,.79),(1.27,.60,.62),'wood')
 for x in (-.45,-.22,0,.22,.45):box('Offering slot bar',(x,-.68,1.12),(.08,.59,.04),'gold')
 beam('Bell rope',(0,-.55,2.71),(0,-.55,1.32),.045,'paper')
 sphere('Shrine bell',(0,-.55,2.56),(.15,.15,.18),'gold')
 for x in (-1.9,1.9):lantern(x,-.65,1.83,'paper')
 sign('雲',0,-1.19,3.16,.58,.58,'jade')

BUILDERS=[vending_corner,gacha_corner,ramen,lantern_gate,poster_garden,shrine]
for f in BUILDERS:f()
bpy.context.view_layer.update()

def bake(name,objs):
 pts=[];ns=[];cols=[];deps=bpy.context.evaluated_depsgraph_get()
 for o in objs:
  eo=o.evaluated_get(deps);me=eo.to_mesh();me.calc_loop_triangles();nt=eo.matrix_world.to_3x3().inverted().transposed()
  for tri in me.loop_triangles:
   color=me.materials[tri.material_index].diffuse_color
   for index,loop in zip(tri.vertices,tri.loops):
    p=eo.matrix_world@me.vertices[index].co;n=(nt@me.corner_normals[loop].vector).normalized()
    pts.append(dict(x=-p.x,y=p.z,z=-p.y));ns.append(dict(x=-n.x,y=n.z,z=-n.y));cols.append(dict(zip('rgba',color)))
  eo.to_mesh_clear()
 return dict(name=name,points=pts,normals=ns,colors=cols,collide=name not in ('NimboLanternGate','NimboPosterGarden'))
props=[bake(n,parts) for n,parts in PARTS.items()]
(ROOT/'japanese-props.json').write_text(json.dumps(dict(props=props),separators=(',',':')))
metrics=[]
for p in props:
 lo={k:min(v[k] for v in p['points']) for k in 'xyz'};hi={k:max(v[k] for v in p['points']) for k in 'xyz'}
 metrics.append(dict(name=p['name'],triangles=len(p['points'])//3,boundsMin=lo,boundsMax=hi,collide=p['collide'],front='+Z'))
(ROOT/'metrics.json').write_text(json.dumps(metrics,indent=2)+'\n')
# Save the authoritative library before introducing presentation-only transforms.
for n in PARTS:bpy.data.objects[n]['front']='Blender -Y / Unity +Z'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'NimboJapaneseKit.blend'))

# Explicit local collision contracts, separate from the existing whole-bounds
# prefab collider. Unity centre/size, metres. Visual overhangs have no collider.
colliders={
 'NimboVendingCorner':[
  {'center':[x,1.20,-.17],'size':[1.25,2.30,.82]} for x in (-.94,.49)],
 'NimboGachaCorner':[
  {'center':[x,1.10,-.12],'size':[.65,1.92,.65]} for x in (-1.05,-.32,.41)],
 'NimboRamenFront':[
  {'center':[0,1.55,-.4],'size':[5.5,2.7,2.5]}],
 'NimboLanternGate':[
  {'center':[x,2.13,0],'size':[.20,3.95,.21]} for x in (-2,2)],
 'NimboPosterGarden':[
  {'center':[0,.4,-.38],'size':[5.1,.8,.85]},
  {'center':[0,.64,.65],'size':[2.5,1.15,.62]}],
 'NimboShrineCorner':[
  {'center':[0,1.55,-.43],'size':[3.3,2.2,1.7]},
  {'center':[0,.79,.69],'size':[1.27,.62,.60]}],
}
for entries in colliders.values():
 for entry in entries:entry['center'][0] *= -1
(ROOT/'collision-contract.json').write_text(json.dumps(dict(units='metres',front='+Z',policy='Solid-only boxes; signs, lanterns, awnings and gate aperture nonblocking. Not yet integrated into runtime.',prefabs=colliders),indent=2)+'\n')

# Small deliberately assembled comparison block. No random placement.
layout=[
 ('NimboRamenFront',-5.9,6.1,0,0),
 ('NimboVendingCorner',.0,5.9,0,0),
 ('NimboGachaCorner',4.9,6.0,0,0),
 ('NimboPosterGarden',-6.0,-6.15,1.2,math.pi),
 ('NimboShrineCorner',-.4,-6.6,1.2,math.pi),
 ('NimboRamenFront',5.7,-6.2,1.2,math.pi),
 ('NimboLanternGate',10.1,5.6,0,math.pi/2),
]
original_parts=dict(PARTS)
for parts in original_parts.values():
 for o in parts:o.hide_render=True;o.hide_set(True)
root('JapanStreetBlock')
blockroot=CURRENT
for name,x,y,z,angle in layout:
 r=bpy.data.objects.new(name+' placed',None);bpy.context.collection.objects.link(r);r.parent=blockroot;r.location=(x,y,z);r.rotation_euler.z=angle
 for original in original_parts[name]:
  copy=original.copy();copy.data=original.data;copy.parent=r;copy.hide_render=False;copy.hide_set(False);bpy.context.collection.objects.link(copy);PARTS['JapanStreetBlock'].append(copy)
# Surface ownership: center combat lane is open from y=-4..4. The raised
# southern sidewalk owns its curb, wall and short access ramp.
box('Open eight metre combat street',(0,0,-.10),(25,8,.20),'roof')
box('North sidewalk',(0,5.9,-.03),(25,3.8,.06),'stone')
box('South terrace rear strip',(0,-7.5,.57),(25,3,1.26),'stone')
box('South terrace front east',(2.25,-5,.57),(20.5,2,1.26),'stone')
box('South retaining edge',(0,-4.02,.58),(25,.12,1.22),'cream')
# Use a broad ramp at west edge to connect street/terrace; no seam at landing.
rampverts=[(-11.7,-3.8,0),(-8.8,-3.8,0),(-8.8,-6,1.2),(-11.7,-6,1.2),(-8.8,-6,0),(-11.7,-6,0)]
mesh=bpy.data.meshes.new('Ramp surface');mesh.from_pydata(rampverts,[],[(0,3,2,1),(1,2,4),(0,5,3),(3,5,4,2),(0,1,4,5)]);mesh.update()
o=bpy.data.objects.new('Short terrace ramp',mesh);bpy.context.collection.objects.link(o);o.parent=CURRENT;o.data.materials.append(MATS['cream']);PARTS['JapanStreetBlock'].append(o)
# Break the retaining-edge obstruction over the ramp by omitting its west end.
edge=bpy.data.objects['South retaining edge'];edge.location.x=2.25;edge.dimensions.x=20.5
for x in range(-10,12,3):box('Lane dash',(x,0,.014),(1.25,.10,.022),'paper')
for xx in (-10.8,-10.2,-9.6,-9.0):box('Crosswalk stripe',(xx,1.0,.014),(.33,5.3,.024),'paper')
# Human-scale mannequin stands exactly2m on street, separate from game geometry.
cyl('Scale marker foot',(0,0,.025),.34,.05,'gold',vertices=12)
box('2m marker torso',(0,0,1.20),(.43,.25,.66),'gold',.04)
for xx in (-.13,.13):beam('2m marker leg',(xx,0,.18),(xx,0,.86),.13,'ink')
for xx in (-1,1):beam('2m marker arm',(xx*.27,0,1.47),(xx*.38,-.03,.96),.12,'ink')
sphere('2m marker head',(0,0,1.80),(.18,.17,.20),'paper')
bpy.context.view_layer.update()
block=bake('JapanStreetBlock',PARTS['JapanStreetBlock']);block['collide']=False
(ROOT/'japanese-block.json').write_text(json.dumps(dict(props=[block]),separators=(',',':')))
(ROOT/'block-layout.json').write_text(json.dumps(dict(layout=[dict(prefab=n,blenderPosition=[x,y,z],yawRadians=a) for n,x,y,z,a in layout],laneWidth=8,terraceRise=1.2,triangleCount=len(block['points'])//3,markerHeight=2),indent=2)+'\n')
# Display each authored cluster on its own neutral square. Library remains saved at origin.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Nimbo study sky');scene.world.color=(.55,.62,.70)
scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast'
world=scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.64,.72,1);world.node_tree.nodes['Background'].inputs[1].default_value=.55
bpy.ops.object.light_add(type='AREA', location=(1,-5,8));key=bpy.context.object;key.data.energy=1300;key.data.shape='DISK';key.data.size=7;key.rotation_euler=(math.radians(22),0,math.radians(-18))
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.lens=50
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.016));plane=bpy.context.object;plane.name='Study floor';floor=bpy.data.materials.new('Study floor');floor.diffuse_color=(.58,.66,.66,1);plane.data.materials.append(floor)

# Reuse camera/light but save the authored block before capturing.
cam.location=(14,-15,10);cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=32
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'NimboJapaneseBlock.blend'))
scene.render.resolution_x=1440;scene.render.resolution_y=960
for label,eye,target,scale in [
 ('street',(14,-15,10),(0,0,1),32),
 ('reverse',(-15,16,11),(0,0,1),32),
 ('player',(-14,-1.7,2.3),(3,1.3,1.7),18),
 ('plan',(0,0,28),(0,0,0),27),
]:
 cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 scene.render.filepath=str(ROOT/f'block-{label}.png');bpy.ops.render.render(write_still=True)
print('BLOCK COMPLETE',len(block['points'])//3,flush=True)

for o in PARTS['JapanStreetBlock']:o.hide_render=True
scene.render.resolution_x=960;scene.render.resolution_y=720;scene.cycles.samples=16
for n,parts in original_parts.items():
 for other,ops in original_parts.items():
  for obj in ops:obj.hide_render=other!=n
 metric=next(m for m in metrics if m['name']==n);w=metric['boundsMax']['x']-metric['boundsMin']['x'];h=metric['boundsMax']['y'];d=metric['boundsMax']['z']-metric['boundsMin']['z']
 cam.data.ortho_scale=max(w*1.3,h*1.65,d*1.7)
 target=Vector((0,0,h*.48))
 for view,pos in [('front',(7,-10,6)),('rear',(-7,10,6))]:
  cam.location=pos;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
  scene.render.filepath=str(ROOT/f'{n}-{view}.png');bpy.ops.render.render(write_still=True)
 print('KIT',n,metric,flush=True)
print('KIT COMPLETE',ROOT,flush=True)
