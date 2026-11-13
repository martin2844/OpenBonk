using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney
{
	public class MoneyUtility
	{
		public static int[] moneyTiers = { 20, 5, 1 };

		private const float spawnInterval = 60f;

		private static float nextSilverSpawnTime;

		private static List<int> coins = new List<int>();

		private static Dictionary<GameObject, MoneyFlying> flyingMoneyDict = new Dictionary<GameObject, MoneyFlying>();

		private static MoneyFlying lastSpawnedMoney;

		private static int chestBasePrice = 30;

		private static int priceIncreasePerChest = 35;

		private static int priceIncreasePerChestOver10 = 300;

		private static int priceIncreasePerChestOver20 = 550;

		private static int priceIncreasePerChestOver30 = 1200;

		private static int priceIncreasePerChestOver40 = 2400;

		private static int priceIncreasePerChestOver50 = 4500;

		private static float chestPriceIncrease = 1.22f;

		private static int chestsPurchased;

		private static float bigPotMultiplier = 2f;

		private static float potMoneyFractionOfChest = 0.3f;

		public static Action A_ChestPriceIncreased;

		public static void Init()
		{
			Enemy.A_EnemyDied -= OnEnemyDied;
			Enemy.A_EnemyDied += OnEnemyDied;
			GameManager.A_RunStarted -= OnNewRun;
			GameManager.A_RunStarted += OnNewRun;
			GameManager.A_StageStarted -= OnNewStage;
			GameManager.A_StageStarted += OnNewStage;
		}

		public static void Cleanup()
		{
			Enemy.A_EnemyDied -= OnEnemyDied;
			GameManager.A_RunStarted -= OnNewRun;
			GameManager.A_StageStarted -= OnNewStage;
		}

		private static void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
		{
			CheckSilver(enemy);
			int amount = enemy.GetMoney();
			var inventory = GameManager.Instance.GetPlayerInventory();
			if (inventory.itemInventory.GetItem(EItem.GoldenGlove) is ItemGoldenGlove glove) amount += glove.GetExtraGold();
			if (inventory.statusEffects.HasStatusEffect(Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.Stonks))
				amount = Mathf.RoundToInt(amount * Assets.Scripts.Inventory__Items__Pickups.Pickups.PowerupConstants.GetStonksMultiplier());
			SpawnMoney(amount, enemy.GetCenterPosition());
		}

		private static void CheckSilver(Enemy enemy)
		{
			if (Assets.Scripts.Utility.MyTime.time > nextSilverSpawnTime) SpawnSilver(enemy);
		}

		private static void SpawnSilver(Enemy enemy)
		{
			SpawnSilver(enemy.GetCenterPosition());
		}

		public static void SpawnSilver(Vector3 pos)
		{
			SpawnSilverNoTimerImpact(1, pos);
			nextSilverSpawnTime = Assets.Scripts.Utility.MyTime.time + spawnInterval / PlayerStats.GetStat(EStat.SilverIncreaseMultiplier);
		}

		public static void SpawnSilverNoTimerImpact(int amount, Vector3 pos)
		{
			var pool = PoolManager.Instance.silverPool;
			for (int i = 0; i < amount; i++)
			{
				if (pool.CountInactive == 0 && pool.CountAll >= 30) continue;
				GameObject obj = pool.Get();
				if (obj != null) obj.GetComponent<SilverFlying>().Set(pos);
			}
		}

		private static void OnNewRun()
		{
			chestsPurchased = 0;
			flyingMoneyDict?.Clear();
			lastSpawnedMoney = null;
		}

		private static void OnNewStage()
		{
			flyingMoneyDict.Clear();
			lastSpawnedMoney = null;
		}

		public static List<int> Exchange(int amount)
		{
			coins.Clear();
			foreach (int tier in moneyTiers)
				while (amount >= tier) { coins.Add(tier); amount -= tier; }
			return coins;
		}

		public static void SpawnMoney(int amount, Vector3 pos)
		{
			foreach (int coin in Exchange(amount))
			{
				var pool = PoolManager.Instance.goldPool;
				GameObject obj = pool.CountInactive == 0 && pool.CountAll >= MoneyFlying.maxMoneyObjects ? null : pool.Get();
				if (obj != null)
				{
					if (!flyingMoneyDict.TryGetValue(obj, out MoneyFlying money))
					{
						money = obj.GetComponent<MoneyFlying>();
						if (money == null) continue;
						flyingMoneyDict.Add(obj, money);
					}
					money.Set(coin, pos);
					lastSpawnedMoney = money;
				}
				else if (lastSpawnedMoney != null) lastSpawnedMoney.AddValue(coin);
				else GameManager.Instance.GetPlayerInventory().ChangeGold(coin);
			}
		}

		public static int GetChestPrice()
		{
			int n = chestsPurchased;
			int increase = priceIncreasePerChest * n;
			if (n > 10) increase += (n - 10) * priceIncreasePerChestOver10;
			if (n > 20) increase += (n - 20) * priceIncreasePerChestOver20;
			if (n > 30) increase += (n - 30) * priceIncreasePerChestOver30;
			if (n > 40) increase += (n - 30) * priceIncreasePerChestOver40;
			if (n > 50) increase += (n - 30) * priceIncreasePerChestOver50;
			return Mathf.RoundToInt((chestBasePrice * Mathf.Pow(chestPriceIncrease, n) + increase) * PlayerStats.GetStat(EStat.ChestPriceMultiplier));
		}

		public static int GetShadyguyPrice()
		{
			return Mathf.RoundToInt(chestBasePrice * Mathf.Pow(chestPriceIncrease, chestsPurchased * 0.65f)
				* (MapController.index + 1) * PlayerStats.GetStat(EStat.ChestPriceMultiplier));
		}

		public static int GetItemPriceShadyGuy(EItemRarity rarity)
		{
			float multiplier = rarity == EItemRarity.Rare ? 2f : rarity == EItemRarity.Epic ? 4f : rarity == EItemRarity.Legendary ? 8f : 1f;
			return Mathf.RoundToInt(GetShadyguyPrice() * multiplier);
		}

		public static int GetPotMoney(bool isBig)
		{
			float multiplier = isBig ? bigPotMultiplier : 1f;
			var inventory = GameManager.Instance.GetPlayerInventory();
			if (inventory.itemInventory.GetItem(EItem.Pumpkin) is ItemPumpkin pumpkin)
				multiplier *= pumpkin.GetRewardMultiplier();
			return Mathf.RoundToInt(Mathf.Clamp(GetChestPrice() * potMoneyFractionOfChest, 0f, 1000f) * multiplier);
		}

		public static void IncreaseChestPrice()
		{
			chestsPurchased++;
			A_ChestPriceIncreased?.Invoke();
		}
	}
}
