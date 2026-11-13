using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Inventory__Items__Pickups;

namespace Assets.Scripts.Inventory__Items__Pickups.Interactables
{
	public class InteractablePot : BaseInteractable
	{
		public GameObject goldPrefab;

		public GameObject xpPrefab;

		public GameObject hpPrefab;

		public GameObject silverPrefab;

		public GameObject potBreakFx;

		private bool broken;

		public bool isBig;

		public bool isSilver;

		public LocalizedString localizedString;

		private string potLuckMovingStatName;

		private float luckPerPot = 0.005f;

		public bool isInCrypt;

		public static string debugName = "Pots";

		public static string debugNameSilver = "Silver Pots";

		public static string debugNameCrypt = "Crypt Pots";

		public static string debugGraveyardName = "Pumpkin";

#if UNITY_ANDROID && !UNITY_EDITOR
		[System.NonSerialized] public int androidProbeSeed = -1;
#endif

		public override bool Interact()
		{
			if (broken) return false;
			broken = true;
			Vector3 position = transform.position;
			Destroy(gameObject);
			if (potBreakFx != null) Instantiate(potBreakFx, position + Vector3.up, Quaternion.identity);
			if (isSilver)
			{
				if (silverPrefab != null) Instantiate(silverPrefab, position + Vector3.up, Quaternion.identity);
				MoneyUtility.SpawnSilverNoTimerImpact(isBig ? 2 : 1, position);
				return true;
			}
#if UNITY_ANDROID && !UNITY_EDITOR
			if (androidProbeSeed >= 0) global::Utility.MyRandom.random = new System.Random(androidProbeSeed);
#endif
			float roll = (float)global::Utility.MyRandom.random.NextDouble();
			bool noXp = ChallengesTracker.HasChallengeModifier("no_xp");
			if (roll <= 0.45f && !noXp)
			{
				int xp = GetXp();
				int count = Mathf.Min(xp, 10);
				int value = count > 0 ? Mathf.RoundToInt((float)xp / count) : 0;
				if (xpPrefab != null) Instantiate(xpPrefab, position + Vector3.up, Quaternion.identity);
				SpawnStuff(EPickup.Xp, value, 0.25f, count);
			}
			else if (roll > 0.9f && !noXp)
			{
				if (hpPrefab != null) Instantiate(hpPrefab, position + Vector3.up, Quaternion.identity);
				SpawnStuff(EPickup.Health, 1, 0.5f, 1);
			}
			else
			{
				if (goldPrefab != null) Instantiate(goldPrefab, position + Vector3.up, Quaternion.identity);
				MoneyUtility.SpawnMoney(MoneyUtility.GetPotMoney(isBig), position);
			}
			GiveLuck();
			return true;
		}

		private void GiveLuck()
		{
			float amount = (0.5f + MapController.index * 0.5f) * luckPerPot;
			if (isBig) amount *= 1.5f;
			var modifier = new StatModifier { stat = EStat.Luck, modifyType = EStatModifyType.Flat, modification = amount };
			if (MyPlayer.Instance?.inventory?.statInventory != null)
				MyPlayer.Instance.inventory.statInventory.ChangeStat(modifier, true, 0f, false);
			if (UiManager.Instance != null && UiManager.Instance.scoreUi != null)
				UiManager.Instance.scoreUi.AddScore(modifier, true, false, 0.8f);
		}

		private int GetXp()
		{
			var inventory = GameManager.Instance.GetPlayerInventory();
			int baseXp = Mathf.RoundToInt(XpUtility.XpToNextLevelTotal(inventory.playerXp.xp) * 0.075f) + 3;
			float multiplier = isBig ? 2f : 1f;
			if (inventory.itemInventory.GetItem(EItem.Pumpkin) is ItemPumpkin pumpkin)
				multiplier *= pumpkin.GetRewardMultiplier();
			return Mathf.RoundToInt(baseXp * multiplier);
		}

		private void SpawnStuff(EPickup ePickup, int value, float pickupDelay, int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				if (ePickup != EPickup.Xp)
				{
					if (EffectManager.Instance != null) EffectManager.Instance.SpawnPickupOrb(ePickup, transform.position + Vector3.up);
					continue;
				}
				Pickup pickup = PickupManager.Instance.SpawnPickup(ePickup, transform.position + Vector3.up, value, true, pickupDelay);
				if (pickup != null) pickup.StartFollowingPlayer(GameManager.Instance.GetPlayer().transform);
			}
		}

		public override string GetInteractString()
		{
			return localizedString != null ? localizedString.GetLocalizedString() : "Break pot";
		}

		public override bool ShowInDebug()
		{
			return !isSilver;
		}

		public override string GetDebugName()
		{
			if (isSilver) return debugNameSilver;
			if (isInCrypt) return debugNameCrypt;
			return MapController.currentMap != null && MapController.currentMap.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard ? debugGraveyardName : debugName;
		}

		private void OnDisable()
		{
			if (ShowInDebug() && !broken) A_DebugDisable?.Invoke(GetDebugName());
		}
	}
}
