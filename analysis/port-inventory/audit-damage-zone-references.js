// Bounded byte scan: E8 bytes may occur inside other instructions or data.
// Indirect/virtual calls and runtime-created objects are outside its scope.
const fs = require('fs');
const crypto = require('crypto');
const source = '/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
const binary = fs.readFileSync(source);
const digest = crypto.createHash('sha256').update(binary).digest('hex');
if (digest !== 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw Error('Unexpected original binary');
const methods = JSON.parse(fs.readFileSync('analysis/il2cpp/script.json', 'utf8')).ScriptMethod;
const selected = methods.filter(m => /^(DamageZone\$\$(Set|UpdateSize|FixedUpdate)|StatContainer\$\$(SetStat|Update))$/.test(m.Name));
if (selected.length !== 5) throw Error('Unexpected target list');
const hits = new Map(selected.map(m => [m.Address, []]));
for (let pos = 0x2162990; pos + 5 <= 0x2162990 + 0x2cc3ab0; pos++) {
  if (binary[pos] !== 0xe8) continue;
  const target = pos + 5 + binary.readInt32LE(pos + 1);
  if (hits.has(target)) hits.get(target).push('0x' + pos.toString(16));
}
console.log('GameAssembly SHA256', digest);
for (const m of selected) console.log(m.Name, 'target=0x' + m.Address.toString(16), 'raw_e8_candidates=' + hits.get(m.Address).length, hits.get(m.Address).join(' '));
console.log('Scope: raw E8 candidates only; excludes indirect/virtual dispatch, asset references, dynamic loading and execution.');
