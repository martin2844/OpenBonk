// Bounded walk from the 24 name-matched original enemy attack prefab roots.
// Includes prefab/material references only, not scripts or dynamic loads.
const fs = require('fs'), cp = require('child_process'), path = require('path');
const project = 'unity/OpenBonk';
const assets = project + '/Assets', cache = project + '/Library/PackageCache';
const roots = [assets, ...(fs.existsSync(cache) ? [cache] : [])];
const index = new Map();
for (const meta of cp.execFileSync('rg', ['--files', ...roots, '-g', '*.meta'], {encoding:'utf8', maxBuffer:50e6}).trim().split('\n')) {
  const m = fs.readFileSync(meta, 'utf8').match(/^guid: ([a-f0-9]{32})$/m);
  if (m) index.set(m[1], meta.slice(0,-5));
}
const evidence = fs.readFileSync('build/pc-speed-isolated/enemy-attack-root-components-20261108.log','utf8');
const names = [...evidence.matchAll(/^MATCH (\S+) pc_candidates 1 android_candidates 1 /gm)].map(m => m[1]);
if (names.length !== 24 || new Set(names).size !== 24) throw new Error('Expected 24 distinct matched attack roots');
const textures = new Map(), missing = new Set();
for (const name of names) {
  const queue = [`${assets}/GameObject/${name}.prefab`], seen = new Set();
  while (queue.length) {
    const file = queue.pop(); if (seen.has(file)) continue; seen.add(file);
    if (!fs.existsSync(file)) { missing.add(file); continue; }
    const ext = path.extname(file).toLowerCase();
    if (!['.prefab','.mat'].includes(ext)) continue;
    for (const m of fs.readFileSync(file,'utf8').matchAll(/guid: ([a-f0-9]{32})/g)) {
      const target = index.get(m[1]);
      if (!target) { if (!m[1].startsWith('0000000000000000')) missing.add(m[1]); continue; }
      const e = path.extname(target).toLowerCase();
      if (['.prefab','.mat'].includes(e)) queue.push(target);
      else if (e === '.png' && ext === '.mat') {
        const item = textures.get(target) || {path: target, roots: new Set(), materials: new Set()};
        item.roots.add(name); item.materials.add(path.basename(file)); textures.set(target,item);
      }
    }
  }
}
const out = [...textures.values()].map(x => ({name:path.basename(x.path,'.png'),path:x.path,roots:[...x.roots].sort(),materials:[...x.materials].sort()})).sort((a,b)=>a.name.localeCompare(b.name));
fs.writeFileSync('analysis/port-inventory/enemy-attack-textures.json', JSON.stringify(out,null,2)+'\n');
fs.writeFileSync('analysis/port-inventory/enemy-attack-texture-names.csv','icon_name\n'+out.map(x=>x.name).join('\n')+'\n');
console.log(`roots=${names.length} textures=${out.length} unresolved=${missing.size}`);
for (const x of out) console.log(`${x.name}\t${x.roots.join(',')}\t${x.materials.join(',')}`);
for (const x of missing) console.log('UNRESOLVED '+x);
