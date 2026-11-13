using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGasmask : ItemBase
	{
		private float armorPerStack = .005f;

		private float overhealPerStack = .005f;

		private float maxArmorPerAmount = .4f;

		private float maxOverhealPerAmount = .25f;

		private float maxArmor;

		private float maxOverheal;

		private int lastStoredStacks;

		private float updateInverval = 1f;

		private float nextUpdateTime;

		protected override void OnInitOrAmountChanged()
		{
			nextUpdateTime = MyTime.time + updateInverval;
			maxArmor = amount * maxArmorPerAmount;
			maxOverheal = amount * maxOverhealPerAmount;
			// The cap changed even when the number of poisoned enemies did not.
			lastStoredStacks = -1;
			// The item will not tick after its last stack is removed; notify the
			// stat system now so its previously applied bonuses are cleared.
			if (amount <= 0) UpdateRetaliation();
		}

		private void UpdateRetaliation()
		{
			int count = GetNumPoisonedEnemies();
			if (lastStoredStacks == count) return;
			SetStat(new StatModifier { stat = EStat.Overheal, modifyType = EStatModifyType.Flat, modification = Mathf.Min(count * overhealPerStack, maxOverheal) });
			SetStat(new StatModifier { stat = EStat.Armor, modifyType = EStatModifyType.Flat, modification = Mathf.Min(count * armorPerStack, maxArmor) });
			lastStoredStacks = count;
		}

		private int GetNumPoisonedEnemies()
		{
			return DebuffPoison.numPoisonedEnemies;
		}

		private void OnStageStarted()
		{
			DebuffPoison.numPoisonedEnemies = 0;
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + updateInverval;
			UpdateRetaliation();
		}

		public ItemGasmask(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
			GameManager.A_StageStarted += OnStageStarted;
		}

		public override void Cleanup()
		{
			GameManager.A_StageStarted -= OnStageStarted;
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
				["stat1"] = EnumUtility.EnumToReadable(EStat.Armor) ?? "",
				["stat2"] = EnumUtility.EnumToReadable(EStat.Overheal) ?? ""
			};
		}
	}
}
