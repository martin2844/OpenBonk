// Map explicit editor test evidence to current serialized character/passive wiring.
// Coverage is a test pointer, not an assertion of full passive or game parity.
const fs=require('fs'),path=require('path'),inv=require('./inventory.json');
const root='/home/your-user/megabonk';
const cases={
 PassiveAbilityRngBlessing:['FoxPassivePortCheck','XP, luck and localized description'],
 PassiveAbilityCritHappens:['LevelPassivePortCheck','level stat, description, cleanup'],
 PassiveAbilityReinforced:['LevelPassivePortCheck','level stat, description, cleanup'],
 PassiveAbilityWarrior:['LevelPassivePortCheck','level stat, description, cleanup'],
 PassiveAbilityCurse:['LevelPassivePortCheck','level stat, description, cleanup'],
 PassiveAbilityVampire:['LevelPassivePortCheck','level stat, description, cleanup'],
 PassiveAbilityQuantumXp:['LevelPassivePortCheck','level stat, description, cleanup'],
 PassiveAbilityFlowstate:['LevelPassivePortCheck','level stat, description, cleanup'],
 PassiveAbilityWallClimb:['WallClimbPassivePortCheck','XP, max health stat, description'],
 PassiveAbilityEnduring:['EnduringPassivePortCheck','size, frozen damage, cap, reset'],
 PassiveAbilityPlague:['PlaguePassivePortCheck','enemy death, poison, debuff reuse, reset'],
 PassiveAbilityShadowstep:['ShadowstepPassivePortCheck','evasion, damage gates, cleanup'],
 PassiveAbilityStonks:['StonksPassivePortCheck','gold tiers, stat update, XP, description'],
 PassiveAbilityLockIn:['LockInPortCheck','health scaling, cooldown, thorns, cleanup'],
 PassiveAbilityFloating:['FloatingPortCheck','grounded and airborne scaling, landing'],
 PassiveAbilityBullseye:['BullseyePortCheck','marker, hit explosion, expiry and reset'],
 PassiveAbilitySpeedDemon:['SpeedDemonPortCheck','timed speed, movement damage, damage exclusions, cleanup'],
 PassiveAbilityGamba:['GambaPortCheck','seeded upgrade, XP catch-up, popup cap, cleanup'],
 PassiveAbilityHoarder:['HoarderPortCheck','gates, chest timer, placement and facing'],
 PassiveAbilityZooma:['ZoomaPortCheck','movement charge, hit, lightning, effect, cleanup','-retry'],
 PassiveAbilityFlex:['FlexPassiveInventoryCheck','actual asset description, readiness, cooldown, cleanup']
};
const out=inv.characters.map(c=>{
 const t=cases[c.passiveFactory];
 if(!t) return {character:c.name,passive:c.passiveFactory,status:'no fixture mapped by this script'};
 const log=`build/android/inventory-${t[0]}${t[2]??''}.log`, text=fs.readFileSync(path.join(root,log),'utf8');
 if(!text.includes(t[0]+' passed:'))throw Error('Missing pass marker in '+log);
 return {character:c.name,passive:c.passiveFactory,status:'selected editor checks passed',checked:t[1],log};
});
fs.writeFileSync(path.join(root,'analysis/port-inventory/character-coverage.json'),JSON.stringify(out,null,2)+'\n');
console.log(`Characters with a selected passing editor fixture: ${out.filter(c=>c.log).length}/${out.length}`);
console.log('Without a mapped fixture: '+out.filter(c=>!c.log).map(c=>c.character).join(', '));
console.log('LIMIT: these tests use controlled state and cover selected paths only. No claim of complete parity, real gameplay, Android or device verification. Other tests may exist but were not mapped.');
