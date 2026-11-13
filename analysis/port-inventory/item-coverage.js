// Explicit selected-path evidence only. A missing mapping means not audited by
// this script, not that no test exists. Never infer full item correctness.
const fs=require('fs'),path=require('path'),items=require('./inventory.json').items;
const root='/home/your-user/megabonk';
const evidence={
 SlipperyRing:['SlipperyRingEvadePortCheck','two stacks, seeded damage gating, evade events, bypass and removal','slippery-ring-evade-20261107'],
 LeechingCrystal:['LeechingCrystalPortCheck','inventory stacks, maximum health, regeneration and removal','leeching-crystal-inventory-check'],
 TurboSocks:['BasicStatItemsPortCheck','two-stack movement stat, removal and maximum-speed calculation','turbo-socks-speed-retry'],
 Wrench:['ChargeShrinePlayCheck','inventory acquisition, shrine charge and scaled rewards','charge-shrine-inventory-reward-check'],
 Beacon:['ChargeShrinePlayCheck','inventory acquisition and shrine healing-zone activation','charge-shrine-inventory-reward-check'],
 Campfire:['CampfireHealthIntegrationCheck','inventory, regeneration stat and actual controlled health gain','campfire-health-check'],
 DemonBlade:['DemonBladePortCheck','critical gating, healing chance, stacks and cleanup','demon-blade-retry'],
 Rollerblades:['RollerbladesPortCheck','movement threshold, refresh timing, stack cap and removal','rollerblades-integrated'],
 ShatteredWisdom:['ShatteredWisdomPlayCheck','XP pickup damage, cooldown and value updates','shattered-wisdom-play-final'],
 SluttyCannon:['RocketItemIntegrationPlayCheck','item proc cap, rocket pool, targeting and damage','rocket-item-integration'],
 SuckyMagnet:['SuckyHoofTravelPlayCheck','inventory acquisition, pooled XP travel, collection and removal','sucky-hoof-travel-play'],
 Dragonfire:['FirefieldHitEffectPlayCheck','inventory acquisition, guaranteed field proc, damage, expiration, removal'],
 Battery:['BasicStatItemsPortCheck','stat propagation, stacks and removal'],
 Beer:['BasicStatItemsPortCheck','health and damage tradeoff, weapon damage, stacks and removal'],
 Borgar:['BorgarDropInventoryPortCheck','death event, distance boundary, stacked healing and removal'],
 Clover:['BasicStatItemsPortCheck','stat propagation, stacks and removal'],
 Backpack:['BasicStatItemsPortCheck','stat propagation, stacks and removal'],
 Oats:['BasicStatItemsPortCheck','stat propagation, stacks and removal'],
 ForbiddenJuice:['BasicStatItemsPortCheck','critical-chance calculation, stacks and removal'],
 GymSauce:['BasicStatItemsPortCheck','weapon damage calculation, stacks and removal'],
 Medkit:['MedkitXpIntegrationPortCheck','inventory stacks, timed health regeneration and removal'],
 TimeBracelet:['MedkitXpIntegrationPortCheck','inventory stacks, XP gain and removal'],
 SpeedBoi:['SpeedBoiPortCheck','health threshold, slowdown, cooldown, stacked duration, damage and removal'],
 Scarf:['ScarfItemPortCheck','grounded event, stacked airborne weapon damage and removal'],
 OverpoweredLamp:['OverpoweredLampPortCheck','on-hit repetition with Moldy Cheese, stacks, zero coefficient and removal'],
 MoldyCheese:['MoldyCheeseInventoryPortCheck','inventory dispatch, zero coefficient, five-item poison and removal'],
 TacticalGlasses:['ItemCombatPortCheck','stacked high-health pre-attack bonus and description'],
 BossBuster:['ItemCombatPortCheck','elite/boss pre-attack bonus and description'],
 GiantFork:['GiantForkPortCheck','crit gate, chance, stacks, removal'],
 GamerGoggles:['GogglesKnucklesPortCheck','low-health bonus and description'],
 BrassKnuckles:['GogglesKnucklesPortCheck','distance-gated damage and description'],
 UnstableTransfusion:['BloodmarkItemsPortCheck','hit proc, bloodmark and description'],
 BloodyCleaver:['BloodmarkItemsPortCheck','deferred lifesteal stacks and description'],
 Cactus:['CactusItemPortCheck','damage events, radial hits, stacks, effect, cleanup'],
 OldMask:['EnemyDifficultyItemsPortCheck','stacks, stat, elite weighting, removal'],
 Skuleg:['EnemyDifficultyItemsPortCheck','stacks, difficulty stat, removal'],
 PhantomShroud:['PhantomShroudPortCheck','evasion, proc, cap, timeout, cleanup'],
 Kevin:['ItemKevinPortCheck','self-damage, stacks, health cap, cleanup'],
 Snek:['SnekPortCheck','hit proc, poison, burst, effect, cleanup'],
 GlovePoison:['PoisonGlovePortCheck','area damage, poison, cooldown, effect'],
 GloveLightning:['LightningGlovePortCheck','area damage, stun, cooldown, effect'],
 GloveBlood:['BloodGlovePortCheck','healing, bloodmarks, threshold, cooldown'],
 GloveCurse:['CursedGlovePortCheck','stats, chance, area damage, tick cap'],
 GlovePower:['PowerGlovePortCheck','chance, area hit, knockback, cooldown, stacks'],
 CursedDoll:['CursedDollPortCheck','target limits, boss damage, cooldown, cleanup'],
 DemonicBlood:['DemonicBloodPortCheck','death scaling, timing, cap and cleanup'],
 DemonicSoul:['DemonicBloodPortCheck','death-driven stacks, damage refresh, cap, partial removal and event cleanup','demonic-soul-integrated'],
 Pot:['ExtraUpgradeLevelsPortCheck','weapon cap, diminishing stacks, partial/final removal and reacquisition','extra-upgrade-removal-native-20261107'],
 WizardsHat:['ExtraUpgradeLevelsPortCheck','tome cap, actual upgrades, partial/final removal and reacquisition','extra-upgrade-removal-native-20261107'],
 GoldenShield:['GoldenShieldPortCheck','damage event, gold drops, cooldown and cleanup'],
 GoldenGlove:['GoldenGlovePortCheck','seeded rolls, overload, removal and reacquisition'],
 EnergyCore:['EnergyCorePortCheck','burst timing, projectile impact and reuse'],
 CreditCardRed:['CreditCardsChestPortCheck','chest events, accumulation and removal'],
 CreditCardGreen:['CreditCardsChestPortCheck','chest events, accumulation and removal'],
 BeefyRing:['BeefyRingPortCheck','timed health and damage scaling, stacks, removal and reacquisition','beefy-removal-fixed-20261107'],
 GoldenSneakers:['GoldenSneakersPortCheck','movement sampling, gold, stacking and collection'],
 CowardsCloak:['CowardsCloakPortCheck','speed, damage, cap, expiry and cleanup'],
 HolyBook:['HolyBookPortCheck','inventory stats, healing damage, radius and cooldown'],
 Ghost:['GhostItemIntegrationCheck','interaction, stacks, projectile hit, saturation and cleanup'],
 IceCrystal:['IceCrystalPortCheck','proc chance, duration, stacking and cap'],
 BobDead:['BobDeadPortCheck','movement threshold, ghost sampling, damage, stacking and removal','bob-dead-check'],
 BobsLantern:['BobsLanternPortCheck','delay, radius, boss trigger, cooldown, reuse and caps','bobs-lantern-check-final2'],
 Bonker:['BonkerPlayCheck','chance, direct and area damage, effect pool, stacks and splash limit','bonker-check'],
 EagleClaw:['EagleClawPortCheck','stacked airborne damage and grounded knock-up through inventory','eagle-claw-integrated'],
 ElectricPlug:['ElectricPlugPortCheck','damage event, target count, scaling, lightning and effect reuse','electric-plug-retry'],
 Feathers:['FlappyFeathersPortCheck','jump event, normalized boost and inventory stats','flappy-feathers-retry'],
 Gasmask:['GasmaskPortCheck','poison lifecycle, update interval, caps, stacking and stage reset','gasmask-stack-removal-fixed'],
 GrandmasSecretTonic:['GrandmasTonicPlayCheck','critical-only proc, batch damage, radius, pool and cap','grandmas-tonic-play'],
 IdleJuice:['IdleJuicePortCheck','timing, stack cap, movement threshold and active cleanup','idle-juice-active-removal-fixed'],
 JoesDagger:['JoesDaggerPortCheck','eligibility, chance, cooldown, boss exclusion, stacking and removal','joes-dagger-check'],
 Mirror:['MirrorPortCheck','cooldown, cone, reflection, boss reduction, invulnerability and cleanup','mirror-health-check'],
 QuinsMask:['QuinsMaskPlayCheck','eligible sources, damage, effects, stacking and cap','quins-mask-play'],
 SoulHarvester:['SoulHarvesterPortCheck','release event, projectile spawn and damage, stacks and cleanup','soul-harvester-check'],
 SpicyMeatball:['SpicyMeatballPlayCheck','proc chance, area damage, effect pool, radius and per-tick limit','spicy-meatball-check'],
 ToxicBarrel:['ToxicBarrelPortCheck','damage event, poison, radius, cooldown, effect and removal','toxic-barrel-retry'],
 SpikyShield:['SpikyShieldPortCheck','timed armor and thorns, stacking, final removal and reacquisition','spiky-shield-inventory-reacquire-20261107'],
 ZaWarudo:['ZaWarudoPortCheck','shield, overheal, revival, freeze, stacking and final death','zawarudo-check'],
 CryptKey:['CryptKeyPickupPlayCheck','coffin pickup, inventory event, effect and duplicate interaction','crypt-key-play-check'],
 CageKey:['MonkeCageQuestPlayCheck','original cage key, inventory gate, unlock and duplicate-interaction guard','monke-cage-check'],
 IceCube:['IceCubeProbabilityPortCheck','stacked ice damage and controlled ice-hit freeze probability boundaries','icecube-probability-final-20261107'],
 Key:['KeyChestPlayCheck','one and two stack chance, removal, controlled free and paid chest outcomes','key-chest-play-retry-20261108'],
 GoldenRing:['GoldenRingSelectionCheck','legendary chest selection, six-run gate and seeded low/high roll','golden-ring-selection-20261108'],
 Chonkplate:['ChonkplatePortCheck','stacked stats, controlled damage-event healing, cap reduction and removal','chonkplate-overheal-removal-20261108']
};
// Android observers are recorded separately from editor fixtures: these runs
// force setup and do not establish physical-device or full-item coverage.
const androidEvidence={
 EchoShard:['echo-collection-retry-20261107','Echo collection probe PASS:','forced stacks and enemy death; XP color, collection and award'],
 LightningOrb:['lightning-orb-combat-20261107-android','Lightning orb probe PASS:','forced stacks; ordinary weapon hits, lightning damage and stun'],
 Pumpkin:['pumpkin-same-seed-android','Stage pumpkin comparison PASS:','two forced stacks; same-seed pot generation comparison'],
 Anvil:['anvil-offer-ui-android-20261107','Anvil offer UI probe PASS:','forced item and offer; virtual controller selects weapon upgrade']
};
const out=items.map(i=>{
 const t=evidence[i.code];
 if(!t){
  const a=androidEvidence[i.code];
  if(!a)return {item:i.name,code:i.code,status:'not mapped by this audit'};
  const log=`build/android/${a[0]}.log`,text=fs.readFileSync(path.join(root,log),'utf8');
  if(!text.includes(a[1]))throw Error('Missing Android pass marker '+log);
  return {item:i.name,code:i.code,status:'selected Android emulator paths passed',checked:a[2],log};
 }
 const log=`build/android/${t[2] || 'inventory-'+t[0]}.log`,text=fs.readFileSync(path.join(root,log),'utf8');
 if(!text.includes(t[0]+' passed:'))throw Error('Missing pass marker '+log);
 return {item:i.name,code:i.code,status:'selected editor paths passed',checked:t[1],log};
});
fs.writeFileSync(path.join(root,'analysis/port-inventory/item-coverage.json'),JSON.stringify(out,null,2)+'\n');
console.log(`Selected behavioral fixture mapped: ${out.filter(x=>x.log).length}/${out.length} items`);
console.log('LIMIT: mapped tests use controlled state and cover selected paths only; no full parity, physical Retroid or natural gameplay claim. Unmapped items may have other tests.');
