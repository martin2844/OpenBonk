using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemCowardsCloak : ItemBase
	{
		private float speedPerAmount = .05f;

		private float speedPerStack = .3f;

		private int maxStacks = 2;

		private int stacksPerAmount = 2;

		private float extraDurationPerAmount;

		private float stacksResetAtTime;

		private int stacks;

		protected override void OnInitOrAmountChanged()
		{
			RefreshStats();
		}

		private void OnDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
		{
			if (dc.damage > 0f) AddTemporaryStack();
		}

		private void AddTemporaryStack()
		{
			if (stacks < maxStacks) stacks++;
			stacksResetAtTime = MyTime.time + 3f + (amount * .5f - 1f);
			RefreshStats();
		}

		public override void Tick()
		{
			if (stacks > 0 && MyTime.time > stacksResetAtTime) { stacks = 0; RefreshStats(); }
		}

		private void RefreshStats()
		{
			SetStat(new StatModifier { stat = EStat.MoveSpeedMultiplier, modifyType = EStatModifyType.Flat, modification = speedPerAmount * amount });
			SetStat(new StatModifier { stat = EStat.MoveSpeedMultiplier, modifyType = EStatModifyType.Multiplication, modification = 1f + speedPerStack * stacks });
		}

		public override void Init()
		{
			PlayerHealth.A_TakeDamage += OnDamage;
		}

		public override void Cleanup()
		{
			// The shipped binary adds the handler again here. Unsubscribe to avoid
			// retaining removed items and accumulating handlers across runs.
			PlayerHealth.A_TakeDamage -= OnDamage;
		}

		public ItemCowardsCloak(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
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

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string> { ["stat1"] = EnumUtility.EnumToReadable(EStat.MoveSpeedMultiplier) ?? "", ["value1"] = string.Format("+{0}%", speedPerAmount * 100f) });
		}
	}
}
