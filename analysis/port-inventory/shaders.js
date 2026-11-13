// Inventory exported material-to-shader links. This does not establish use in
// a scene, visual correctness, completeness of shader passes, or build output.
const fs=require('fs'), path=require('path');
const root='/home/your-user/megabonk', assets=path.join(root,'unity/OpenBonk/Assets');
const shaderDir=path.join(assets,'Shader'), materialDir=path.join(assets,'Material');
const shaders=new Map();
for(const f of fs.readdirSync(shaderDir).filter(x=>x.endsWith('.shader.meta'))) {
 const meta=fs.readFileSync(path.join(shaderDir,f),'utf8'), guid=meta.match(/^guid: ([a-f0-9]+)/m)?.[1];
 if(!guid) continue;
 const shaderFile=path.join(shaderDir,f.slice(0,-5));
 const text=fs.readFileSync(shaderFile,'utf8');
 shaders.set(guid,{name:text.match(/Shader\s+"([^"]+)"/)?.[1]??null,asset:path.relative(root,shaderFile),materials:[]});
}
const unresolved=[];
for(const f of fs.readdirSync(materialDir).filter(x=>x.endsWith('.mat'))) {
 const text=fs.readFileSync(path.join(materialDir,f),'utf8'), ref=text.match(/^  m_Shader: (.+)$/m)?.[1];
 const guid=ref?.match(/guid: ([a-f0-9]+)/)?.[1], s=shaders.get(guid);
 if(s)s.materials.push(f); else unresolved.push({material:f,ref:ref??null,kind:ref==='{fileID: 0}'?'null':guid==='0000000000000000f000000000000000'?'unity-builtin':'other'});
}
const used=[...shaders.values()].filter(x=>x.materials.length).sort((a,b)=>b.materials.length-a.materials.length);
fs.writeFileSync(path.join(root,'analysis/port-inventory/shaders.json'),JSON.stringify({scope:'exported Assets/Material and Assets/Shader only',used,unresolved},null,2)+'\n');
console.log(`Materials mapped to exported shader assets: ${used.reduce((a,s)=>a+s.materials.length,0)}; distinct shaders: ${used.length}; other/unresolved references: ${unresolved.length}`);
console.log('Most referenced: '+used.slice(0,8).map(s=>`${s.name} (${s.materials.length})`).join(', '));
console.log('Other references: '+JSON.stringify(unresolved.reduce((a,x)=>(a[x.kind]=(a[x.kind]||0)+1,a),{})));
console.log('LIMIT: exported material directory only; no scene reachability, built-in or package shader resolution, visual parity, pass coverage, or runtime test.');
