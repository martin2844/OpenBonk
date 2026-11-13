#!/usr/bin/env node
// Verify the three known shared item factory cases against the shipped x86-64 binary.
const fs = require('fs');
const crypto = require('crypto');
const path = '/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so';
const bytes = fs.readFileSync(path);
const hash = crypto.createHash('sha256').update(bytes).digest('hex');
const expectedHash = 'a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1';
if (hash !== expectedHash) throw new Error(`Unexpected GameAssembly.so SHA-256 ${hash}`);
// Factory disassembly shows an 88-entry signed-relative jump table at this address;
// case 0x227e465 constructs ItemNoImplementation (see ItemFactory-CreateItem.asm).
const base = 0xb6f7c8, target = 0x227e465;
const actual = [];
for (let item = 0; item <= 0x57; item++) {
  if (base + bytes.readInt32LE(base + 4 * item) === target) actual.push(item);
}
const expected = [69, 79, 81]; // CageKey, GoldenRing, CryptKey in EItem.cs
if (actual.join(',') !== expected.join(',')) throw new Error(`Unexpected cases: ${actual}`);
console.log(`PASS: SHA-256 ${hash}; original ItemNoImplementation factory cases: CageKey=69, GoldenRing=79, CryptKey=81; target=0x${target.toString(16)}`);
console.log('Factory dispatch only; does not rule out behavior attached to these items elsewhere.');
