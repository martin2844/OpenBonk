// Check the specific original Vector3 static-field reference against these
// four recovered methods and their reconstructed constant return bodies.
// This does not cover any other projectile behavior or runtime reachability.
const fs = require('fs');
const cp = require('child_process');
const crypto = require('crypto');
const binary = '/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
const expected = 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1';
const hash = crypto.createHash('sha256').update(fs.readFileSync(binary)).digest('hex');
if (hash !== expected) throw Error('Unexpected original binary: ' + hash);
const relocation = cp.execFileSync('readelf', ['-rW', binary], {encoding:'utf8', maxBuffer:128*1024*1024});
if (!/^000000000506c918\s+\S+\s+R_X86_64_RELATIVE\s+515e460\s*$/m.test(relocation)) throw Error('Vector3 relocation differs');
const metadata = JSON.parse(fs.readFileSync('analysis/il2cpp/script.json', 'utf8')).ScriptMetadata;
if (!metadata.some(x => x.Address === 0x515e460 && x.Name === 'UnityEngine.Vector3_TypeInfo')) throw Error('Vector3 metadata differs');
const header = fs.readFileSync('analysis/il2cpp/il2cpp.h', 'utf8');
if (!/struct UnityEngine_Vector3_StaticFields \{\s*struct UnityEngine_Vector3_o zeroVector;/.test(header)) throw Error('Vector3 static layout differs');
const root = 'unity/OpenBonk/Assets/Scripts/Assembly-CSharp/';
for (const name of ['ProjectileLightningBolt', 'ProjectileBloodMagic', 'ProjectileFirefield', 'ProjectileMines']) {
  const asm = fs.readFileSync('analysis/recovered/' + name + '-GetMovementDirection.asm', 'utf8');
  const src = fs.readFileSync(root + name + '.cs', 'utf8');
  if (!/506c918/.test(asm) || !/mov\s+rax,QWORD PTR \[rax\+0xb8\]/.test(asm) || !/movsd\s+xmm0,QWORD PTR \[rax\]/.test(asm) || !/movss\s+xmm1,DWORD PTR \[rax\+0x8\]/.test(asm)) throw Error(name + ': unexpected static field load');
  if (!/GetMovementDirection\s*\([^)]*\)\s*\{\s*return (?:Vector3\.zero|default\(Vector3\));\s*\}/.test(src)) throw Error(name + ': source differs');
  console.log(name + ': original static Vector3.zero load matches port');
}
console.log('PASS: limited static comparison; original SHA-256 ' + hash);
