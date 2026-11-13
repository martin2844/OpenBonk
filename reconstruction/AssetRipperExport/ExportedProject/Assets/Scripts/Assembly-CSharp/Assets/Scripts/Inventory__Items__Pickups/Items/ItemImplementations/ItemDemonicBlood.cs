using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemDemonicBlood : ItemBase
	{
		private static readonly float hpPerStack = .5f;

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
			if (stacks < maxStacks) stacks++;
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + 1f;
			// Also refresh decreases after an item is removed; the original only
			// refreshed increasing counts and could retain a stale health bonus.
			if (stacks == lastUsedStacks) return;
			SetStat(new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Flat, modification = stacks * hpPerStack });
			lastUsedStacks = stacks;
		}

		public override void Init()
		{
			Enemy.A_EnemyDied += OnEnemyDied;
		}

		public override void Cleanup()
		{
			// The shipped cleanup adds a duplicate callback instead of removing it.
			Enemy.A_EnemyDied -= OnEnemyDied;
		}

		public ItemDemonicBlood(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			maxStacksPerAmount = Mathf.RoundToInt(100f / hpPerStack);
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

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string> { ["stat1"] = EnumUtility.EnumToReadable(EStat.MaxHealth) ?? "", ["value1"] = string.Format("+{0}", hpPerStack), ["value2"] = string.Format("{0}", maxStacksPerAmount) });
		}
	}
}
