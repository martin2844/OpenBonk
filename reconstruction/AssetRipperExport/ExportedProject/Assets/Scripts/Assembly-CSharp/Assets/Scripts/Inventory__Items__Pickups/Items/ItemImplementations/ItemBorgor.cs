using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using UnityEngine;
using Assets.Scripts.Objects.Pooling;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBorgor : ItemBase
	{
		public const int maxAmountAbsolute = 20;

		private const float maxSpawnDistance = 35f;

		private float baseChance;

		private float chancePerAmount;

		private float ratioHeal;

		private int flatHealPerAmount;

		private int flatHeal;

		private float chance;

		protected override void OnInitOrAmountChanged()
		{
			chance = baseChance + (amount - 1) * chancePerAmount;
			flatHeal = 8 + amount * flatHealPerAmount;
		}

		private void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
		{
			if (global::Utility.MyRandom.random.NextDouble() > chance || enemy.enemyMovement.distanceToTarget >= maxSpawnDistance) return;
			GameObject pickup = PoolManager.Instance.borgorPool.Get();
			if (pickup == null) return;
			pickup.transform.position = enemy.transform.position;
			pickup.SetActive(true);
			pickup.GetComponent<Borgar>().Set(flatHeal, ratioHeal);
		}

		public override void Init()
		{
			Enemy.A_EnemyDied += OnEnemyDied;
		}

		public override void Cleanup()
		{
			Enemy.A_EnemyDied -= OnEnemyDied;
		}

		public static int GetMaxAmount()
		{
			return maxAmountAbsolute;
		}

		public ItemBorgor(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			baseChance = 0.02f;
			chancePerAmount = 0.01f;
			ratioHeal = 0.08f;
			flatHealPerAmount = 2;
			flatHeal = 10;
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value1"] = string.Format("+{0}%", baseChance * 100f) });
		}
	}
}
