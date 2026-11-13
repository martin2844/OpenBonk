// Bounded GUID walk: weapon attack prefabs -> referenced prefabs/materials -> PNG textures.
const fs=require('fs'),cp=require('child_process'),path=require('path');
const root='unity/OpenBonk/Assets';
const metas=cp.execFileSync('rg',['--files',root,'-g','*.meta'],{encoding:'utf8',maxBuffer:50e6}).trim().split('\n'),index=new Map();
for(const p of metas){const m=fs.readFileSync(p,'utf8').match(/^guid: ([a-f0-9]{32})$/m);if(m)index.set(m[1],p.slice(0,-5));}
const weapons=JSON.parse(fs.readFileSync('analysis/port-inventory/inventory.json','utf8')).weapons;
const result=new Map(),missing=new Set();
for(const w of weapons){const q=[path.join(root,'GameObject',w.attack)],seen=new Set();while(q.length){const p=q.pop();if(seen.has(p))continue;seen.add(p);if(!fs.existsSync(p)){missing.add(p);continue;}const ext=path.extname(p).toLowerCase();if(!['.prefab','.mat'].includes(ext))continue;let s=fs.readFileSync(p,'utf8');for(const m of s.matchAll(/guid: ([a-f0-9]{32})/g)){let target=index.get(m[1]);if(!target){if(!m[1].startsWith('0000000000000000'))missing.add(m[1]);continue;}let e=path.extname(target).toLowerCase();if(['.prefab','.mat'].includes(e))q.push(target);else if(e==='.png'&&ext==='.mat'){let x=result.get(target)||{path:target,weapons:new Set(),materials:new Set()};x.weapons.add(w.name);x.materials.add(path.basename(p));result.set(target,x);}}}}
const out=[...result.values()].map(x=>{let buf=fs.readFileSync(x.path);return {name:path.basename(x.path,'.png'),path:x.path,width:buf.readUInt32BE(16),height:buf.readUInt32BE(20),weapons:[...x.weapons].sort(),materials:[...x.materials].sort()};}).sort((a,b)=>a.name.localeCompare(b.name));
fs.writeFileSync('analysis/port-inventory/weapon-effect-textures.json',JSON.stringify(out,null,2)+'\n');
console.log(`Weapons ${weapons.length}, textures ${out.length}, missing GUIDs or files ${missing.size}`);
for(const x of out)console.log(`${x.name}\t${x.width}x${x.height}\t${x.weapons.length} weapons\t${x.materials.join(',')}`);
for(const m of missing)console.log('UNRESOLVED',m);
