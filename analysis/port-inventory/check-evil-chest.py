import UnityPy
from pathlib import Path

original = Path.home() / '.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data/sharedassets2.assets'
hits = []
for obj in UnityPy.load(str(original)).objects:
    if obj.type.name != 'Material':
        continue
    material = obj.read()
    if material.m_Name != 'm_evilChest':
        continue
    hits.append(obj.path_id)
    props = material.m_SavedProperties
    print(f'original material path ID {obj.path_id}; shader file ID {material.m_Shader.m_FileID}, path ID {material.m_Shader.m_PathID}; property counts: colors={len(props.m_Colors)}, floats={len(props.m_Floats)}, textures={len(props.m_TexEnvs)}')
    assert material.m_Shader.m_PathID == 0
assert len(hits) == 1, hits
print('The checked original material also has no shader. This does not prove the corrupt chest renders correctly or how the original handles this material at runtime.')
