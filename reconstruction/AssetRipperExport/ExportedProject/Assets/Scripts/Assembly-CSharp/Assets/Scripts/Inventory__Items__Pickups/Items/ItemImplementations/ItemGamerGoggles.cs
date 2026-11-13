using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGamerGoggles : ItemBase
	{
		private float maxDamagePerAmount = 1f;

		private float maxDamage;

		private float updateCooldown = 1f;

		private float nextUpdateTime;

		private float lastValue = -1f;

		protected override void OnInitOrAmountChanged()
		{
			maxDamage = amount * maxDamagePerAmount;
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + updateCooldown;
			var health = MyPlayer.Instance.inventory.playerHealth;
			float healthRatio = (float)health.GetCombinedHp() / health.GetCombinedMaxHp();
			float damage = healthRatio < .5f ? (.5f - healthRatio) * 2f * maxDamage : 0f;
			if (Mathf.Abs(lastValue - damage) < .02f) return;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = damage });
			lastValue = damage;
		}

		public ItemGamerGoggles(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
		}

		public override void Cleanup()
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "" };
		}
	}
}
