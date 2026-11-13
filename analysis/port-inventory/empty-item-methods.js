// Prioritize item overrides that are empty in the port but not an obvious
// immediate return in the shipped binary. This is a triage heuristic only.
const fs=require('fs'), path=require('path'), crypto=require('crypto');
const root='/home/your-user/megabonk', inv=require('./inventory.json');
const passive=process.argv.includes('--passives');
const part=passive?'AbilitiesPassive/Implementations':'Items/ItemImplementations';
const base=path.join(root,'unity/OpenBonk/Assets/Scripts/Assembly-CSharp/Assets/Scripts/Inventory__Items__Pickups',part);
const binary='/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
const sha=crypto.createHash('sha256').update(fs.readFileSync(binary)).digest('hex');
if(sha!=='a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw Error('Original binary hash mismatch: '+sha);
const all=require('../il2cpp/script.json').ScriptMethod;
const candidates=[];
// For this SHA-identified ELF, readelf -lW shows the executable LOAD has
// equal file offset and virtual address. These are only short entry-byte tests.
const fd=fs.openSync(binary,'r'); let examined=0, matched=0;
const entities=passive?inv.characters.map(c=>({name:c.name,enabled:c.enabled,factory:c.passiveFactory})):inv.items;
for(const item of entities.filter(i=>i.enabled && i.factory && i.factory!=='ItemNoImplementation')) {
 let file=path.join(base,item.factory+'.cs');
 if(!fs.existsSync(file)) file=path.join(path.dirname(base),item.factory+'.cs');
 if(!fs.existsSync(file)) { candidates.push({item:item.name,kind:'source missing',file}); continue; }
 const src=fs.readFileSync(file,'utf8');
 for(const m of src.matchAll(/(?:public|protected|private)\s+(?:override\s+)?(?:void|bool|float|int|string)\s+(\w+)\s*\([^)]*\)\s*\{\s*(?:return\s+(?:false|true|0|null);\s*)?\}/g)) {
  examined++;
  const name=m[1];
  const match=all.filter(x=>x.Name.endsWith('.'+item.factory+'$$'+name));
  if(match.length!==1) {candidates.push({item:item.name,implementation:item.factory,method:name,kind:'original lookup ambiguous',count:match.length}); continue;}
  const addr=match[0].Address;
  matched++; if(addr<0x1d35000 || addr>=0x4e2d210) throw Error('Address outside executable LOAD');
  const bytes=Buffer.alloc(4); if(fs.readSync(fd,bytes,0,4,addr)!==4)throw Error('Short read');
  const trivial=bytes[0]===0xc3 || (bytes[0]===0x31 && bytes[1]===0xc0 && bytes[2]===0xc3) || (bytes[0]===0xb0 && bytes[1]===0x01 && bytes[2]===0xc3);
  if(!trivial) candidates.push({item:item.name,implementation:item.factory,method:name,kind:'inspect original',rva:'0x'+addr.toString(16),firstBytes:bytes.toString('hex')});
  else {
   const value=m[0].match(/return\s+(false|true|0|null);/)?.[1] ?? 'void';
   const original=bytes[0]===0xc3?'void':bytes[0]===0xb0?'true':'zero';
   if((value==='void'&&original!=='void')||(value==='true'&&original!=='true')||(['false','0','null'].includes(value)&&original!=='zero'))
    candidates.push({item:item.name,implementation:item.factory,method:name,kind:'different trivial return',rva:'0x'+addr.toString(16),source:value,original});
  }
 }
}
fs.writeFileSync(path.join(root,'analysis/port-inventory/'+(passive?'passive-triage.json':'item-triage.json')),JSON.stringify(candidates,null,2)+'\n');
for(const c of candidates) console.log(`${c.item}\t${c.implementation ?? ''}\t${c.method ?? ''}\t${c.kind}\t${c.rva ?? ''}`);
console.log(`Candidates: ${candidates.length}. Empty source and non-immediate native return are not proof of missing gameplay; original may be a trampoline, stateless method, or work elsewhere.`);
console.log(`Empty/trivial methods examined: ${examined}; unambiguous original methods checked: ${matched}. Immediate void, false/zero/null, and true entry returns compared; other code and inherited behavior remain unexamined.`);
fs.closeSync(fd);
