#!/usr/bin/env node
// Candidate-only scan of immediate writes in retained original constructor listings.
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const binary = fs.readFileSync('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so');
if (crypto.createHash('sha256').update(binary).digest('hex') !== 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw Error('unexpected original binary');
const dump = fs.readFileSync('analysis/il2cpp/dump.cs', 'utf8');
const root = 'unity/OpenBonk/Assets/Scripts/Assembly-CSharp/Assets/Scripts/Inventory__Items__Pickups/Items/ItemImplementations';
let checked = 0, discrepancies = 0;
for (const file of fs.readdirSync('analysis/recovered').filter(s => /^Item\w+-\.ctor\.asm$/.test(s))) {
  const type = file.split('-')[0], source = path.join(root, type+'.cs');
  if (!fs.existsSync(source)) continue;
  const block = dump.match(new RegExp('public class '+type+' : [^\\n]+\\n\\{\\n\\s*// Fields([\\s\\S]*?)\\n\\s*// Methods'));
  if (!block) continue;
  const fields = new Map();
  for (const m of block[1].matchAll(/(?:public|private|protected)\s+(float|int|bool)\s+(\w+);\s*\/\/\s*0x([\da-f]+)/gi)) fields.set(parseInt(m[3],16),{type:m[1],name:m[2]});
  const src = fs.readFileSync(source,'utf8'); let asm = fs.readFileSync(path.join('analysis/recovered',file),'utf8');
  const owner = asm.match(/mov\s+(r(?:bx|1[2345])),rdi\b/); if (!owner) continue;
  // The compiler also combines adjacent 32-bit fields into a single immediate store.
  const packed = new RegExp('movabs\\s+(r\\w+),0x([0-9a-f]+)[^\\n]*\\n[^\\n]*mov\\s+QWORD PTR \\['+owner[1]+'\\+0x([0-9a-f]+)\\],\\1\\b','gi');
  let unpacked = '';
  for (const m of asm.matchAll(packed)) {
    const bits=BigInt('0x'+m[2]), offset=parseInt(m[3],16);
    unpacked+=`\nmov DWORD PTR [${owner[1]}+0x${offset.toString(16)}],0x${(bits & 0xffffffffn).toString(16)}`;
    unpacked+=`\nmov DWORD PTR [${owner[1]}+0x${(offset+4).toString(16)}],0x${((bits >> 32n) & 0xffffffffn).toString(16)}`;
  }
  const vectors = new RegExp('mov(?:ap|up)s\\s+(xmm\\d+),XMMWORD PTR \\[rip[^\\]]*\\][^\\n]*#\\s*([0-9a-f]+)[^\\n]*\\n[^\\n]*mov(?:ap|up)s\\s+XMMWORD PTR \\['+owner[1]+'\\+0x([0-9a-f]+)\\],\\1\\b','gi');
  for (const m of asm.matchAll(vectors)) {
    const address=parseInt(m[2],16), offset=parseInt(m[3],16);
    for(let i=0;i<16;i+=4) unpacked+=`\nmov DWORD PTR [${owner[1]}+0x${(offset+i).toString(16)}],0x${binary.readUInt32LE(address+i).toString(16)}`;
  }
  asm += unpacked;
  const pattern = new RegExp('mov\\s+(DWORD|BYTE) PTR \\['+owner[1]+'\\+0x([0-9a-f]+)\\],0x([0-9a-f]+)','gi');
  for (const m of asm.matchAll(pattern)) {
    const field=fields.get(parseInt(m[2],16)); if(!field || (field.type==='bool') !== (m[1].toUpperCase()==='BYTE')) continue;
    const decl=src.match(new RegExp('\\b(?:public|private|protected)\\s+'+field.type+'\\s+'+field.name+'\\s*(?:=\\s*(-?(?:\\d+\\.?\\d*|\\.\\d+)(?:f)?|true|false))?\\s*;'));
    if (!decl) continue;
    const val = BigInt('0x'+m[3]); let expected;
    if (field.type==='float') {let b=Buffer.alloc(4);b.writeUInt32LE(Number(val & 0xffffffffn));expected=b.readFloatLE();}
    else if(field.type==='bool') expected=val!==0n; else expected=Number(BigInt.asIntN(32,val));
    // A declaration without an initializer may be assigned inside the constructor.
    const constructor=src.match(new RegExp('public\\s+'+type+'\\s*\\([^)]*\\)\\s*(?::\\s*base\\([^)]*\\))?\\s*\\{([^{}]*)\\}'));
    const assignment=constructor && constructor[1].match(new RegExp('\\b(?:this\\.)?'+field.name+'\\s*=\\s*(-?(?:\\d+\\.?\\d*|\\.\\d+)f?|true|false)\\s*;'));
    const literal=assignment ? assignment[1] : decl[1];
    const actual=field.type==='bool' ? literal==='true' : Number((literal||'0').replace(/f$/,''));
    if (field.type==='float' ? Object.is(Math.fround(actual),Math.fround(expected)) : actual===expected) checked++;
    else { discrepancies++; console.log(`CANDIDATE ${type}.${field.name}: original immediate=${expected}, source declaration=${actual} [${file}]`); }
  }
}
console.log(`Compared ${checked+discrepancies} scalar writes including adjacent packed and contiguous vector constants; ${discrepancies} candidates. Not a complete constructor audit: other store forms, called constructors, later writes, nonliteral initializers and register reassignment are excluded.`);
