#!/usr/bin/env node
const fs = require('fs'), crypto = require('crypto');
const binary=fs.readFileSync('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/GameAssembly.so');
if(crypto.createHash('sha256').update(binary).digest('hex')!=='a86fd434619dafb328ec741b6bc285992ee1f52e69533b2db8f35185c2ed5ba1') throw new Error('unexpected game binary');
const dir='unity/OpenBonk/Assets/Scripts/Assembly-CSharp/Assets/Scripts/Inventory__Items__Pickups/Items/ItemImplementations/';
// Values come from the recovered factory instruction blocks: Beer 0x227ea77,
// DemonBlade 0x227eb05, CowardsCloak 0x227ea98. Integers use IL2CPP field offsets.
const beer=Buffer.alloc(8); beer.writeBigUInt64LE(0xbd4ccccd3e4ccccdn);
const blade=Buffer.alloc(8); blade.writeBigUInt64LE(0x3e8000003c23d70an);
const cloak=binary.subarray(0xb329d0,0xb329e0);
const checks=[['ItemBeer','damagePerStack',beer.readFloatLE(0)],['ItemBeer','maxHealthPerStack',beer.readFloatLE(4)],['ItemDemonBlade','critChance',blade.readFloatLE(0)],['ItemDemonBlade','healChancePerStack',blade.readFloatLE(4)],['ItemCowardsCloak','speedPerAmount',cloak.readFloatLE(0)],['ItemCowardsCloak','speedPerStack',cloak.readFloatLE(4)],['ItemCowardsCloak','maxStacks',cloak.readInt32LE(8)],['ItemCowardsCloak','stacksPerAmount',cloak.readInt32LE(12)]];
// Further immediate writes in the recovered factory; decimal values below are
// decoded float32 constants, with field names matched using the IL2CPP dump.
checks.push(
 ['ItemBattery','attackSpeedPerStack',.08], ['ItemChonkplate','overhealPerAmount',.75], ['ItemChonkplate','lifestealPerAmount',.2],
 ['ItemBackpack','projectilesPerAmount',1], ['ItemClover','luckPerAmount',.1],
 ['ItemBeefyRing','maxHpPerStack',10], ['ItemBeefyRing','powerPerHpPerAmount',.002],
 ['ItemCampfire','healthRegenPerMinutePerAmount',1100], ['ItemCampfire','setupTime',.6], ['ItemCampfire','distThreshold',1.75],
 ['ItemBrassKnuckles','damagePerAmount',.25], ['ItemBrassKnuckles','baseRadius',8], ['ItemBrassKnuckles','radiusAddPerAmount',2],
 ['ItemCreditCardRed','damagePerChestAmount',.025], ['ItemCreditCardGreen','luckPerChestPerAmount',.02], ['ItemCreditCardGreen','chestPriceIncreasePerAmount',.1],
 ['ItemBossBuster','damagePerAmount',.15], ['ItemBeacon','extraShrinesPerAmount',2], ['ItemBeacon','healingRadiusPerAmount',2], ['ItemBeacon','healingFractionPerInterval',.025],
 ['ItemBobLantern','cooldownMin',5], ['ItemBobLantern','cooldownMax',45], ['ItemBobLantern','cooldownReductionPerAmount',3], ['ItemBobLantern','radiusMin',50], ['ItemBobLantern','radiusMax',250], ['ItemBobLantern','radiusPerAmount',10]
);
checks.push(['ItemBobDead','unitsPerProjectile',14], ['ItemBobDead','minSpawnTime',.05], ['ItemCactus','damagePerAmount',5], ['ItemCactus','numProjectilesPerAmount',2]);
for(const [type,field,expected] of checks){
 const src=fs.readFileSync(dir+type+'.cs','utf8'); const re=new RegExp('private\\s+(?:float|int)\\s+'+field+'\\s*=\\s*(-?[\\d.]+)f?\\s*;'); const match=src.match(re);
 if(!match || Math.fround(Number(match[1]))!==Math.fround(expected)) throw new Error(`${type}.${field}: port=${match?.[1]} original=${expected}`);
 console.log(`PASS ${type}.${field}: ${match[1]}`);
}
const borgor = fs.readFileSync(dir+'ItemBorgor.cs','utf8');
const originalBorgor = binary.subarray(0xb32060,0xb32070);
for(const [field, expected] of [['baseChance',originalBorgor.readFloatLE(0)], ['chancePerAmount',originalBorgor.readFloatLE(4)], ['ratioHeal',originalBorgor.readFloatLE(8)], ['flatHealPerAmount',originalBorgor.readInt32LE(12)], ['flatHeal',10]]) {
 const match=borgor.match(new RegExp('\\b'+field+'\\s*=\\s*(-?[\\d.]+)f?\\s*;'));
 if(!match || Math.fround(Number(match[1]))!==Math.fround(expected)) throw new Error(`ItemBorgor.${field}: port=${match?.[1]} original=${expected}`);
 console.log(`PASS ItemBorgor.${field}: ${match[1]}`);
}
const bob=fs.readFileSync(dir+'ItemBobDead.cs','utf8'), cactus=fs.readFileSync(dir+'ItemCactus.cs','utf8');
if (!/damageSource\s*=\s*EItem\.BobDead\.ToString\(\)/.test(bob)) throw new Error('BobDead damage source mismatch');
if (!/projectileDirections\s*=\s*new List<Vector3>\(\)/.test(cactus)) throw new Error('Cactus directions list initialization mismatch');
console.log('PASS BobDead enum damage source and Cactus directions-list initialization');
console.log(`${checks.length} explicit initializers and 5 Borgor constructor assignments match selected original factory data; behavior and remaining defaults are outside this check.`);
