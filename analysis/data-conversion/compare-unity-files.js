#!/usr/bin/env node
// Read-only evidence for the Linux-to-Android data conversion problem.
// Compare original serialized files with their corresponding rebuilt Android
// APK/OBB entries, reassembling Unity's numbered .splitN chunks numerically.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { spawnSync, spawn } = require('child_process');
const base = process.argv[2] || '/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data';
const apk = process.argv[3] || 'build/android/Megabonk-arm64-split-test.apk';
const obb = process.argv[4] || 'build/android/Megabonk-arm64-split-test.main.obb';
const files = ['globalgamemanagers', 'globalgamemanagers.assets', 'level0', 'level1', 'level2', 'level3', 'level4', 'sharedassets0.assets', 'sharedassets1.assets', 'sharedassets2.assets', 'sharedassets3.assets', 'sharedassets4.assets', 'resources.assets'];
function entries(archive) {
  const result = spawnSync('unzip', ['-Z1', archive], {encoding: 'utf8', maxBuffer: 16 * 1024 * 1024});
  if (result.status !== 0) throw Error(`Cannot list ${archive}: ${result.stderr}`);
  return result.stdout.split('\n').filter(Boolean);
}
function hashFile(file) {
  return new Promise((resolve, reject) => {
    const hash = crypto.createHash('sha256'); let bytes = 0, header = Buffer.alloc(0);
    fs.createReadStream(file).on('data', chunk => { bytes += chunk.length; hash.update(chunk); if (header.length < 128) header = Buffer.concat([header, chunk.subarray(0, 128 - header.length)]); })
      .on('error', reject).on('end', () => resolve({bytes, sha256: hash.digest('hex'), serialized: serializedHeader(header)}));
  });
}
// Version 22 serialized-file headers have an extended 48-byte header. The
// metadata begins with a NUL-terminated Unity version and little-endian target.
function serializedHeader(header) {
  if (header.length < 64 || header.readUInt32BE(8) !== 22) return null;
  const end = header.indexOf(0, 48);
  if (end < 0 || end + 5 > header.length) return null;
  const unity = header.subarray(48, end).toString('ascii');
  if (!/^\d+\.\d+\./.test(unity)) return null;
  return {format: 22, unity, targetPlatform: header.readInt32LE(end + 1)};
}
async function hashZip(archive, names) {
  const hash = crypto.createHash('sha256'); let bytes = 0, header = Buffer.alloc(0);
  for (const name of names) await new Promise((resolve, reject) => {
    const child = spawn('unzip', ['-p', archive, name], {stdio: ['ignore', 'pipe', 'pipe']});
    let error = ''; child.stderr.on('data', x => error += x);
    child.stdout.on('data', x => { bytes += x.length; hash.update(x); if (header.length < 128) header = Buffer.concat([header, x.subarray(0, 128 - header.length)]); });
    child.on('error', reject); child.on('close', code => code === 0 ? resolve() : reject(Error(`unzip ${name}: ${error}`)));
  });
  return {bytes, sha256: hash.digest('hex'), serialized: serializedHeader(header)};
}
(async () => {
  const sources = [{archive: apk, entries: entries(apk)}, {archive: obb, entries: entries(obb)}];
  const report = [];
  for (const name of files) {
    const originalPath = path.join(base, name);
    if (!fs.existsSync(originalPath)) continue;
    const candidates = [];
    for (const source of sources) {
      const prefix = 'assets/bin/Data/' + name;
      const exact = source.entries.filter(x => x === prefix);
      const parts = source.entries.map(x => ({x, n: x.startsWith(prefix + '.split') && /^\d+$/.test(x.slice((prefix + '.split').length)) ? Number(x.slice((prefix + '.split').length)) : -1})).filter(x => x.n >= 0).sort((a,b) => a.n-b.n);
      if (exact.length && parts.length) throw Error(`Both split and unsplit: ${name}`);
      if (exact.length) candidates.push({archive: source.archive, names: exact});
      if (parts.length) {
        if (parts.some((p, i) => p.n !== i)) throw Error(`Missing or duplicate split for ${name}`);
        candidates.push({archive: source.archive, names: parts.map(p => p.x)});
      }
    }
    if (candidates.length !== 1) { report.push({name, candidates: candidates.length}); continue; }
    const original = await hashFile(originalPath);
    const android = await hashZip(candidates[0].archive, candidates[0].names);
    report.push({name, original, android: {...android, archive: candidates[0].archive, chunks: candidates[0].names.length}, identical: original.sha256 === android.sha256});
  }
  console.log(JSON.stringify({originalDirectory: base, comparison: report}, null, 2));
})().catch(e => { console.error(e.stack || e); process.exitCode = 1; });
