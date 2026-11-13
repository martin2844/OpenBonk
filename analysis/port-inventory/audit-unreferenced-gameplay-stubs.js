// Bounded raw x86 direct-call scan. An E8 byte can also occur inside another
// instruction or data, and indirect/virtual calls are not covered.
const fs = require('fs'), crypto = require('crypto');
const binary = fs.readFileSync('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so');
const hash = crypto.createHash('sha256').update(binary).digest('hex');
if (hash !== 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw Error('Unexpected original binary');
const methods = JSON.parse(fs.readFileSync('analysis/il2cpp/script.json', 'utf8')).ScriptMethod;
const selected = methods.filter(m => /DamagePlayerTrigger\$\$Set$|ItemProjectileOrb\$\$(Init|Step|ProjectileDone)$/.test(m.Name));
if (selected.length !== 4) throw Error('Unexpected target list');
const calls = new Map(selected.map(m => [m.Address, []]));
// IL2CPP ELF section's file offset equals its virtual address in this binary.
for (let pos = 0x2162990; pos + 5 <= 0x2162990 + 0x2cc3ab0; pos++) {
  if (binary[pos] !== 0xe8) continue;
  const target = pos + 5 + binary.readInt32LE(pos + 1);
  if (calls.has(target)) calls.get(target).push('0x' + pos.toString(16));
}
console.log('GameAssembly SHA256', hash);
for (const m of selected) console.log(m.Name, 'target=0x' + m.Address.toString(16), 'raw_e8_candidates=' + calls.get(m.Address).length, calls.get(m.Address).join(' '));
console.log('Scope: raw E8 byte candidates in IL2CPP section; not instruction decoding, indirect calls, virtual dispatch, asset references, or proof of runtime reachability.');
