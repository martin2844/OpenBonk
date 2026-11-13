// Heuristic only: literal-null methods of scene/prefab-referenced scripts whose
// original IL2CPP entry is not a recognizable immediate null return.
// Does not resolve overloads, nested classes, conditional compilation, or usage.
const fs = require('fs'), cp = require('child_process'), crypto = require('crypto');
const root = 'unity/OpenBonk/Assets';
const files = cp.execFileSync('rg', ['--files', `${root}/Scripts/Assembly-CSharp`, '-g', '*.cs'], {encoding:'utf8'}).trim().split('\n');
const refs = cp.execFileSync('rg', ['-o', '--no-filename', 'm_Script: \\{fileID: 11500000, guid: [a-f0-9]{32}', root, '-g', '*.prefab', '-g', '*.unity'], {encoding:'utf8', maxBuffer: 64 * 1024 * 1024});
const used = new Set([...refs.matchAll(/guid: ([a-f0-9]{32})/g)].map(x => x[1]));
const classes = new Map();
for (const file of files) {
  const source = fs.readFileSync(file, 'utf8');
  const name = source.match(/\bclass\s+(\w+)/)?.[1];
  const guid = fs.existsSync(file + '.meta') && fs.readFileSync(file + '.meta', 'utf8').match(/guid: (\w+)/)?.[1];
  if (name && guid && used.has(guid)) classes.set(name, {file, source, guid});
}
const methods = JSON.parse(fs.readFileSync('analysis/il2cpp/script.json')).ScriptMethod;
const binaryPath = '/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
if (crypto.createHash('sha256').update(fs.readFileSync(binaryPath)).digest('hex') !== 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1')
  throw new Error('Original binary differs from the metadata used for these addresses');
const binary = fs.openSync(binaryPath, 'r');
let candidates = 0;
for (const method of methods) {
  const key = method.Name.match(/(?:^|\.)(\w+)\$\$(\w+)$/);
  if (!key || !classes.has(key[1])) continue;
  const cls = classes.get(key[1]);
  if (!new RegExp('\\b' + key[2] + '\\s*\\([^)]*\\)\\s*\\{\\s*return\\s+null\\s*;\\s*\\}').test(cls.source)) continue;
  const bytes = Buffer.alloc(8); fs.readSync(binary, bytes, 0, bytes.length, method.Address);
  if ((bytes[0] === 0x31 && bytes[1] === 0xc0 && bytes[2] === 0xc3) ||
      (bytes[0] === 0x48 && bytes[1] === 0x31 && bytes[2] === 0xc0 && bytes[3] === 0xc3) ||
      (bytes[0] === 0xb8 && bytes.readUInt32LE(1) === 0 && bytes[5] === 0xc3)) continue;
  console.log(`${key[1]}.${key[2]} entry=0x${method.Address.toString(16)} guid=${cls.guid} file=${cls.file}`);
  candidates++;
}
console.log(`Candidates=${candidates}; regex and entry-byte screening do not establish runtime reachability or missing functionality.`);
