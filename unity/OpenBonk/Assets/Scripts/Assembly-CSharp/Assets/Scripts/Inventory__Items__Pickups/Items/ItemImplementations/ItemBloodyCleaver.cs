using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBloodyCleaver : ItemBase
	{
		private int bloodmarkStacksPerLifestealPerAmount = 1;

		private int bloodmarkStacksPerLifesteal;

		private float bloodmarkChancePerAmount = .5f;

		private float totalBloodmarkChance;

		private static string damageSource = EDebuff.Bloodmark.ToString();

		private DamageContainer dc = new DamageContainer(0f, damageSource);

		private Dictionary<Enemy, int> lifestealProcTracker = new Dictionary<Enemy, int>();

		protected override void OnInitOrAmountChanged()
		{
			bloodmarkStacksPerLifesteal = amount * bloodmarkStacksPerLifestealPerAmount;
			totalBloodmarkChance = amount * bloodmarkChancePerAmount;
		}

		public override void Init()
		{
			PlayerHealth.A_LifestealProc += OnLifestealProc;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_LifestealProc -= OnLifestealProc;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (global::Utility.MyRandom.random.NextDouble() >= dc.procCoefficient * totalBloodmarkChance) return;
			int stacks = bloodmarkChancePerAmount > 0f ? Mathf.FloorToInt(totalBloodmarkChance) : 0;
			if (global::Utility.MyRandom.random.NextDouble() < dc.procCoefficient * (totalBloodmarkChance - stacks)) stacks++;
			for (int i = 0; i < stacks; i++) dc.enemy.AddDebuff(EDebuff.Bloodmark, dc, 5f);
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		private void OnLifestealProc(Enemy enemy, int lifestealAmount)
		{
			if (lifestealAmount <= 0 || enemy.IsDeadOrDyingNextFrame()) return;
			lifestealProcTracker.TryGetValue(enemy, out int previous);
			lifestealProcTracker[enemy] = previous + lifestealAmount;
		}

		public override void Tick()
		{
			foreach (var entry in lifestealProcTracker)
				entry.Key.AddDebuff(EDebuff.Bloodmark, dc, 4f, entry.Value * bloodmarkStacksPerLifesteal);
			lifestealProcTracker.Clear();
		}

		public ItemBloodyCleaver(ItemInventory itemInventoryRef)
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.Lifesteal) ?? "",
				["value1"] = string.Format("{0}%", bloodmarkChancePerAmount * 100f),
				["bloodmark"] = LocalizationUtility.GetLocalizedString("DamageSources", EDebuff.Bloodmark.ToString()) ?? ""
			};
		}
	}
}
