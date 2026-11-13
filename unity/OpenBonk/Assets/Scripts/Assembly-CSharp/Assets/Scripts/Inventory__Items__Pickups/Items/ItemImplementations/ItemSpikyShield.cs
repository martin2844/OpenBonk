using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemSpikyShield : ItemBase
	{
		private float armorPerAmount = 0.1f;

		private float retaliationPerArmorPerAmount = 200f;

		private float lastStoredArmor;

		private float nextUpdateTime;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.Armor, modifyType = EStatModifyType.Flat, modification = armorPerAmount * amount });
			// The last removal deletes this item before its next timed update.
			// Queue a thorns recalculation so its cached contribution disappears too.
			if (amount <= 0)
				SetStat(new StatModifier { stat = EStat.Thorns, modifyType = EStatModifyType.Flat, modification = 0f });
			nextUpdateTime = MyTime.time + 1f;
		}

		private void UpdateRetaliation()
		{
			float armor = PlayerStats.GetStatRaw(EStat.Armor);
			if (armor == lastStoredArmor) return;
			SetStat(new StatModifier { stat = EStat.Thorns, modifyType = EStatModifyType.Flat, modification = armor * retaliationPerArmorPerAmount * amount });
			lastStoredArmor = armor;
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + 1f;
			UpdateRetaliation();
		}

		public ItemSpikyShield(ItemInventory itemInventoryRef)
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
			return new Dictionary<string, object> {
				["stat1"] = EnumUtility.EnumToReadable(EStat.Thorns), ["stat2"] = EnumUtility.EnumToReadable(EStat.Armor),
				["value1"] = string.Format("+{0}", retaliationPerArmorPerAmount / 100f),
				["value2"] = "1%", ["value3"] = string.Format("{0}%", armorPerAmount * 100f)
			};
		}
	}
}
