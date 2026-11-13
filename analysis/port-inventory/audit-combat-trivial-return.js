// Bounded heuristic: simple bool/float/int returns on named combat-like classes
// compared with original IL2CPP entry bytes and exported prefab/scene references.
const fs = require('fs'), cp = require('child_process');
const root = 'unity/OpenBonk/Assets';
const methods = JSON.parse(fs.readFileSync('analysis/il2cpp/script.json')).ScriptMethod;
const files = cp.execSync(`rg --files ${root}/Scripts/Assembly-CSharp | rg '\\.cs$'`, {encoding:'utf8'}).trim().split('\n');
const classes = {};
for (const path of files) {
  const source = fs.readFileSync(path, 'utf8');
  const cls = source.match(/\bclass (\w+)/);
  const meta = fs.existsSync(path + '.meta') && fs.readFileSync(path + '.meta', 'utf8').match(/guid: (\w+)/);
  if (cls && meta && /Enemy|Attack|Projectile|Damage|Weapon|Combat/.test(cls[1])) classes[cls[1]] = {path, source, guid:meta[1]};
}
const binary = fs.openSync('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so', 'r');
let candidates = 0, referenced = 0;
for (const method of methods) {
  const key = method.Name.match(/(?:^|\.)(\w+)\$\$(\w+)$/);
  if (!key || !classes[key[1]] || /Gizmos|Validate|Test/.test(key[2])) continue;
  const cls = classes[key[1]];
  const body = cls.source.match(new RegExp('(bool|float|int) ' + key[2] + '\\([^)]*\\)\\s*\\{\\s*return\\s+(false|true|0f?|default\\([^)]*\\));\\s*\\}'));
  if (!body) continue;
  const bytes = Buffer.alloc(8); fs.readSync(binary, bytes, 0, 8, method.Address);
  // Decode only unambiguous constant-return forms. Do not discard an immediate
  // return merely because it is simple: its value must match the C# body.
  const expected = body[2] === 'true' ? 1 : 0;
  let original = null;
  if (body[1] === 'float' && bytes[0] === 0x0f && bytes[1] === 0x57 && bytes[2] === 0xc0 && bytes[3] === 0xc3) original = 0;
  if (body[1] !== 'float' && bytes[0] === 0x31 && bytes[1] === 0xc0 && bytes[2] === 0xc3) original = 0;
  if (body[1] === 'bool' && bytes[0] === 0xb0 && bytes[2] === 0xc3 && bytes[1] <= 1) original = bytes[1];
  if (body[1] === 'int' && bytes[0] === 0xb8 && bytes[5] === 0xc3) original = bytes.readInt32LE(1);
  if (original === expected) continue;
  candidates++;
  const query = cp.spawnSync('rg', ['-l', cls.guid, root, '-g', '*.prefab', '-g', '*.unity'], {encoding:'utf8'});
  if (query.status !== 0 && query.status !== 1) throw new Error(query.stderr);
  const refs = query.stdout.trim().split('\n').filter(Boolean);
  if (refs.length) referenced++;
  console.log(`${key[1]}.${key[2]} entry=0x${method.Address.toString(16)} csharp=${body[2]} native=${original === null ? 'nontrivial-or-unknown' : original} guid=${cls.guid} prefab_or_scene_refs=${refs.length}`);
}
console.log(`Heuristic candidates=${candidates}; entries with exported prefab/scene references=${referenced}. Inlined calls, dynamic loading, overloaded names, non-void or nonempty stubs, missing assets, and actual runtime reachability are not covered.`);
