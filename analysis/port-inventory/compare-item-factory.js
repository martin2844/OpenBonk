#!/usr/bin/env node
const fs = require('fs'), crypto = require('crypto');
const basePath = 'unity/OpenBonk/Assets/Scripts/Assembly-CSharp/Assets/Scripts/Inventory__Items__Pickups/Items/';
const binary = fs.readFileSync('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so');
const hash = crypto.createHash('sha256').update(binary).digest('hex');
if (hash !== 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw new Error('Unexpected binary hash');
const asm = fs.readFileSync('analysis/recovered/ItemFactory-CreateItem.asm', 'utf8').split('\n');
const indices = new Map();
for (let i = 0; i < asm.length; i++) { const match = asm[i].match(/^\s*([0-9a-f]+):/); if (match) indices.set(parseInt(match[1],16), i); }
const relocs = new Map();
for (const m of fs.readFileSync('analysis/il2cpp/elf-relocations.txt','utf8').matchAll(/^([0-9a-f]+)\s+\S+\s+R_X86_64_RELATIVE\s+([0-9a-f]+)/gm)) relocs.set(parseInt(m[1],16),parseInt(m[2],16));
const types = new Map(require('../il2cpp/script.json').ScriptMetadata.map(x=>[x.Address,x.Name]));
const enums = new Map();
for (const m of fs.readFileSync(basePath+'EItem.cs','utf8').matchAll(/\b(\w+)\s*=\s*(\d+)/g)) enums.set(m[1],+m[2]);
const cases = [...fs.readFileSync(basePath+'ItemFactory.cs','utf8').matchAll(/case EItem\.(\w+):\s*return new (\w+)\(inventory\)/g)];
let fail=0, inline=0, checked=0, typeChecked=0;
for (const [, name, cls] of cases) {
 const index=enums.get(name); if(index===undefined || index>0x57) throw new Error('Unknown enum '+name);
 const table=0xb6f7c8, target=table+binary.readInt32LE(table+index*4), pos=indices.get(target);
 if(pos===undefined) throw new Error(`Missing disassembly at ${target.toString(16)}`);
 const lines=[]; for (const line of asm.slice(pos,pos+30)) { lines.push(line); if (/\b(?:jmp|ret)\b/.test(line)) break; }
 const pointer=lines[0].match(/#\s+([0-9a-f]+)/); const metadata=pointer && relocs.get(parseInt(pointer[1],16));
 const typeName=metadata && types.get(metadata); const typeMatch=typeName && typeName.match(/Items\.(?:ItemImplementations\.)?(\w+)_TypeInfo$/);
 if (!typeMatch) { console.log(`TYPE UNRESOLVED ${name} #${index}`); fail++; }
 else { typeChecked++; if(typeMatch[1]!==cls) {console.log(`TYPE MISMATCH ${name}: original=${typeMatch[1]} port=${cls}`);fail++;} }
 const constructor=lines.find(x=>/ItemImplementations\.\w+\$\$\.ctor/.test(x));
 if(!constructor) {console.log(`INLINED/UNRESOLVED ${name} #${index} target=0x${target.toString(16)}`);inline++;continue;}
 checked++;
 const original=constructor.match(/ItemImplementations\.(\w+)\$\$\.ctor/)[1];
 if(original!==cls) {console.log(`MISMATCH ${name} #${index}: original=${original} port=${cls}`);fail++;}
}
console.log(`Cases=${cases.length}; type metadata checked=${typeChecked}; direct constructors checked=${checked}; inlined or unresolved constructors=${inline}; mismatches or unknown types=${fail}. Class selection only, not item behavior.`);
if(fail) process.exitCode=1;
