using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemSpicyMeatball : ItemBase
	{
		private float baseRadius = 3f;

		private float radiusPerAmount = 1f;

		private float maxRadius = 8f;

		private float radius;

		private float damageSpreadMultiplier = .65f;

		private float procChance = .25f;

		private string damageSource;

		private DamageContainer reuseDc;

		private int maxProcsPerTick = 50;

		private int numProcsThisTick;

		protected override void OnInitOrAmountChanged()
		{
			radius = Mathf.Clamp((baseRadius + radiusPerAmount * amount) * PlayerStats.GetStat(EStat.SizeMultiplier), 1f, maxRadius);
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (numProcsThisTick >= maxProcsPerTick || !ItemUtility.TryProc(procChance, dc.procCoefficient)) return;
			numProcsThisTick++;
			Vector3 center = dc.enemy.GetCenterPosition();
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, center, radius, out Collider[] targets);
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out var enemy)) continue;
				reuseDc.Reuse(0f, damageSource);
				reuseDc.damage = damageSpreadMultiplier * dc.damage;
				reuseDc.enemy = enemy;
				enemy.DamageFromPlayerOther(reuseDc);
			}
			GameObject fx = PoolManager.Instance.spicyMeatballPool.Get();
			if (fx != null)
			{
				Vector3 offset = Random.insideUnitSphere;
				offset.y = 0f;
				fx.transform.position = dc.enemy.GetCenterPosition() + offset * .4f;
				fx.SetActive(true);
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

		public ItemSpicyMeatball(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.SpicyMeatball.ToString();
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
				["value1"] = string.Format("{0}%", procChance * 100f),
				["value2"] = string.Format("{0}%", damageSpreadMultiplier * 100f)
			};
		}
	}
}
