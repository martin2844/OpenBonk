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
	public class ItemQuinsMask : ItemBase
	{
		private float thornsPerAmount = 20f;

		private float baseRadius = 5f;

		private float radiusPerAmount = 1f;

		private float maxRadius = 10f;

		private float radius;

		private float damageSpreadMultiplier = .5f;

		private float procChance = .5f;

		private HashSet<string> damageSources;

		private DamageContainer procDc;

		public static string damageSource = EItem.QuinsMask.ToString();

		private string aegisDamageSource;

		private int maxProcsPerTick = 100;

		private int numProcsThisTick;

		protected override void OnInitOrAmountChanged()
		{
			radius = Mathf.Clamp((baseRadius + radiusPerAmount * amount) * PlayerStats.GetStat(EStat.SizeMultiplier), 1f, maxRadius);
			SetStat(new StatModifier { stat = EStat.Thorns, modifyType = EStatModifyType.Flat, modification = thornsPerAmount * amount });
		}

		public ItemQuinsMask(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			procDc = new DamageContainer(0f, "");
		}

		public override void Init()
		{
			aegisDamageSource = DataManager.Instance != null ? DataManager.Instance.GetWeapon(EWeapon.Aegis)?.damageSourceName : null;
			damageSources = new HashSet<string> { PlayerHealth.thornsDamageSource, ItemCactus.damageSource, aegisDamageSource, ItemElectricPlug.damageSource };
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (numProcsThisTick >= maxProcsPerTick || dc == null || dc.enemy == null || damageSources == null || !damageSources.Contains(dc.damageSource) || !ItemUtility.TryProc(procChance, dc.procCoefficient)) return;
			numProcsThisTick++;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, dc.enemy.GetCenterPosition(), radius, out Collider[] targets);
			for (int i = 0; i < count; i++)
			{
				if (EnemyManager.Instance == null || targets[i] == null || !EnemyManager.Instance.GetEnemy(targets[i], out var enemy) || enemy == null || enemy.IsDead()) continue;
				procDc.Reuse(0f, damageSource); procDc.damage = damageSpreadMultiplier * dc.damage; procDc.enemy = enemy;
				enemy.DamageFromPlayerOther(procDc);
			}
			GameObject fx = PoolManager.Instance != null && PoolManager.Instance.quinMaskPool != null ? PoolManager.Instance.quinMaskPool.Get() : null;
			if (fx != null) { Vector3 offset = Random.insideUnitSphere; offset.y = 0f; fx.transform.position = dc.enemy.GetCenterPosition() + offset * .5f; fx.SetActive(true); }
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public override void Tick()
		{
			numProcsThisTick = 0;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["value1"] = string.Format("{0}%", procChance * 100f) };
		}
	}
}
