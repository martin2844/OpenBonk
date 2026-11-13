// Compare the original x86-64 pickup-effect switch against the C# reconstruction.
// This is a static, version-specific audit, not a rendering or runtime test.
const fs = require('fs');
const crypto = require('crypto');
const original = '/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
const source = 'unity/OpenBonk/Assets/Scripts/Assembly-CSharp/EffectManager.cs';
const bytes = fs.readFileSync(original);
const hash = crypto.createHash('sha256').update(bytes).digest('hex');
if (hash !== 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw Error('Unexpected original binary');
const input = fs.readFileSync(source, 'utf8');
const start = input.indexOf('private void OnPickup(Pickup pickup)');
if (start < 0) throw Error('OnPickup missing');
const method = input.slice(start);
const names = ['Health', 'Nuke', 'Time', 'Shield', 'Rage', 'Haste', 'Stonks', 'Magnet'];
// Field offsets are from the original IL2CPP layout of EffectManager.
const fields = {0x20:'nukePickup', 0x28:'magnetPickup', 0x30:'hastePickup', 0x38:'ragePickup', 0x40:'shieldPickup', 0x48:'stonksPickup', 0x50:'healthPickup'};
const table = 0xb6fe1c;
for (let i=0; i<names.length; i++) {
  const name = names[i], target = table + bytes.readInt32LE(table + i*4);
  const clause = method.match(new RegExp('case EPickup\\.' + name + ':([^\\n]*)'));
  if (target === 0x233d6ae) {
    if (clause) throw Error(`${name}: original returns without creating a pickup effect`);
    console.log(`${name}: no EffectManager pickup prefab (both)`);
    continue;
  }
  const op = bytes.subarray(target, target+3).toString('hex');
  if (op !== '4d8b76') throw Error(`${name}: unexpected instruction at ${target.toString(16)}`);
  const field = fields[bytes[target+3]];
  const status = target === 0x233d6c8 || target === 0x233d6ce || target === 0x233d6d4;
  if (!field || !clause || !clause[1].includes(`prefab = ${field}`) || /statusEffect\s*=\s*true/.test(clause[1]) !== status)
    throw Error(`${name}: port differs from original ${field}, status=${status}`);
  console.log(`${name}: ${field}, statusEffect=${status} (both)`);
}
if (!method.includes('if (statusEffect) effect.GetComponent<StatusEffectPickup>()?.Set();')) throw Error('Port status initialization missing');
console.log('PASS: original jump table and port pickup-effect mapping agree');
