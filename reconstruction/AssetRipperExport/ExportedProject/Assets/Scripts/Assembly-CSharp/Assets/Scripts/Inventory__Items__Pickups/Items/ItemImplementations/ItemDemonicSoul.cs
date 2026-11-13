using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemDemonicSoul : ItemBase
	{
		private static readonly float attackDamagePerStack = .001f;

		private int maxStacksPerAmount;

		private int stacks;

		private int maxStacks;

		private int lastUsedStacks;

		private float nextUpdateTime;

		protected override void OnInitOrAmountChanged()
		{
			maxStacks = amount * maxStacksPerAmount;
			if (stacks > maxStacks) stacks = maxStacks;
		}

		private void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
		{
			if (stacks < maxStacks) stacks = Mathf.Min(stacks + amount, maxStacks);
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + 1f;
			// Refresh decreases as well as increases after removing an item.
			if (stacks == lastUsedStacks) return;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = stacks * attackDamagePerStack });
			lastUsedStacks = stacks;
		}

		public override void Init()
		{
			Enemy.A_EnemyDied += OnEnemyDied;
		}

		public override void Cleanup()
		{
			// The shipped binary adds the callback again during cleanup.
			Enemy.A_EnemyDied -= OnEnemyDied;
		}

		public ItemDemonicSoul(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			maxStacksPerAmount = Mathf.RoundToInt(1f / attackDamagePerStack);
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
			return new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "", ["value1"] = string.Format("{0}%", attackDamagePerStack * 100f), ["value2"] = "100%" };
		}
	}
}
