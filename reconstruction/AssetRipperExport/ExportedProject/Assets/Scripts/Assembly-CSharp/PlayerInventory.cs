using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesActives;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts._Data;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;

public class PlayerInventory
{
	public PlayerStatsNew playerStats;

	public CharacterData characterData;

	public ItemInventory itemInventory;

	public WeaponInventory weaponInventory;

	public PlayerXp playerXp;

	public PlayerStatusEffects statusEffects;

	public PlayerHealth playerHealth;

	public TomeInventory tomeInventory;

	public StatInventory statInventory;

	public PassiveAbility passiveAbility;

	public ActiveAbility activeAbility;

	public static int maxGoldPerFrame = 10000;

	public static int goldThisFrame;

	private bool hasHitGoldCap;

	public int banishes;

	public int refreshes;

	public int skips;

	public static Action<PlayerInventory, int> A_GoldChange;

	public bool pause;

	private int pendingXp;

	public int skipsUsed;

	public int refreshesUsed;

	public int banishesUsed;

	public float gold { get; private set; }

	public int goldInt { get; private set; }

	public PlayerInventory(CharacterData characterData, bool ignoreShopItems = false)
	{
		this.characterData = characterData;
		InitSkipRefreshBanish();
		playerXp = new PlayerXp();
		itemInventory = new ItemInventory();
		weaponInventory = new WeaponInventory();
		statusEffects = new PlayerStatusEffects();
		tomeInventory = new TomeInventory();
		statInventory = new StatInventory();
		playerStats = new PlayerStatsNew(this, ignoreShopItems);
		playerHealth = new PlayerHealth(playerStats);
		passiveAbility = PassiveAbilityFactory.CreatePassiveAbility(characterData.passive);
		passiveAbility.Init();
		ProjectileScythe.nextHitIsBig = false;
		activeAbility = ActiveAbilityFactory.CreateAbility(EAbiltiyActive.Dash);
		activeAbility?.Init();
		if (!ignoreShopItems) weaponInventory.AddWeapon(characterData.weapon, null);
	}

	public void PhysicsTick()
	{
		if (pause) return;
		weaponInventory.Tick();
		itemInventory.Tick();
		playerStats.Tick();
		statusEffects.Tick();
		playerHealth.Tick();
		statInventory.Tick();
		passiveAbility.Tick();
		activeAbility?.Tick();
	}

	public void Update()
	{
		hasHitGoldCap = false;
		goldThisFrame = 0;
	}

	public void AddUpgrade(IUpgradable upgradable, List<StatModifier> upgradeOffer, ERarity rarity)
	{
		if (upgradable.GetType() == typeof(TomeData)) tomeInventory.AddTome((TomeData)upgradable, upgradeOffer, rarity);
		else weaponInventory.AddWeapon((WeaponData)upgradable, upgradeOffer);
	}

	public void ChangeGold(int amount)
	{
		if (hasHitGoldCap) return;
		float change = amount > 0 ? amount * playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.GoldIncreaseMultiplier) : amount;
		if (change > 0f)
		{
			if (Mathf.RoundToInt(change) + goldThisFrame > maxGoldPerFrame)
			{
				hasHitGoldCap = true;
				change = maxGoldPerFrame - goldThisFrame;
			}
			goldThisFrame += Mathf.FloorToInt(change);
		}
		gold = Mathf.Min(2147483648f, gold + change);
		int previous = goldInt;
		goldInt = Mathf.FloorToInt(gold);
		A_GoldChange?.Invoke(this, goldInt - previous);
	}

	public void AddXp(int amount)
	{
		if (!ChallengesTracker.HasChallengeModifier("no_xp") && !playerHealth.IsDead()) pendingXp += amount;
	}

	public void LateUpdate()
	{
		if (pendingXp > 0)
		{
			playerXp.AddXp(pendingXp);
			pendingXp = 0;
		}
	}

	public void AddSilver(int amount)
	{
		if (!playerHealth.IsDead()) SaveManager.Instance.progression.AddSilver(amount);
	}

	public void AddLevel()
	{
		var currentXp = Assets.Scripts.Actors.Player.MyPlayer.Instance?.inventory?.playerXp;
		if (currentXp != null)
			AddXp(Inventory__Items__Pickups.XpUtility.XpToNextLevelTotal(currentXp.xp));
	}

	public int GetCharacterLevel()
	{
		return playerXp.level;
	}

	public bool HasPassive(EPassive passive)
	{
		return passiveAbility != null && passiveAbility.GetPassiveType() == passive;
	}

	private void InitSkipRefreshBanish()
	{
		if (SaveManager.Instance.config.cfGameSettings.pege_mode == 1)
		{
			banishes = refreshes = skips = 0;
			return;
		}
		int runs = (int)Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat("runs");
		var refreshData = DataManager.Instance.GetShopItemData(Assets.Scripts._Data.ShopItems.EShopItem.Refresh);
		var skipData = DataManager.Instance.GetShopItemData(Assets.Scripts._Data.ShopItems.EShopItem.Skip);
		var banishData = DataManager.Instance.GetShopItemData(Assets.Scripts._Data.ShopItems.EShopItem.Banish);
		refreshes = Mathf.Min(refreshData.GetLevel(), refreshData.maxLevel) + (runs > 5 ? 1 : 0);
		skips = Mathf.Min(skipData.GetLevel(), skipData.maxLevel) + (runs > 2 ? 1 : 0);
		banishes = Mathf.Min(banishData.GetLevel(), banishData.maxLevel) + (runs > 8 ? 1 : 0);
		skipsUsed = refreshesUsed = banishesUsed = 0;
	}

	public void Cleanup()
	{
		itemInventory.Cleanup();
		weaponInventory.Cleanup();
		statusEffects.OnDestroy();
		playerStats.OnDestroy();
		playerHealth.OnDestroy();
		passiveAbility.Cleanup();
	}
}
