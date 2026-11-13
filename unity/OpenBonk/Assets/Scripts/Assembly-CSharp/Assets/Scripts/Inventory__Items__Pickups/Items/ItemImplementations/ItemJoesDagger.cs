using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemJoesDagger : ItemBase
	{
		private float attackDamagePerProcPerAmount = .01f;

		private float attackDamagePerProc = .01f;

		private float executionChance = .01f;

		private float accumulatedDamaged;

		private int stacks;

		private int maxStacks = 999999;

		private int lastUsedStacks;

		private float nextUpdateTime;

		private string damageSource = EItem.JoesDagger.ToString();

		private const float maxRollsPerMinute = 200f;

		private float rollCooldown = .3f;

		private float nextRollTime;

		private int joesProcs;

		private float nextProcTime;

		protected override void OnInitOrAmountChanged()
		{
			attackDamagePerProc = amount * attackDamagePerProcPerAmount;
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + 1f;
			if (stacks <= lastUsedStacks) return;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = accumulatedDamaged });
			lastUsedStacks = stacks;
		}

		private void OnEnemyDamage(Enemy e, DamageContainer dc)
		{
			if (MyTime.time < nextRollTime || e == null || e.IsBoss() || dc == null || !dc.isExecute) return;
			nextRollTime = MyTime.time + rollCooldown;
			stacks++;
			accumulatedDamaged += attackDamagePerProc;
		}

		public override void Init()
		{
			Enemy.A_Damage += OnEnemyDamage;
		}

		public override void Cleanup()
		{
			Enemy.A_Damage -= OnEnemyDamage;
		}

		public ItemJoesDagger(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
			if (dc == null || !dc.canProcJoe || MyTime.time < nextProcTime) return;
			if (global::Utility.MyRandom.random.NextDouble() >= executionChance) return;
			dc.damageSource = damageSource;
			dc.isExecute = true;
			joesProcs++;
			nextProcTime = MyTime.time + rollCooldown;
		}

		public override bool HasPreAttackProc()
		{
			return true;
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
				["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "",
				["value1"] = string.Format("{0}%", executionChance * 100f),
				["value2"] = string.Format("{0}%", attackDamagePerProc * 100f),
				["execute"] = LocalizationUtility.GetLocalizedString("DamageSources", EDamageEffect.Execute.ToString()) ?? ""
			};
		}
	}
}
