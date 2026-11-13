using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemKevin : ItemBase
	{
		private float damageChancePerAmount = 0.25f;

		private float damageChance;

		private int numHits;

		public static string damageSource = EItem.Kevin.ToString();

		public static Action<int> A_PunchedByKevin;

		protected override void OnInitOrAmountChanged()
		{
			damageChance = amount * damageChancePerAmount;
		}

		public override void Init()
		{
			Enemy.A_Damage += OnEnemyDamaged;
		}

		public override void Cleanup()
		{
			Enemy.A_Damage -= OnEnemyDamaged;
		}

		private void OnEnemyDamaged(Enemy enemy, DamageContainer dc)
		{
			if (MyPlayer.Instance.inventory.playerHealth.hp >= 2) numHits++;
		}

		private void CheckSelfDamage()
		{
			var health = MyPlayer.Instance.inventory.playerHealth;
			if (health.hp < 2) return;
			bool hadHits = numHits > 0;
			int damage = 0;
			for (int i = 0; i < numHits; i++)
			{
				int guaranteed = Mathf.FloorToInt(damageChance);
				damage += guaranteed;
				if (global::Utility.MyRandom.random.NextDouble() < damageChance - guaranteed) damage++;
			}
			numHits = 0;
			damage = Mathf.Min(damage, health.hp - 1);
			if (damage > 0) health.DamagePlayerExternal(damage, 0f, Vector3.up, true, damageSource, DcFlags.BypassAll);
			A_PunchedByKevin?.Invoke(hadHits ? 1 : 0);
		}

		public override void Tick()
		{
			CheckSelfDamage();
		}

		public ItemKevin(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, string> { ["value1"] = string.Format("+{0}%", damageChancePerAmount * 100f) });
		}
	}
}
