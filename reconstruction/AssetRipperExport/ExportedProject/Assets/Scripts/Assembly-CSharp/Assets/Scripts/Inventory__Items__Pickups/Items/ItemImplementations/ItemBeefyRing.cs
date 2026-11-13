using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBeefyRing : ItemBase
	{
		private int maxHpPerStack = 10;

		private float powerPerHpPerAmount = .002f;

		private int lastStoredMaxHp;

		private float nextUpdateTime;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Flat, modification = maxHpPerStack * amount });
			// The item is removed before its next timed refresh, so invalidate
			// the cached damage contribution when its last stack disappears.
			if (amount <= 0)
				SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = 0f });
			nextUpdateTime = MyTime.time + 1f;
		}

		private void RefreshPower()
		{
			int maxHp = MyPlayer.Instance.inventory.playerHealth.GetCombinedMaxHp();
			if (maxHp == lastStoredMaxHp) return;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = maxHp * powerPerHpPerAmount * amount });
			lastStoredMaxHp = maxHp;
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + 1f;
			RefreshPower();
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public ItemBeefyRing(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> {
				["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "",
				["stat2"] = EnumUtility.EnumToReadable(EStat.MaxHealth) ?? "",
				["value1"] = string.Format("+{0}%", powerPerHpPerAmount * 100f * 100f),
				["value2"] = string.Format("{0}", 100)
			};
		}
	}
}
