// Bounded heuristic for components directly serialized on weapon attack prefabs.
// Not a scan of referenced projectiles, dynamically attached scripts, inheritance,
// nontrivial implementations, or all gameplay methods.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root='/home/your-user/megabonk', assets=path.join(root,'unity/OpenBonk/Assets');
const bin='/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
if(crypto.createHash('sha256').update(fs.readFileSync(bin)).digest('hex')!=='a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1')throw Error('Unexpected original binary');
const methods=require('../il2cpp/script.json').ScriptMethod, inv=require('./inventory.json'), meta=new Map();
function walk(dir){ for(const entry of fs.readdirSync(dir,{withFileTypes:true})) {const f=path.join(dir,entry.name);if(entry.isDirectory())walk(f);else if(entry.name.endsWith('.cs.meta')){const m=fs.readFileSync(f,'utf8').match(/^guid: ([a-f0-9]+)/m);if(m)meta.set(m[1],f.slice(0,-5));}} }
walk(path.join(assets,'Scripts'));
const fd=fs.openSync(bin,'r'), all=[], summary=[];
for(const weapon of inv.weapons.filter(x=>x.enabled)) {
 const prefab=path.join(assets,'GameObject',weapon.attack), data=fs.readFileSync(prefab,'utf8');
 const guids=[...new Set([...data.matchAll(/m_Script: \{fileID: 11500000, guid: ([a-f0-9]+)/g)].map(x=>x[1]))];
 const row={weapon:weapon.name,prefab:weapon.attack,components:[],unknown:[]};
 for(const guid of guids) {
  const f=meta.get(guid);if(!f){row.unknown.push(guid);continue;}
  const source=fs.readFileSync(f,'utf8'), name=path.basename(f,'.cs');
  row.components.push({name,file:path.relative(root,f)});
  for(const m of source.matchAll(/(?:public|protected|private)\s+(?:(?:override|virtual|new|static|sealed)\s+)*(?:void|bool|int|float|string)\s+(\w+)\s*\([^)]*\)\s*\{\s*(?:return\s+(?:false|true|0|null);\s*)?\}/g)) {
   const method=m[1], targets=methods.filter(x=>x.Name.endsWith('.'+name+'$$'+method) || x.Name===name+'$$'+method);
   if(targets.length!==1){all.push({weapon:weapon.name,component:name,method,kind:'unresolved original',count:targets.length});continue;}
   const addr=targets[0].Address;
   if(addr<0x1d35000||addr>=0x4e2d210)throw Error('outside executable LOAD');
   const b=Buffer.alloc(4);fs.readSync(fd,b,0,4,addr);
   const native=b[0]===0xc3?'void':b[0]===0x31&&b[1]===0xc0&&b[2]===0xc3?'zero':b[0]===0xb0&&b[1]===1&&b[2]===0xc3?'true':'nontrivial';
   const value=m[0].match(/return\s+(false|true|0|null);/)?.[1]??'void';
   if(native==='nontrivial'||(value==='void'&&native!=='void')||(value==='true'&&native!=='true')||(['false','0','null'].includes(value)&&native!=='zero'))
    all.push({weapon:weapon.name,component:name,method,kind:'inspect discrepancy',source:value,native,rva:'0x'+addr.toString(16),bytes:b.toString('hex')});
  }
 }
 summary.push(row);
}
fs.closeSync(fd); fs.writeFileSync(path.join(root,'analysis/port-inventory/weapon-components.json'),JSON.stringify({summary,candidates:all},null,2)+'\n');
for(const c of all)console.log(`${c.weapon}\t${c.component}\t${c.method}\t${c.kind}\t${c.rva??''}`);
console.log(`Weapons: ${summary.length}; unresolved script GUIDs: ${summary.reduce((n,x)=>n+x.unknown.length,0)}; candidates: ${all.length}.`);
console.log('LIMIT: direct serialized scripts only, excludes referenced prefabs, inherited methods and nontrivial source; candidates require manual inspection.');
