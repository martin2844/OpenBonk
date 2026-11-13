// Bounded serialized-data audit: enabled hat orientation references to enabled
// characters must have the requested Low/Lowest mesh reference. It does not
// validate mesh contents, bone order, hat placement or runtime availability.
const fs = require('fs');
const root = 'unity/OpenBonk/Assets/MonoBehaviour/';
const files = fs.readdirSync(root).filter(name => name.endsWith('.asset'));
const characters = new Map();
for (const name of files) {
  const text = fs.readFileSync(root + name, 'utf8');
  const character = text.match(/^  eCharacter: (\d+)/m);
  if (!character || !/^  StatCategoryRatios:/m.test(text) || !/^  isEnabled: 1/m.test(text)) continue;
  characters.set(+character[1], { name,
    low: !/^  meshLow: \{fileID: 0\}/m.test(text),
    lowest: !/^  meshLowest: \{fileID: 0\}/m.test(text) });
}
let references = 0, missing = 0, hats = 0;
for (const name of files) {
  const text = fs.readFileSync(root + name, 'utf8');
  if (!/^  orientations:/m.test(text) || !/^  isEnabled: 1/m.test(text)) continue;
  hats++;
  const orientations = /  - character: (\d+)\n    meshForHat: ([12])/g;
  for (const match of text.matchAll(orientations)) {
    const character = characters.get(+match[1]);
    if (!character) continue;
    references++;
    const field = match[2] === '1' ? 'low' : 'lowest';
    if (!character[field]) { missing++; console.log(`${name}: ${character.name} missing ${field}`); }
  }
}
console.log(`enabledCharacters=${characters.size} enabledHats=${hats} alternateMeshReferences=${references} missing=${missing}`);
if (missing) process.exitCode = 1;
