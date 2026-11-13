// Inventory the references actually serialized in Android's MainMenu.
// A factory mapping is evidence of wiring only, never proof of functional parity.
const fs = require('fs'), path = require('path');
const root = '/home/your-user/megabonk';
const assets = path.join(root, 'unity/OpenBonk/Assets');
const dir = path.join(assets, 'MonoBehaviour');
const scene = fs.readFileSync(path.join(assets, 'ScenesAndroid/MainMenu.unity'), 'utf8');
const byGuid = new Map();
for (const file of fs.readdirSync(dir).filter(f => f.endsWith('.asset.meta'))) {
  const meta = fs.readFileSync(path.join(dir, file), 'utf8');
  const id = meta.match(/^guid: ([a-f0-9]+)$/m);
  if (id) byGuid.set(id[1], path.join(dir, file.slice(0, -5)));
}
const prefabs = new Map();
for (const file of fs.readdirSync(path.join(assets,'GameObject')).filter(f=>f.endsWith('.prefab.meta'))) {
  const meta=fs.readFileSync(path.join(assets,'GameObject',file),'utf8');
  const id=meta.match(/^guid: ([a-f0-9]+)$/m); if(id) prefabs.set(id[1],file.slice(0,-5));
}
function idFromRef(s) { return s?.match(/guid: ([a-f0-9]+)/)?.[1] ?? null; }
function assetName(ref) { const f=byGuid.get(idFromRef(ref)); return f ? path.basename(f,'.asset') : null; }
function prefabName(ref) { return prefabs.get(idFromRef(ref)) ?? null; }
function field(s, name) { return s.match(new RegExp('^  '+name+': (.*)$','m'))?.[1] ?? null; }
function list(name) {
  const m = scene.match(new RegExp('^  '+name+':\\n((?:  - .*\\n)+)', 'm'));
  if (!m) throw Error('Missing scene list '+name);
  return [...m[1].matchAll(/guid: ([a-f0-9]+)/g)].map(x => {
    const file = byGuid.get(x[1]); if (!file) throw Error('Unresolved '+x[1]);
    const text = fs.readFileSync(file, 'utf8');
    return {name:field(text,'m_Name'), enabled:field(text,'isEnabled') === '1', guid:x[1], asset:path.relative(root,file), text};
  });
}
const scripts = path.join(assets, 'Scripts/Assembly-CSharp/Assets/Scripts/Inventory__Items__Pickups/Items');
const enumText = fs.readFileSync(path.join(scripts,'EItem.cs'),'utf8');
const itemEnum = new Map([...enumText.matchAll(/\b(\w+)\s*=\s*(\d+)\s*,?/g)].map(m=>[m[2],m[1]]));
const factoryText = fs.readFileSync(path.join(scripts,'ItemFactory.cs'),'utf8');
const factory = new Map([...factoryText.matchAll(/case EItem\.(\w+): return new (\w+)\(/g)].map(m=>[m[1],m[2]]));
const items = list('unsortedItems').map(({text,...x})=>{
  const id=field(text,'eItem'), code=itemEnum.get(id), impl=factory.get(code) ?? null;
  return {...x, id, code:code ?? null, factory:impl, inPool:field(text,'inItemPool') === '1'};
});
const passiveRoot=path.join(assets,'Scripts/Assembly-CSharp/Assets/Scripts/Inventory__Items__Pickups/AbilitiesPassive');
const passiveEnum = new Map([...fs.readFileSync(path.join(passiveRoot,'EPassive.cs'),'utf8').matchAll(/\b(\w+)\s*=\s*(\d+)/g)].map(m=>[m[2],m[1]]));
const passiveFactory = new Map([...fs.readFileSync(path.join(passiveRoot,'PassiveAbilityFactory.cs'),'utf8').matchAll(/case EPassive\.(\w+): return new (\w+)\(/g)].map(m=>[m[1],m[2]]));
const characters=list('unsortedCharacterData').map(({text,...x})=>{
 const passive=field(text,'passive'), passivePath=byGuid.get(idFromRef(passive));
 const passiveId=passivePath ? field(fs.readFileSync(passivePath,'utf8'),'ePassive') : null;
 const passiveCode=passiveEnum.get(passiveId) ?? null;
 return {...x, id:field(text,'eCharacter'), weaponAsset:assetName(field(text,'weapon')), passiveAsset:assetName(passive), passiveCode, passiveFactory:passiveFactory.get(passiveCode) ?? null, prefab:prefabName(field(text,'prefab'))};
});
const weapons=list('unsortedWeapons').map(({text,...x})=>({...x,id:field(text,'eWeapon'), attack:prefabName(field(text,'attack'))}));
const tomes=list('unsortedTomes').map(({text,...x})=>({...x,id:field(text,'eTome')}));
const output={scope:'Android MainMenu serialized DataManager; static wiring only',items,characters,weapons,tomes};
fs.writeFileSync(path.join(root,'analysis/port-inventory/inventory.json'), JSON.stringify(output,null,2)+'\n');
for(const [name,arr] of Object.entries({items,characters,weapons,tomes})) console.log(`${name}: ${arr.length} references, ${arr.filter(x=>x.enabled).length} enabled`);
console.log('Enabled items with absent mapping:',items.filter(x=>x.enabled&&!x.factory).map(x=>x.name).join(', ')||'none');
console.log('Enabled items mapped to ItemNoImplementation:',items.filter(x=>x.enabled&&x.factory==='ItemNoImplementation').map(x=>x.name).join(', ')||'none');
console.log('Characters without resolved weapon, passive or prefab:',characters.filter(x=>x.enabled&&(!x.weaponAsset||!x.passiveAsset||!x.prefab)).map(x=>x.name).join(', ')||'none');
console.log('Characters without mapped passive factory:',characters.filter(x=>x.enabled&&!x.passiveFactory).map(x=>x.name+':'+x.passiveCode).join(', ')||'none');
console.log('Weapons without resolved attack prefab:',weapons.filter(x=>x.enabled&&!x.attack).map(x=>x.name).join(', ')||'none');
console.log('LIMIT: this is serialized reference/factory inventory, not behavior, runtime availability, completeness, original comparison, or test coverage. NoImplementation can be intentional.');
