using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts._Data;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts._Data.Hats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Tools;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Saves___Serialization.Progression.Achievements
{
	public static class AchievementTracker
	{
		private static float baseMovementSpeed;

		private static float noDamageTimer;

		private static bool hasTakenDamageThisRun;

		private static bool hasDealtDamageThisRun;

		private static int consecutiveIceCrystalCooks;

		private static int consecutiveMoldyCheeseCooks;

		private static int runChestsBought;

		private static bool hasSpawnedLuckTomeQuest;

		private static string a_tacticalGlasses;

		private static string a_bossBuster;

		private static string a_luckTome;

		private static string a_quinsMask;

		private static string a_roberto;

		private static string a_hatSheriff;

		private static string aegisDamageSource;

		private static int chargedShrines;

		private static int chargedShrinesNoInterruptions;

		private static int totalChargeShrines;

		private static string a_hatPot;

		private static string a_kevin;

		private static int numBoomboxes;

		private static string a_hatTophat;

		private static string a_hatTophatLong;

		public static void Init()
		{
			InteractablesStatus.A_InteractableUsed -= OnInteractableUsedDebug;
			InteractablesStatus.A_InteractableUsed += OnInteractableUsedDebug;
			InteractableBossSpawner.A_BossDefeated -= OnStageBossDefeated;
			InteractableBossSpawner.A_BossDefeated += OnStageBossDefeated;
			FinalFightController.A_BossDefeated -= OnStageBossDefeated;
			FinalFightController.A_BossDefeated += OnStageBossDefeated;
			MainMenu.A_MenuOpened -= OnMainMenuOpened;
			MainMenu.A_MenuOpened += OnMainMenuOpened;
			InteractableBossSpawner.A_NumBossesDefeated -= OnStageBossDefeatedNum;
			InteractableBossSpawner.A_NumBossesDefeated += OnStageBossDefeatedNum;
			FinalFightController.A_BossDefeatedTime -= OnStageBossDefeatedInTime;
			FinalFightController.A_BossDefeatedTime += OnStageBossDefeatedInTime;
			MyTime.A_Tick -= OnTick;
			MyTime.A_Tick += OnTick;
			PlayerHealth.A_TakeDamage -= OnDamageTaken;
			PlayerHealth.A_TakeDamage += OnDamageTaken;
			Enemy.A_Damage -= OnEnemyDamaged;
			Enemy.A_Damage += OnEnemyDamaged;
			Enemy.A_EnemyDied -= OnEnemyDied;
			Enemy.A_EnemyDied += OnEnemyDied;
			GameManager.A_RunStarted -= OnRunStarted;
			GameManager.A_RunStarted += OnRunStarted;
			DetectInteractables.A_Interacted -= OnInteracted;
			DetectInteractables.A_Interacted += OnInteracted;
			GameManager.A_StageStarted -= OnStageStarted;
			GameManager.A_StageStarted += OnStageStarted;
			InteractableCryptLeave.A_FirstDungeonCompleted -= OnCryptSpeedrun;
			InteractableCryptLeave.A_FirstDungeonCompleted += OnCryptSpeedrun;
			PlayerXp.A_LevelUp -= OnLevelUp;
			PlayerXp.A_LevelUp += OnLevelUp;
			GraveyardBossRoom.A_BossDied -= OnGhostBossDied;
			GraveyardBossRoom.A_BossDied += OnGhostBossDied;
			TrackStats.A_PotBroken -= OnPotBroken;
			TrackStats.A_PotBroken += OnPotBroken;
			WeaponInventory.A_WeaponAdded -= OnWeaponAddedOrUpgraded;
			WeaponInventory.A_WeaponAdded += OnWeaponAddedOrUpgraded;
			ItemInventory.A_ItemAdded -= OnItemAdded;
			ItemInventory.A_ItemAdded += OnItemAdded;
			TomeInventory.A_TomeUpgrade -= OnTomeAddedOrUpgraded;
			TomeInventory.A_TomeUpgrade += OnTomeAddedOrUpgraded;
			ChargeShrine.A_ChargeShrineSpawned -= OnChargeShrineSpawned;
			ChargeShrine.A_ChargeShrineSpawned += OnChargeShrineSpawned;
			ChargeShrine.A_Charged -= OnShrineCharged;
			ChargeShrine.A_Charged += OnShrineCharged;
			InteractableMicrowave.A_Used -= OnMicrowaveUsed;
			InteractableMicrowave.A_Used += OnMicrowaveUsed;
			MyPlayer.A_PlayerInventoryInitialized -= OnPLayerInventoryInited;
			MyPlayer.A_PlayerInventoryInitialized += OnPLayerInventoryInited;
			PlayerStatsNew.A_StatUpdate -= OnStatUpdate;
			PlayerStatsNew.A_StatUpdate += OnStatUpdate;
			LateFixedUpdate.A_LateUpdate -= OnLateFixedUpdate;
			LateFixedUpdate.A_LateUpdate += OnLateFixedUpdate;
			MyAchievements.A_Unlocked -= OnUnlock;
			MyAchievements.A_Unlocked += OnUnlock;
			UnlocksFooter.A_Purchased -= OnPurchased;
			UnlocksFooter.A_Purchased += OnPurchased;
		}

		public static void Cleanup()
		{
			InteractablesStatus.A_InteractableUsed -= OnInteractableUsedDebug;
			InteractableBossSpawner.A_BossDefeated -= OnStageBossDefeated;
			FinalFightController.A_BossDefeated -= OnStageBossDefeated;
			MainMenu.A_MenuOpened -= OnMainMenuOpened;
			InteractableBossSpawner.A_NumBossesDefeated -= OnStageBossDefeatedNum;
			FinalFightController.A_BossDefeatedTime -= OnStageBossDefeatedInTime;
			MyTime.A_Tick -= OnTick;
			PlayerHealth.A_TakeDamage -= OnDamageTaken;
			Enemy.A_Damage -= OnEnemyDamaged;
			Enemy.A_EnemyDied -= OnEnemyDied;
			GameManager.A_RunStarted -= OnRunStarted;
			DetectInteractables.A_Interacted -= OnInteracted;
			GameManager.A_StageStarted -= OnStageStarted;
			InteractableCryptLeave.A_FirstDungeonCompleted -= OnCryptSpeedrun;
			PlayerXp.A_LevelUp -= OnLevelUp;
			GraveyardBossRoom.A_BossDied -= OnGhostBossDied;
			TrackStats.A_PotBroken -= OnPotBroken;
			WeaponInventory.A_WeaponAdded -= OnWeaponAddedOrUpgraded;
			ItemInventory.A_ItemAdded -= OnItemAdded;
			TomeInventory.A_TomeUpgrade -= OnTomeAddedOrUpgraded;
			ChargeShrine.A_ChargeShrineSpawned -= OnChargeShrineSpawned;
			ChargeShrine.A_Charged -= OnShrineCharged;
			InteractableMicrowave.A_Used -= OnMicrowaveUsed;
			MyPlayer.A_PlayerInventoryInitialized -= OnPLayerInventoryInited;
			PlayerStatsNew.A_StatUpdate -= OnStatUpdate;
			LateFixedUpdate.A_LateUpdate -= OnLateFixedUpdate;
			MyAchievements.A_Unlocked -= OnUnlock;
			UnlocksFooter.A_Purchased -= OnPurchased;
		}

		private static void OnFinalSwarmStart()
		{
		}

		private static void OnRunStarted()
		{
			runChestsBought = 0;
			hasTakenDamageThisRun = false;
			hasDealtDamageThisRun = false;
			hasSpawnedLuckTomeQuest = false;
			chargedShrines = 0;
			chargedShrinesNoInterruptions = 0;
			totalChargeShrines = 0;
		}

		private static void OnStageStarted()
		{
			numBoomboxes = 0;
			consecutiveIceCrystalCooks = 0;
			consecutiveMoldyCheeseCooks = 0;
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyPlayer.Instance == null || MapController.currentMap == null) return;
			if (!MyAchievements.IsAchievementDone("a_desert") && MapController.currentMap.eMap == EMap.Forest &&
				MapController.index == 1 && MyPlayer.Instance.character == ECharacter.Cl4nk)
				MyAchievements.TryUnlock("a_desert");
		}

		private static void OnPLayerInventoryInited(PlayerInventory inv)
		{
			if (inv?.playerStats == null) return;
			baseMovementSpeed = inv.playerStats.GetStat(EStat.MoveSpeedMultiplier);
			noDamageTimer = 0f;
		}

		private static void OnStatUpdate(EStat stat)
		{
			if (GameManager.Instance == null || MyPlayer.Instance?.inventory?.playerStats == null || SaveManager.Instance?.progression == null || DataManager.Instance == null) return;
			if (stat == EStat.MaxHealth && !MyAchievements.IsAchievementDone("a_chonkplate"))
			{
				var health = MyPlayer.Instance.inventory.playerHealth;
				var achievement = DataManager.Instance.GetAchievement("a_chonkplate");
				if (health != null && achievement != null && health.maxHp >= achievement.targetValue) MyAchievements.TryUnlock("a_chonkplate");
			}
			if (stat == EStat.Difficulty && !MyAchievements.IsAchievementDone("a_skuleg"))
			{
				var achievement = DataManager.Instance.GetAchievement("a_skuleg");
				if (achievement != null && MyPlayer.Instance.inventory.playerStats.GetStat(EStat.Difficulty) * 100f >= achievement.targetValue) MyAchievements.TryUnlock("a_skuleg");
			}
		}

		private static void OnTick()
		{
			if (GameManager.Instance == null || SaveManager.Instance?.progression == null || DataManager.Instance == null) return;
			if (!MyAchievements.IsAchievementDone("a_aura") && GameManager.Instance.IsPlaying())
			{
				noDamageTimer += MyTime.fixedDeltaTime;
				if (noDamageTimer > 120f) MyAchievements.TryUnlock("a_aura");
			}
			CheckSwarmTime("a_ghost", 1);
			CheckSwarmTime("a_bob", 60);
			CheckSwarmTime("a_soulHarvester", 60);
			if (!MyAchievements.IsAchievementDone("a_kills5000") && RunStats.GetStat(EMyStat.kills) >= 5001)
				MyAchievements.TryUnlock("a_kills5000");
		}

		private static void CheckSwarmTime(string name, int multiplier)
		{
			if (MyAchievements.IsAchievementDone(name)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && MyTime.finalSwarmTimer >= achievement.targetValue * multiplier)
				MyAchievements.TryUnlock(name);
		}

		private static void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
		{
			if (enemy == null || SaveManager.Instance == null || SaveManager.Instance.progression == null || DataManager.Instance == null) return;
			float elapsed = MyTime.time - enemy.spawnedAtTime;
			if (enemy.IsBoss() && !enemy.IsStageBoss()) TryUnlockTimed("a_tacticalGlasses", elapsed);
			if (enemy.IsStageBoss()) TryUnlockTimed("a_bossBuster", elapsed);
			if (!hasSpawnedLuckTomeQuest && EffectManager.Instance != null && GameManager.Instance != null &&
				!MyAchievements.IsAchievementDone("a_luckTome") &&
				global::Utility.MyRandom.random.NextDouble() <= MyAchievements.GetAchievementTargetValueFloat("a_luckTome") * .01f)
			{
				hasSpawnedLuckTomeQuest = true;
				EffectManager.Instance.TrySpawnLuckQuest(global::Utility.RaycastUtility.RayToGround(enemy.GetCenterPosition()) + UnityEngine.Vector3.up * 2f);
			}
			if (MyPlayer.Instance != null && MyPlayer.Instance.character == ECharacter.Athena && enemy.IsStageBoss() &&
				deathSource != null && deathSource.damageSource == EWeapon.Aegis.ToString() && !MyAchievements.IsAchievementDone("a_quinsMask"))
				MyAchievements.TryUnlock("a_quinsMask");
			if (enemy.enemyData != null && enemy.enemyData.enemyName == global::Actors.Enemies.EEnemy.GhostInvincible && !MyAchievements.IsAchievementDone("a_roberto"))
				MyAchievements.TryUnlock("a_roberto");
			if (MyPlayer.Instance != null && MyPlayer.Instance.character == ECharacter.Cl4nk && enemy.IsBoss())
				TryUnlockTimed("a_hatSheriff", elapsed - enemy.teleportTime);
		}

		private static void TryUnlockTimed(string name, float elapsed)
		{
			if (MyAchievements.IsAchievementDone(name)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && elapsed <= achievement.targetValue) MyAchievements.TryUnlock(name);
		}

		private static void OnStageBossDefeated(bool isOpeningPortal)
		{
			if (MyPlayer.Instance == null || SaveManager.Instance?.progression == null || DataManager.Instance == null) return;
			var timed = DataManager.Instance.GetAchievement("a_cursedTome");
			if (timed != null && MyTime.runTimer <= timed.targetValue * 60f) MyAchievements.TryUnlock("a_cursedTome");
			if (MyTime.runTimer <= AchievementGenerator.GetSkinAchValue(ESkinType.Speedrun) * 60f)
				MyAchievements.TryUnlock(AchievementGenerator.GetSkinAchievementName(MyPlayer.Instance.character, ESkinType.Speedrun));
			if (MapController.IsFinalBossStage())
				MyAchievements.TryUnlock(AchievementGenerator.GetSkinAchievementName(MyPlayer.Instance.character, ESkinType.FinalBoss));
			if (MapController.currentMap != null)
			{
				if (MapController.currentMap.eMap == EMap.Forest)
				{
					if (MapController.index == 0) MyAchievements.TryUnlock("a_clank");
					else if (MapController.index == 1) MyAchievements.TryUnlock("a_robinette");
					else if (MapController.index == 2) MyAchievements.TryUnlock("a_chadwell");
				}
				else if (MapController.currentMap.eMap == EMap.Desert)
				{
					if (MapController.index == 0) MyAchievements.TryUnlock("a_ninja");
					else if (MapController.index == 1) { MyAchievements.TryUnlock("a_vlad"); MyAchievements.TryUnlock("a_spaceNoodle"); }
				}
			}
			if (!hasTakenDamageThisRun) MyAchievements.TryUnlock("a_holyBook");
			var curses = DataManager.Instance.GetAchievement("a_speedBoi");
			if (GameManager.Instance != null && curses != null && GameManager.Instance.bossCurses >= curses.targetValue) MyAchievements.TryUnlock("a_speedBoi");
			var goggles = DataManager.Instance.GetAchievement("a_gamerGoggles");
			var health = MyPlayer.Instance.inventory?.playerHealth;
			if (health != null && goggles != null)
			{
				float threshold = goggles.targetValue / 100f;
				if (health.GetHpRatio() <= threshold) MyAchievements.TryUnlock("a_gamerGoggles");
			}
			var items = MyPlayer.Instance.inventory?.itemInventory;
			if (items != null && RunStats.GetStat(EMyStat.powerupsUsed) <= 0 && items.items.Count == 0 && chargedShrines <= 0)
				MyAchievements.TryUnlock("a_heroSword");
			if (MapController.IsFinalBossStage() && items != null && MyPlayer.Instance.character == ECharacter.Amog &&
				items.GetAmount(EItem.MoldyCheese) >= 8 && items.GetAmount(EItem.Snek) > 0)
				MyAchievements.TryUnlock("a_hatCheese");
			var weapons = MyPlayer.Instance.inventory?.weaponInventory;
			if (MapController.IsFinalBossStage() && MapController.currentMap != null && MapController.currentMap.eMap == EMap.Forest &&
				MyPlayer.Instance.character == ECharacter.SirOofie && weapons != null && weapons.GetWeaponLevel(EWeapon.HeroSword) > 0 &&
				weapons.GetWeaponLevel(EWeapon.CorruptSword) > 0 && weapons.GetWeaponLevel(EWeapon.Scythe) > 0)
				MyAchievements.TryUnlock("a_hatMedieval");
		}

		private static void OnStageBossDefeatedInTime(float time)
		{
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyPlayer.Instance == null || MapController.currentMap == null) return;
			const string name = "a_skin_megachadKevin";
			if (MyAchievements.IsAchievementDone(name) || !MapController.IsFinalBossStage() || MyPlayer.Instance.character != ECharacter.Megachad || MapController.currentMap.eMap != EMap.Forest) return;
			HatData hat = MyPlayer.Instance.playerRenderer != null ? MyPlayer.Instance.playerRenderer.GetCurrentHat() : null;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (hat != null && hat.eHat == EHat.Kevin && achievement != null && time <= achievement.targetValue) MyAchievements.TryUnlock(name);
		}

		private static void OnStageBossDefeatedNum(int numBosses)
		{
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyPlayer.Instance == null || MapController.currentMap == null) return;
			const string name = "a_hatHeadset";
			if (MyAchievements.IsAchievementDone(name) || MyPlayer.Instance.character != ECharacter.Ninja || MapController.currentMap.eMap != EMap.Desert || MapController.index != 0) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && numBosses - 1 >= achievement.targetValue) MyAchievements.TryUnlock(name);
		}

		private static void OnDamageTaken(PlayerHealth ph, DamageContainer dc, bool brokeShield)
		{
			noDamageTimer = 0f;
			hasTakenDamageThisRun = true;
		}

		private static void OnEnemyDamaged(Enemy arg1, DamageContainer arg2)
		{
			hasDealtDamageThisRun = true;
		}

		private static void OnLevelUp(int level)
		{
			CheckCharacterLevel("a_armorTome", ECharacter.SirOofie, level);
			CheckCharacterLevel("a_bloodyCleaver", ECharacter.Vlad, level);
			CheckCharacterLevel("a_demonicSoul", ECharacter.Calcium, level);
		}

		private static void CheckCharacterLevel(string name, ECharacter character, int level)
		{
			if (MyAchievements.IsAchievementDone(name)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && level >= achievement.targetValue && MyPlayer.Instance.character == character)
				MyAchievements.TryUnlock(name);
		}

		private static void OnChestBought()
		{
		}

		private static void OnShrineCharged(bool noChargeInterruption)
		{
			chargedShrines++;
			if (noChargeInterruption) chargedShrinesNoInterruptions++;
			if (chargedShrines >= totalChargeShrines && !MyAchievements.IsAchievementDone("a_suckyMagnet"))
				MyAchievements.TryUnlock("a_suckyMagnet");
			if (MapController.index >= 2 && chargedShrinesNoInterruptions >= totalChargeShrines && !MyAchievements.IsAchievementDone("a_chaosTome"))
				MyAchievements.TryUnlock("a_chaosTome");
		}

		private static void OnChargeShrineSpawned()
		{
			totalChargeShrines++;
		}

		private static void OnMicrowaveUsed(EItem eItem)
		{
			if (!MyAchievements.IsAchievementDone("a_noelle"))
			{
				consecutiveIceCrystalCooks = eItem == EItem.IceCrystal ? consecutiveIceCrystalCooks + 1 : 0;
				if (eItem == EItem.IceCrystal && DataManager.Instance != null && MyAchievements.CheckAchievementValue("a_noelle", consecutiveIceCrystalCooks))
					MyAchievements.TryUnlock("a_noelle");
			}
			if (!MyAchievements.IsAchievementDone("a_poisonGloves"))
			{
				consecutiveMoldyCheeseCooks = eItem == EItem.MoldyCheese ? consecutiveMoldyCheeseCooks + 1 : 0;
				if (eItem == EItem.MoldyCheese && DataManager.Instance != null && MyAchievements.CheckAchievementValue("a_poisonGloves", consecutiveMoldyCheeseCooks))
					MyAchievements.TryUnlock("a_poisonGloves");
			}
		}

		private static void OnGhostBossDied()
		{
			if (!MyAchievements.IsAchievementDone("a_scythe")) MyAchievements.TryUnlock("a_scythe");
			if (MyAchievements.IsAchievementDone("a_hatSunglasses") || MyPlayer.Instance.character != ECharacter.Calcium) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement("a_hatSunglasses");
			if (achievement != null && MyTime.stageTimer <= achievement.targetValue) MyAchievements.TryUnlock("a_hatSunglasses");
		}

		private static void OnWeaponAddedOrUpgraded(WeaponBase weapon)
		{
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || weapon?.weaponData == null) return;
			CheckWeaponLevel("a_durationTome", weapon, EWeapon.Axe);
			CheckWeaponLevel("a_wirelessDaggers", weapon, EWeapon.LightningStaff);
			if (MyPlayer.Instance.character == ECharacter.Vlad && weapon.level >= 41 && !MyAchievements.IsAchievementDone("a_wizardsHat"))
				MyAchievements.TryUnlock("a_wizardsHat");
			const string magicHat = "a_hatMagic";
			if (MyPlayer.Instance.character != ECharacter.Fox || MyAchievements.IsAchievementDone(magicHat)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(magicHat);
			var inventory = MyPlayer.Instance.inventory.weaponInventory;
			if (achievement != null && inventory != null && inventory.GetWeaponLevel(EWeapon.FireStaff) >= achievement.targetValue &&
				inventory.GetWeaponLevel(EWeapon.LightningStaff) >= achievement.targetValue && inventory.GetWeaponLevel(EWeapon.BloodMagic) >= achievement.targetValue)
				MyAchievements.TryUnlock(magicHat);
		}

		private static void CheckWeaponLevel(string name, WeaponBase weapon, EWeapon expected)
		{
			if (weapon.weaponData.eWeapon != expected || MyAchievements.IsAchievementDone(name)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && weapon.level >= achievement.targetValue) MyAchievements.TryUnlock(name);
		}

		private static void OnTomeAddedOrUpgraded(ETome eTome, EStat stat)
		{
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyPlayer.Instance?.inventory?.tomeInventory == null) return;
			var inventory = MyPlayer.Instance.inventory.tomeInventory;
			switch (eTome)
			{
				case ETome.Agility: CheckTomeAchievement("a_turboSocks", eTome, inventory); break;
				case ETome.Cooldown: CheckTomeAchievement("a_battery", eTome, inventory); break;
				case ETome.Regeneration: CheckTomeAchievement("a_leechingCrystal", eTome, inventory); break;
				case ETome.Xp: CheckTomeAchievement("a_echoShard", eTome, inventory); break;
				case ETome.Attraction: CheckTomeAchievement("a_shatteredKnowledge", eTome, inventory); break;
				case ETome.Precision: CheckTomeAchievement("a_sniperRifle", eTome, inventory); break;
				case ETome.Blood: CheckTomeAchievement("a_bloodMagic", eTome, inventory); break;
				case ETome.Knockback: CheckTomeAchievement("a_blackHole", eTome, inventory); break;
				case ETome.Luck: CheckTomeAchievement("a_dice", eTome, inventory); break;
				case ETome.Damage: CheckTomeAchievement("a_megachad", eTome, inventory); break;
				case ETome.Thorns: CheckTomeAchievement("a_athena", eTome, inventory); break;
				case ETome.Cursed:
					if (MyTime.runTimer <= 600f) CheckTomeAchievement("a_corruptedSword", eTome, inventory);
					CheckTomeAchievement("a_cursedGloves", eTome, inventory);
					break;
			}
			if (eTome == ETome.Agility || eTome == ETome.Cooldown)
			{
				const string name = "a_turboSkates";
				MyAchievement achievement = DataManager.Instance.GetAchievement(name);
				if (!MyAchievements.IsAchievementDone(name) && achievement != null && inventory.GetTomeLevel(ETome.Agility) >= achievement.targetValue && inventory.GetTomeLevel(ETome.Cooldown) >= achievement.targetValue)
					MyAchievements.TryUnlock(name);
			}
		}

		private static void CheckTomeAchievement(string name, ETome tome, TomeInventory inventory)
		{
			if (!MyAchievements.IsAchievementDone(name) && MyAchievements.CheckAchievementValue(name, inventory.GetTomeLevel(tome)))
				MyAchievements.TryUnlock(name);
		}

		private static void OnItemAdded(EItem item)
		{
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyPlayer.Instance?.inventory?.itemInventory == null) return;
			if (item == EItem.LeechingCrystal) CheckItemAchievement("a_kevin", item);
			if (item == EItem.Pot && MyPlayer.Instance.character == ECharacter.Roberto) CheckItemAchievement("a_hatPot", item);
		}

		private static void CheckItemAchievement(string name, EItem item)
		{
			if (MyAchievements.IsAchievementDone(name)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && MyPlayer.Instance.inventory.itemInventory.GetAmount(item) >= achievement.targetValue)
				MyAchievements.TryUnlock(name);
		}

		private static void OnPickupTriggered(Pickup pickup)
		{
		}

		private static void OnPotBroken()
		{
			const string achievementName = "a_xpTome";
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyAchievements.IsAchievementDone(achievementName)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(achievementName);
			if (achievement != null && RunStats.GetStat("potsBroken") >= achievement.targetValue)
				MyAchievements.TryUnlock(achievementName);
		}

		private static void OnInteracted(BaseInteractable interactable, bool success)
		{
			if (!success || interactable == null) return;
			if (interactable is InteractableTumbleWeed && EffectManager.Instance != null && DataManager.Instance != null &&
				SaveManager.Instance != null && SaveManager.Instance.progression != null && GameManager.Instance != null)
			{
				if (!MyAchievements.IsAchievementDone("a_katana") && MapController.index == 0 &&
					UnityEngine.Random.Range(0f, 1f) <= MyAchievements.GetAchievementTargetValue("a_katana") / 100f)
					EffectManager.Instance.TrySpawnKatanaQuest(global::Utility.RaycastUtility.RayToGround(interactable.transform.position) + UnityEngine.Vector3.up * 2f);
				if (!MyAchievements.IsAchievementDone("a_shotgun") && MapController.index == 1 &&
					UnityEngine.Random.Range(0f, 1f) <= MyAchievements.GetAchievementTargetValue("a_shotgun") / 100f)
					EffectManager.Instance.TrySpawnShotgunQuest(global::Utility.RaycastUtility.RayToGround(interactable.transform.position) + UnityEngine.Vector3.up * 2f);
			}
			if (interactable is InteractableBoombox && !MyAchievements.IsAchievementDone("a_boombox"))
			{
				numBoomboxes++;
				if (numBoomboxes >= MyAchievements.GetAchievementTargetValue("a_boombox")) MyAchievements.TryUnlock("a_boombox");
			}
		}

		private static void OnCryptSpeedrun(float cryptTime)
		{
			if (!RsgController.isCurrentMapRandomSeed) return;
			string name = AchievementGenerator.GetSkinAchievementName(MyPlayer.Instance.character, ESkinType.Crypt);
			if (MyAchievements.IsAchievementDone(name)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && cryptTime <= achievement.targetValue) MyAchievements.TryUnlock(name);
		}

		private static void OnInteractableUsedDebug(string debugName)
		{
			CheckPotUnlock(debugName);
			CheckPumpkinUnlock(debugName);
		}

		private static void OnMainMenuOpened()
		{
			CheckCrownAchievement();
		}

		private static void CheckPotUnlock(string debugName)
		{
			if (GameManager.Instance == null || !GameManager.Instance.isCrypt || GameManager.Instance.cryptIndex != 0 ||
				!RsgController.isCurrentMapRandomSeed || Challenges.ChallengesTracker.HasChallengeModifier("no_items")) return;
			if (debugName != InteractableChest.debugNameCrypt && debugName != InteractablePot.debugNameCrypt) return;
			if (InteractablesStatus.interactablesByName.TryGetValue(InteractableChest.debugNameCrypt, out var chests) &&
				InteractablesStatus.interactablesByName.TryGetValue(InteractablePot.debugNameCrypt, out var pots) &&
				chests.IsDone() && pots.IsDone() && SaveManager.Instance?.progression != null && DataManager.Instance != null)
				MyAchievements.TryUnlock("a_potSteel");
		}

		private static void CheckPumpkinUnlock(string debugName)
		{
			if (MyPlayer.Instance == null || MyPlayer.Instance.character != ECharacter.Roberto || debugName != InteractablePot.debugGraveyardName) return;
			if (InteractablesStatus.interactablesByName.TryGetValue(InteractablePot.debugGraveyardName, out var pumpkins) &&
				pumpkins.IsDone() && SaveManager.Instance?.progression != null && DataManager.Instance != null)
				MyAchievements.TryUnlock("a_pumpkin");
		}

		private static void OnUnlock(MyAchievement achUnlocked)
		{
			const string target = "a_allQuests";
			if (SaveManager.Instance?.progression == null || DataManager.Instance?.unsortedAchievements == null || MyAchievements.IsAchievementDone(target)) return;
			foreach (var achievement in DataManager.Instance.unsortedAchievements)
			{
				if (achievement == null) return;
				if (achievement.internalName != target && !MyAchievements.IsAchievementDone(achievement.internalName)) return;
			}
			MyAchievements.TryUnlock(target);
		}

		private static void OnPurchased(UnlockableBase unlockable)
		{
			const string target = "a_unlockEverything";
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || MyAchievements.IsAchievementDone(target)) return;
			foreach (var item in DataManager.Instance.GetAllPurchasable())
				if (item != null && item.isEnabled && !MyAchievements.IsPurchased(item)) return;
			MyAchievements.TryUnlock(target);
		}

		public static void CheckCrownAchievement()
		{
			if (SaveManager.Instance?.progression == null || DataManager.Instance == null || global::Inventory__Items__Pickups.XpUtility.xpForLevelsTable == null) return;
			const string name = "a_hatCrown";
			if (MyAchievements.IsAchievementDone(name)) return;
			foreach (ECharacter character in System.Enum.GetValues(typeof(ECharacter)))
				if (!SaveManager.Instance.progression.GetCharacterProgression(character).HasStar()) return;
			MyAchievements.TryUnlock(name);
		}

		private static void OnLateFixedUpdate()
		{
			if (GameManager.Instance == null || MapController.currentMap == null || MapController.currentMap.eMap != EMap.Forest || SaveManager.Instance?.progression == null || DataManager.Instance == null) return;
			int kills = RunStats.GetStat(EMyStat.kills);
			CheckForestKills("a_hatTophat", kills);
			CheckForestKills("a_hatTophatLong", kills);
		}

		private static void CheckForestKills(string name, int kills)
		{
			if (MyAchievements.IsAchievementDone(name)) return;
			MyAchievement achievement = DataManager.Instance.GetAchievement(name);
			if (achievement != null && kills >= achievement.targetValue) MyAchievements.TryUnlock(name);
		}
	}
}
