// Enumerate textures referenced directly by materials in the serialized
// MainMenu enemy-data list. Does not follow runtime material swaps or effects.
const fs = require('fs'), cp = require('child_process'), path = require('path');
const assets = 'unity/OpenBonk/Assets';
const index = new Map();
for (const meta of cp.execFileSync('rg',['--files',assets,'-g','*.meta'],{encoding:'utf8',maxBuffer:50e6}).trim().split('\n')) {
  const m = fs.readFileSync(meta,'utf8').match(/^guid: ([a-f0-9]{32})$/m);
  if (m) index.set(m[1], meta.slice(0,-5));
}
const scene = fs.readFileSync(`${assets}/ScenesAndroid/MainMenu.unity`,'utf8');
const block = scene.match(/^  unsortedEnemies:\n((?:  - .*\n)+)/m);
if (!block) throw Error('enemy list not found');
const refs = [...block[1].matchAll(/guid: ([0-9a-f]{32})/g)].map(m=>m[1]);
if (refs.length !== 53) throw Error(`expected 53 enemy references, got ${refs.length}`);
const textures = new Map(), unresolved = [];
for (const guid of refs) {
  const enemy = index.get(guid);
  if (!enemy) { unresolved.push('enemy '+guid); continue; }
  const data = fs.readFileSync(enemy,'utf8');
  const material = data.match(/^  material: \{fileID: (\d+)(?:, guid: ([0-9a-f]{32}), type: \d+)?\}/m);
  if (!material) { unresolved.push('material field '+enemy); continue; }
  if (material[1] === '0') continue;
  const mat = index.get(material[2]);
  if (!mat) { unresolved.push('material '+material[2]); continue; }
  for (const match of fs.readFileSync(mat,'utf8').matchAll(/m_Texture: \{fileID: \d+, guid: ([0-9a-f]{32}), type: \d+\}/g)) {
    const tex = index.get(match[1]);
    if (!tex) { unresolved.push('texture '+match[1]); continue; }
    if (path.extname(tex).toLowerCase() !== '.png') { unresolved.push('non-PNG texture '+tex); continue; }
    const value = textures.get(tex) || {path:tex,enemies:new Set(),materials:new Set()};
    value.enemies.add(path.basename(enemy,'.asset')); value.materials.add(path.basename(mat,'.mat')); textures.set(tex,value);
  }
}
const result = [...textures.values()].map(x=>({name:path.basename(x.path,'.png'),path:x.path,enemies:[...x.enemies].sort(),materials:[...x.materials].sort()})).sort((a,b)=>a.name.localeCompare(b.name));
if (new Set(result.map(x=>x.name)).size !== result.length) throw Error('duplicate names require identity resolution');
fs.writeFileSync('analysis/port-inventory/enemy-material-textures.json',JSON.stringify(result,null,2)+'\n');
fs.writeFileSync('analysis/port-inventory/enemy-material-texture-names.csv','icon_name\n'+result.map(x=>x.name).join('\n')+'\n');
console.log(`enemy_entries=${refs.length} textures=${result.length} unresolved=${unresolved.length}`);
for (const x of result) console.log(`${x.name}\t${x.enemies.join(',')}\t${x.materials.join(',')}`);
for (const x of unresolved) console.log('UNRESOLVED '+x);
