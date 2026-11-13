import bpy, os
bpy.ops.wm.open_mainfile(filepath=os.path.abspath('mods/pikachu/study/pikachu-movement.blend'))
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=8; scene.render.resolution_percentage=70
arm=bpy.data.objects['Armature']; board=bpy.data.objects['OpenBonkSurfboard']
for name,frame in [('OpenBonk walk study',1),('OpenBonk walk study',13),('OpenBonk run',1),('OpenBonk run',9),('OpenBonk jump',12),('OpenBonk surf',1),('OpenBonk surf',13)]:
 arm.animation_data.action=bpy.data.actions[name]; scene.frame_set(frame); board.hide_render=name!='OpenBonk surf'; scene.render.filepath=os.path.abspath('mods/pikachu/study/'+name.replace(' ','-')+'-'+str(frame)+'.png'); bpy.ops.render.render(write_still=True)
