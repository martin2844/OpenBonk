// Heuristic for literal-null methods with same-named source calls. It does not
// establish receiver types, overloads, branch compilation or runtime reachability.
const fs = require('fs'), cp = require('child_process'), crypto = require('crypto');
const root = 'unity/OpenBonk/Assets/Scripts/Assembly-CSharp';
const paths = cp.execFileSync('rg', ['--files', root, '-g', '*.cs'], {encoding:'utf8'}).trim().split('\n');
const files = paths.map(path => ({path, source:fs.readFileSync(path, 'utf8')}));
const classes = new Map();
for (const f of files) { const n = f.source.match(/\bclass\s+(\w+)/)?.[1]; if (n) { if (!classes.has(n)) classes.set(n, []); classes.get(n).push(f); } }
const methods = JSON.parse(fs.readFileSync('analysis/il2cpp/script.json')).ScriptMethod;
const binary = '/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
if (crypto.createHash('sha256').update(fs.readFileSync(binary)).digest('hex') !== 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw Error('Original binary differs from metadata');
const fd = fs.openSync(binary, 'r');
for (const method of methods) {
  const k = method.Name.match(/(?:^|\.)(\w+)\$\$(\w+)$/);
  if (!k || !classes.has(k[1])) continue;
  for (const f of classes.get(k[1])) {
    if (!new RegExp('\\b' + k[2] + '\\s*\\([^)]*\\)\\s*\\{\\s*return\\s+null\\s*;\\s*\\}').test(f.source)) continue;
    const b = Buffer.alloc(8); fs.readSync(fd, b, 0, 8, method.Address);
    if ((b[0] === 0x31 && b[1] === 0xc0 && b[2] === 0xc3) || (b[0] === 0x48 && b[1] === 0x31 && b[2] === 0xc0 && b[3] === 0xc3) || (b[0] === 0xb8 && b.readUInt32LE(1) === 0 && b[5] === 0xc3)) continue;
    const re = new RegExp('\\.\\s*' + k[2] + '\\s*\\(');
    const callers = files.filter(x => re.test(x.source));
    if (callers.length) console.log(`${k[1]}.${k[2]} entry=0x${method.Address.toString(16)} file=${f.path} same_name_call_files=${callers.length} sample=${callers.slice(0,5).map(x=>x.path.split('/').pop()).join(',')}`);
  }
}
