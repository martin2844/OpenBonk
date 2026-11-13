using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGoldenShield : ItemBase
	{
		private float chancePerAmount = 1f;

		private float chance;

		private int extraGoldFromOverload;

		private int goldPerAmount = 6;

		private int goldOnHit;

		private float cooldown = .1f;

		private float readyAtTime;

		private float nextSelfDamageReadyTime;

		private float selfDamageCooldown = .2f;

		protected override void OnInitOrAmountChanged()
		{
			chance = chancePerAmount * amount;
			var inventory = MyPlayer.Instance != null ? MyPlayer.Instance.inventory : null;
			int level = inventory != null && inventory.playerXp != null ? inventory.GetCharacterLevel() : 0;
			goldOnHit = (goldPerAmount + Mathf.Min((int)(level * .2f), 20)) * amount;
		}

		private void OnPlayerTakeDamage(PlayerHealth playerHealth, DamageContainer dc, bool b)
		{
			if (readyAtTime > MyTime.time || dc == null) return;
			float rewardScale = 1f;
			if (PlayerHealth.selfDamageSources.Contains(dc.damageSource))
			{
				if (nextSelfDamageReadyTime > MyTime.time) return;
				nextSelfDamageReadyTime = MyTime.time + selfDamageCooldown;
				rewardScale = .1f;
			}
			readyAtTime = MyTime.time + cooldown;
			int gold = Mathf.Max(Mathf.RoundToInt(goldOnHit * rewardScale), 1);
			if (MyPlayer.Instance != null && PoolManager.Instance != null && PoolManager.Instance.goldPool != null)
				MoneyUtility.SpawnMoney(gold, MyPlayer.Instance.transform.position);
		}

		private int GetGold()
		{
			return goldOnHit;
		}

		public override void Init()
		{
			PlayerHealth.A_TakeDamage += OnPlayerTakeDamage;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_TakeDamage -= OnPlayerTakeDamage;
		}

		public ItemGoldenShield(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Tick()
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}
	}
}
