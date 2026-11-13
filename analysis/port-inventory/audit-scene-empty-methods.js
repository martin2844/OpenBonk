// Bounded heuristic: exact empty void methods across exported C# classes with
// original IL2CPP entry bytes and references in exported prefabs/Unity scenes.
// This does not evaluate preprocessor branches; an Android implementation may
// coexist with an empty desktop branch. Class names can collide across files,
// and the first class declaration in a file need not own the matched method.
const fs = require('fs'), cp = require('child_process');
const root = 'unity/OpenBonk/Assets';
const methods = JSON.parse(fs.readFileSync('analysis/il2cpp/script.json')).ScriptMethod;
const files = cp.execSync(`rg --files ${root}/Scripts/Assembly-CSharp | rg '\\.cs$'`, {encoding:'utf8'}).trim().split('\n');
const classes = {};
for (const path of files) {
  const source = fs.readFileSync(path, 'utf8');
  const cls = source.match(/\bclass (\w+)/);
  const meta = fs.existsSync(path + '.meta') && fs.readFileSync(path + '.meta', 'utf8').match(/guid: (\w+)/);
  if (cls && meta) classes[cls[1]] = {path, source, guid:meta[1]};
}
const binary = fs.openSync('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so', 'r');
let candidates = 0, referenced = 0;
for (const method of methods) {
  const key = method.Name.match(/(?:^|\.)(\w+)\$\$(\w+)$/);
  if (!key || !classes[key[1]] || /Gizmos|Validate|Test/.test(key[2])) continue;
  const cls = classes[key[1]];
  if (!new RegExp('(?:void) ' + key[2] + '\\([^)]*\\)\\s*\\{\\s*\\}').test(cls.source)) continue;
  const bytes = Buffer.alloc(8); fs.readSync(binary, bytes, 0, 8, method.Address);
  if (bytes[0] === 0xc3) continue;
  candidates++;
  const query = cp.spawnSync('rg', ['-l', cls.guid, root, '-g', '*.prefab', '-g', '*.unity'], {encoding:'utf8'});
  if (query.status !== 0 && query.status !== 1) throw new Error(query.stderr);
  const refs = query.stdout.trim().split('\n').filter(Boolean);
  if (refs.length) referenced++;
  if (refs.length) console.log(`${key[1]}.${key[2]} entry=0x${method.Address.toString(16)} guid=${cls.guid} prefab_or_scene_refs=${refs.length}`);
}
console.log(`Heuristic candidates=${candidates}; entries with exported prefab/scene references=${referenced}. Preprocessor branches, duplicate class names, method ownership, inlined calls, dynamic loading, overloaded names, non-void or nonempty stubs, missing assets, and actual runtime reachability are not resolved.`);
