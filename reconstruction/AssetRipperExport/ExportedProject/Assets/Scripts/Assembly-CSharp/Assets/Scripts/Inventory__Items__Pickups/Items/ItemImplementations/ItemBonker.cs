using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBonker : ItemBase
	{
		private float baseChance = .02f;

		private float baseDamageMultiplier = 20f;

		private float chancePerStack = .015f;

		private float damageMultiplierPerStack = 10f;

		private float radiusPerStack = 1f;

		private float radius = 3.5f;

		private float maxRadius = 10f;

		private float chance;

		private float damageMultiplier;

		private string damageSource;

		private DamageContainer reuseDc;

		private int maxProcsPerTick = 5;

		private int numProcsThisTick;

		protected override void OnInitOrAmountChanged()
		{
			chance = baseChance;
			damageMultiplier = baseDamageMultiplier;
			if (amount > 0)
			{
				chance += (amount - 1) * chancePerStack;
				damageMultiplier += (amount - 1) * damageMultiplierPerStack;
			}
			radius = Mathf.Min(6f + amount * radiusPerStack, maxRadius);
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (global::Utility.MyRandom.random.NextDouble() >= dc.procCoefficient * chance) return;
			numProcsThisTick++;
			GameObject fx = PoolManager.Instance.bonkPool.Get();
			if (fx != null) fx.transform.position = dc.enemy.transform.position + Vector3.up * (dc.enemy.GetHeight() * .5f);
			reuseDc.damage = damageMultiplier * dc.damage;
			reuseDc.enemy = dc.enemy;
			reuseDc.damageEffect = EDamageEffect.Bonk;
			reuseDc.direction = dc.direction;
			dc.enemy.DamageFromPlayerOther(reuseDc);
			if (numProcsThisTick >= maxProcsPerTick) return;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, dc.enemy.GetCenterPosition(), radius, out Collider[] targets);
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out var enemy)) continue;
				reuseDc.Reuse(0f, damageSource);
				reuseDc.damage = dc.damage;
				reuseDc.enemy = enemy;
				reuseDc.knockback = dc.knockback * 1.25f;
				reuseDc.direction = enemy.GetCenterPosition() - MyPlayer.Instance.transform.position;
				enemy.DamageFromPlayerOther(reuseDc);
			}
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public override void Tick()
		{
			numProcsThisTick = 0;
		}

		public ItemBonker(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.Bonker.ToString();
			reuseDc = new DamageContainer(0f, damageSource);
		}

		public override void Init()
		{
		}

		public override void Cleanup()
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
			return new Dictionary<string, object> {
				["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "",
				["value1"] = string.Format("{0}%", baseChance * 100f),
				["value2"] = string.Format("{0}x", baseDamageMultiplier)
			};
		}
	}
}
