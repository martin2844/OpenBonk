using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts._Data.Progression;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Inventory__Items__Pickups.Chests;

namespace Assets.Scripts.Saves___Serialization.Progression.Stats
{
	public static class TrackStats
	{
		public static Action A_PotBroken;

		private static string minesWeaponDamageSource = EWeapon.Mine.ToString();

		private static string tornadoDamageSource = EWeapon.Tornado.ToString();

		private static HashSet<EPickup> nonPowerupPickups = new HashSet<EPickup> { EPickup.Gold, EPickup.Silver, EPickup.Xp };

		private static Dictionary<EMyStat, string> statStrings;

		public static void Init()
		{
			RunStats.Init();
			Enemy.A_EnemyDied -= OnEnemyDied;
			PlayerXp.A_XpAdded -= OnXpAdded;
			PlayerInventory.A_GoldChange -= OnGoldChange;
			ProgressionSaveFile.A_SilverChanged -= OnSilverChange;
			InteractableChest.A_ChestOpened -= OnChestOpened;
			InteractableChest.A_ChestBought -= OnChestBought;
			ItemInventory.A_ItemAdded -= OnItemAdded;
			PlayerHealth.A_Evaded -= OnEvade;
			PlayerHealth.A_TakeDamage -= OnPlayerTakeDamage;
			InteractableShadyGuy.A_ShadyGuyDone -= OnShadyGuyUsed;
			PlayerHealth.A_LifestealHealing -= OnLifestealHealing;
			Enemy.A_Damage -= OnEnemyDamage;
			WeaponAttack.A_SpawnedProjectileSuccessfully -= OnProjectileSpawned;
			PlayerHealth.A_Died -= OnDead;
			ItemIceCube.A_FreezeEnemy -= OnIcecubeFreezeEnemy;
			InteractableShrineChallenge.A_Completed -= OnChallengeShrineCompleted;
			InteractableMicrowave.A_Exploded -= OnMicrowaveExploded;
			ItemKevin.A_PunchedByKevin -= OnPunchedByKevin;
			MyAchievements.A_Unlocked -= OnAchievementUnlocked;
			ProgressionSaveFile.A_UnlockablePurchased -= OnUnlockPurchased;
			DetectInteractables.A_Interacted -= OnInteracted;
			ChargeShrine.A_Charged -= OnShrineCharged;
			Pickup.A_PickupTriggered -= OnPickup;
			Enemy.A_EnemyDied += OnEnemyDied;
			PlayerXp.A_XpAdded += OnXpAdded;
			PlayerInventory.A_GoldChange += OnGoldChange;
			ProgressionSaveFile.A_SilverChanged += OnSilverChange;
			InteractableChest.A_ChestOpened += OnChestOpened;
			InteractableChest.A_ChestBought += OnChestBought;
			ItemInventory.A_ItemAdded += OnItemAdded;
			PlayerHealth.A_Evaded += OnEvade;
			PlayerHealth.A_TakeDamage += OnPlayerTakeDamage;
			InteractableShadyGuy.A_ShadyGuyDone += OnShadyGuyUsed;
			PlayerHealth.A_LifestealHealing += OnLifestealHealing;
			Enemy.A_Damage += OnEnemyDamage;
			WeaponAttack.A_SpawnedProjectileSuccessfully += OnProjectileSpawned;
			PlayerHealth.A_Died += OnDead;
			ItemIceCube.A_FreezeEnemy += OnIcecubeFreezeEnemy;
			InteractableShrineChallenge.A_Completed += OnChallengeShrineCompleted;
			InteractableMicrowave.A_Exploded += OnMicrowaveExploded;
			ItemKevin.A_PunchedByKevin += OnPunchedByKevin;
			MyAchievements.A_Unlocked += OnAchievementUnlocked;
			ProgressionSaveFile.A_UnlockablePurchased += OnUnlockPurchased;
			DetectInteractables.A_Interacted += OnInteracted;
			ChargeShrine.A_Charged += OnShrineCharged;
			Pickup.A_PickupTriggered += OnPickup;
		}

		public static void Cleanup()
		{
			RunStats.Cleanup();
			Enemy.A_EnemyDied -= OnEnemyDied;
			PlayerXp.A_XpAdded -= OnXpAdded;
			PlayerInventory.A_GoldChange -= OnGoldChange;
			ProgressionSaveFile.A_SilverChanged -= OnSilverChange;
			InteractableChest.A_ChestOpened -= OnChestOpened;
			InteractableChest.A_ChestBought -= OnChestBought;
			ItemInventory.A_ItemAdded -= OnItemAdded;
			PlayerHealth.A_Evaded -= OnEvade;
			PlayerHealth.A_TakeDamage -= OnPlayerTakeDamage;
			InteractableShadyGuy.A_ShadyGuyDone -= OnShadyGuyUsed;
			PlayerHealth.A_LifestealHealing -= OnLifestealHealing;
			Enemy.A_Damage -= OnEnemyDamage;
			WeaponAttack.A_SpawnedProjectileSuccessfully -= OnProjectileSpawned;
			PlayerHealth.A_Died -= OnDead;
			ItemIceCube.A_FreezeEnemy -= OnIcecubeFreezeEnemy;
			InteractableShrineChallenge.A_Completed -= OnChallengeShrineCompleted;
			InteractableMicrowave.A_Exploded -= OnMicrowaveExploded;
			ItemKevin.A_PunchedByKevin -= OnPunchedByKevin;
			MyAchievements.A_Unlocked -= OnAchievementUnlocked;
			ProgressionSaveFile.A_UnlockablePurchased -= OnUnlockPurchased;
			DetectInteractables.A_Interacted -= OnInteracted;
			ChargeShrine.A_Charged -= OnShrineCharged;
			Pickup.A_PickupTriggered -= OnPickup;
		}

		private static void AddValue(EMyStat stat, int value)
		{
			RunStats.AddValue(stat, value);
			MyStats.AddValue(stat, value);
		}

		private static void AddValue(string statKey, int value)
		{
			RunStats.AddValue(statKey, value);
			MyStats.AddValue(statKey, value);
		}

		private static void OnGoldChange(PlayerInventory inv, int amount)
		{
			if (amount > 0) AddValue(EMyStat.goldEarned, amount);
			else AddValue(EMyStat.goldSpent, -amount);
		}

		private static void OnSilverChange(int change)
		{
			if (change > 0) AddValue(EMyStat.silverEarned, change);
		}

		private static void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
		{
			AddValue(EMyStat.kills, 1);
			if (enemy == null) return;
			if (enemy.IsElite()) AddValue(EMyStat.eliteKills, 1);
			if (enemy.IsBoss() && !enemy.IsStageBoss())
			{
				AddValue(EMyStat.minibossKills, 1);
				if (Assets.Scripts.Actors.Player.MyPlayer.Instance?.inventory?.characterData != null && Assets.Scripts.Actors.Player.MyPlayer.Instance.inventory.characterData.eCharacter == ECharacter.Calcium)
					AddValue(EMyStat.minibossKillsCalcium, 1);
			}
			if (enemy.IsStageBoss())
			{
				AddValue(EMyStat.bossKills, 1);
				if (MapController.IsFinalBossStage()) AddValue(EMyStat.finalBossKills, 1);
			}
			if (enemy.enemyData != null)
			{
				if (StatTrackingUtility.IsSkeleton(enemy)) AddValue(EMyStat.skeletonKills, 1);
				if (StatTrackingUtility.IsGoblin(enemy)) AddValue(EMyStat.goblinKills, 1);
				if (StatTrackingUtility.IsWisp(enemy) && Assets.Scripts.Actors.Player.MyPlayer.Instance?.inventory?.characterData != null &&
					Assets.Scripts.Actors.Player.MyPlayer.Instance.inventory.characterData.eCharacter == ECharacter.Fox)
					AddValue(EMyStat.foxWispsKills, 1);
				AddValue(StatTrackingUtility.GetKeyKillsEnemy(enemy), 1);
			}
			if (deathSource != null && !string.IsNullOrEmpty(deathSource.damageSource))
			{
				AddValue(StatTrackingUtility.GetKeyKillsSource(deathSource), 1);
				if (enemy.enemyData != null && enemy.enemyData.enemyName == global::Actors.Enemies.EEnemy.CactusShooter && deathSource.damageSource == PlayerHealth.thornsDamageSource)
					AddValue(EMyStat.cactusKillsWithThorns, 1);
				if (Assets.Scripts.Actors.Player.MyPlayer.Instance?.inventory?.characterData != null &&
					Assets.Scripts.Actors.Player.MyPlayer.Instance.inventory.characterData.eCharacter == ECharacter.Athena &&
					deathSource.damageSource == ItemQuinsMask.damageSource)
					AddValue(EMyStat.quinsMaskKillsAsAthena, 1);
				if (Assets.Scripts.Actors.Player.MyPlayer.Instance?.inventory?.characterData != null &&
					Assets.Scripts.Actors.Player.MyPlayer.Instance.inventory.characterData.eCharacter == ECharacter.Birdo &&
					deathSource.damageSource == minesWeaponDamageSource)
					AddValue(EMyStat.birdoFlyingMinesKills, 1);
				if (deathSource.element == EElement.Fire) AddValue(EMyStat.fireKills, 1);
				else if (deathSource.element == EElement.Lightning) AddValue(EMyStat.lightningKills, 1);
			}
			var movement = Assets.Scripts.Actors.Player.MyPlayer.Instance?.playerMovement;
			if (movement != null)
			{
				if (movement.GetAverageVelocity() < 0.1f) AddValue(EMyStat.standingStillKills, 1);
				if (movement.isTouchingTornado && deathSource != null && deathSource.damageSource == tornadoDamageSource)
					AddValue(EMyStat.killsInTornadoWithTornado, 1);
			}
			if (Assets.Scripts.Actors.Player.MyPlayer.Instance != null)
				AddValue(StatTrackingUtility.GetKeyKillsCharacter(Assets.Scripts.Actors.Player.MyPlayer.Instance.character), 1);
		}

		private static void OnEnemyDamage(Enemy enemy, DamageContainer dc)
		{
			if (dc.crit) AddValue(EMyStat.crits, 1);
		}

		private static void OnChestOpened()
		{
			AddValue(EMyStat.chestsOpened, 1);
		}

		private static void OnItemAdded(EItem eItem)
		{
			AddValue(EMyStat.itemsPickedUp, 1);
		}

		private static void OnXpAdded(PlayerXp playerXp, int amount)
		{
			AddValue(EMyStat.xpGained, amount);
		}

		private static void OnAchievementUnlocked(MyAchievement ach)
		{
			RunStats.AddAchievement(ach);
			MyStats.SetValueForce(EMyStat.questsCompleted.ToString(), SaveManager.Instance.progression.achievements.Count);
			if (ach is ChallengeData)
			{
				MyAchievements.GetAchievementTypeProgress(EAchievementType.Challenges, out int completed, out _, out _);
				MyStats.SetValueForce(EMyStat.challengesCompleted.ToString(), completed);
			}
		}

		private static void OnUnlockPurchased(UnlockableBase unlock)
		{
			MyStats.SetValueForce(EMyStat.unlocksPurchased.ToString(), SaveManager.Instance.progression.purchases.Count);
		}

		private static void OnProjectileSpawned(ProjectileBase projectileBase)
		{
			AddValue(EMyStat.projectilesFired, 1);
		}

		private static void OnInteracted(BaseInteractable interactable, bool success)
		{
			if (success && interactable is InteractablePot) { AddValue(EMyStat.potsBroken, 1); A_PotBroken?.Invoke(); }
			if (success && interactable is InteractableShrineMagnet) AddValue(EMyStat.shrineSucc, 1);
		}

		private static void OnShrineCharged(bool notInterrupted)
		{
			AddValue(EMyStat.shrineCharge, 1);
			if (MapController.currentMap != null && MapController.currentMap.eMap == EMap.Desert && MapEventsDesert.IsActiveStorm() && MyTime.stageTimer >= MapEventsDesert.currentStormStartedAtTime + 2f)
				AddValue(EMyStat.shrineChargeSandstorm, 1);
		}

		private static void OnChallengeShrineCompleted()
		{
			AddValue(EMyStat.shrineChallenge, 1);
		}

		private static void OnChestBought()
		{
			AddValue(EMyStat.chestsBought, 1);
		}

		private static void OnShadyGuyUsed(InteractableShadyGuy shadyGuy)
		{
			if (shadyGuy == null) return;
			switch (shadyGuy.rarity)
			{
				case EItemRarity.Common: AddValue(EMyStat.shadyGuysBlack, 1); break;
				case EItemRarity.Rare: AddValue(EMyStat.shadyGuysBlue, 1); break;
				case EItemRarity.Epic: AddValue(EMyStat.shadyGuysPink, 1); break;
				case EItemRarity.Legendary: AddValue(EMyStat.shadyGuysGold, 1); break;
			}
		}

		private static void OnPlayerTakeDamage(PlayerHealth ph, DamageContainer dc, bool brokeShield)
		{
			if (dc == null || dc.damageBlockedByArmor <= 0) return;
			AddValue(EMyStat.damageReductionArmor, dc.damageBlockedByArmor);
			if (Assets.Scripts.Actors.Player.MyPlayer.Instance != null && Assets.Scripts.Actors.Player.MyPlayer.Instance.character == ECharacter.SirOofie)
				AddValue(EMyStat.damageReductionArmorAsKnight, dc.damageBlockedByArmor);
		}

		private static void OnEvade(Enemy attacker)
		{
			AddValue(EMyStat.evades, 1);
		}

		private static void OnLifestealHealing(int amount)
		{
			AddValue(EMyStat.lifestealHealing, amount);
		}

		private static void OnDead()
		{
			AddValue(EMyStat.runs, 1);
		}

		private static void OnIcecubeFreezeEnemy()
		{
			AddValue(EMyStat.icecubeFreezes, 1);
		}

		private static void OnPickup(Pickup pickup)
		{
			if (!nonPowerupPickups.Contains(pickup.ePickup)) AddValue(EMyStat.powerupsUsed, 1);
		}

		private static void OnMicrowaveExploded()
		{
			AddValue(EMyStat.microwavesExploded, 1);
		}

		private static void OnPunchedByKevin(int times)
		{
			AddValue(EMyStat.kevinPunches, times);
		}

		public static string GetStatString(EMyStat stat)
		{
			if (statStrings == null) statStrings = new Dictionary<EMyStat, string>();
			if (!statStrings.TryGetValue(stat, out string key)) statStrings.Add(stat, key = stat.ToString());
			return key;
		}

		public static string GetCharacterRunsString(ECharacter character)
		{
			return "runsAs" + character;
		}
	}
}
