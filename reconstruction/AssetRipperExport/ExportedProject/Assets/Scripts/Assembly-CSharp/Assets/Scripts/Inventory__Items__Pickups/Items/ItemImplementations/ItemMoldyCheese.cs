using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using UnityEngine;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using System.Collections.Generic;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemMoldyCheese : ItemBase
	{
		private float chanceToStackPerAmount;

		private float totalChance;

		private int procsSinceLastTick;

		public ItemMoldyCheese(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			chanceToStackPerAmount = 0.4f;
		}

		protected override void OnInitOrAmountChanged()
		{
			totalChance = amount * chanceToStackPerAmount;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (totalChance <= 0f) return;
			int stacks = Mathf.FloorToInt(totalChance);
			float remainder = totalChance - stacks;
			if (remainder > 0f && ItemUtility.TryProc(dc.procCoefficient, remainder)) stacks++;
			if (stacks <= 0) return;
			dc.enemy.AddDebuff(EDebuff.Poison, dc, 5f, stacks);
			procsSinceLastTick += stacks;
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
		{
			MyStats.AddValue(EMyStat.moldyCheeseProcs, procsSinceLastTick);
			procsSinceLastTick = 0;
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value1"] = string.Format("+{0}%", chanceToStackPerAmount * 100f) });
		}
	}
}
