using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemPhantomShroud : ItemBase
	{
		private float evasionPerAmount = .05f;
		private float damageMultiplierBase = 2f;
		private float damageMultiplierPerAmount = .5f;
		private float speedAdditionBase = .5f;
		private float speedAdditionPerAmount = .15f;
		private float timeout = 2f;
		private float attackSpeedPerStack = .25f;
		private float damagePerStack = .5f;
		private int stacks;
		private int maxStacks;
		private bool hasEvaded;
		private float speedResetAtTime;
		private bool hasSpeed;

		protected override void OnInitOrAmountChanged()
		{
			maxStacks = amount * 4;
			timeout = Mathf.Min(3f + (amount - 1) * .5f, 6f);
			SetStat(new StatModifier { stat = EStat.Evasion, modifyType = EStatModifyType.Flat, modification = amount * evasionPerAmount });
		}

		private void OnEvade(Enemy enemy)
		{
			hasEvaded = true;
			speedResetAtTime = MyTime.time + timeout;
			if (stacks < maxStacks) stacks++;
			SetStat(new StatModifier { stat = EStat.MoveSpeedMultiplier, modifyType = EStatModifyType.Addition, modification = speedAdditionBase + (amount - 1) * speedAdditionPerAmount });
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = stacks * damagePerStack });
			SetStat(new StatModifier { stat = EStat.AttackSpeed, modifyType = EStatModifyType.Addition, modification = stacks * attackSpeedPerStack });
			hasSpeed = true;
		}

		public override void Tick()
		{
			if (!hasSpeed || MyTime.time <= speedResetAtTime) return;
			SetStat(new StatModifier { stat = EStat.MoveSpeedMultiplier, modifyType = EStatModifyType.Addition, modification = 0f });
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = 0f });
			SetStat(new StatModifier { stat = EStat.AttackSpeed, modifyType = EStatModifyType.Addition, modification = 0f });
			hasSpeed = false;
			stacks = 0;
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
			if (!hasEvaded) return;
			hasEvaded = false;
			itemAttackModifier.AddMultiplier(damageMultiplierBase + (amount - 1) * damageMultiplierPerAmount);
		}
		public override bool HasPreAttackProc() { return true; }
		public override void Init() { PlayerHealth.A_Evaded += OnEvade; }
		public override void Cleanup() { PlayerHealth.A_Evaded -= OnEvade; }
		public ItemPhantomShroud(ItemInventory itemInventoryRef) : base(itemInventoryRef) { }
		public override void ProcOnHitEffects(DamageContainer dc) { }
		public override bool HasOnHitEffectProc() { return false; }
		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, object>
			{
				["value1"] = string.Format("{0}x", damageMultiplierBase),
				["stat1"] = EnumUtility.EnumToReadable(EStat.AttackSpeed) ?? "",
				["stat2"] = EnumUtility.EnumToReadable(EStat.MoveSpeedMultiplier) ?? ""
			});
		}
	}
}
