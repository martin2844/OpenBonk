using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemDemonBlade : ItemBase
	{
		private float critChance = .01f;

		private float healChancePerStack = .25f;

		private float totalHealChance;

		protected override void OnInitOrAmountChanged()
		{
			totalHealChance = amount * healChancePerStack;
			SetStat(new StatModifier { stat = EStat.CritChance, modifyType = EStatModifyType.Flat, modification = critChance });
		}

		private void OnEnemyDamaged(Enemy e, DamageContainer dc)
		{
			if (!dc.crit) return;
			var health = MyPlayer.Instance.inventory.playerHealth;
			if (!health.CanLifesteal()) return;
			float healChance = totalHealChance * dc.procCoefficient;
			int healing = Mathf.FloorToInt(healChance);
			if (global::Utility.MyRandom.random.NextDouble() < healChance - healing) healing++;
			if (healing > 0) health.Heal(healing);
		}

		public ItemDemonBlade(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
			Enemy.A_Damage += OnEnemyDamaged;
		}

		public override void Cleanup()
		{
			Enemy.A_Damage -= OnEnemyDamaged;
		}

		public override void Tick()
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
			return localizedString.GetLocalizedString(new Dictionary<string, string> { ["value1"] = string.Format("+{0}%", healChancePerStack * 100f) });
		}
	}
}
