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
	public class ItemGrandmasSecretTonic : ItemBase
	{
		private float critChanceTotal = .02f;

		private float baseRadius = 3f;

		private float radiusPerAmount = 1f;

		private float maxRadius = 8f;

		private float radius;

		private float damageSpreadMultiplier = .5f;

		private float procChance = .5f;

		private DamageContainer procDc;

		private string damageSource;

		private Dictionary<Collider, float> numTimesEnemiesHit;

		private float totalDamage;

		private readonly List<Collider> _tmpKeys;

		private int maxProcsPerTick = 100;

		private int numProcsThisTick;

		protected override void OnInitOrAmountChanged()
		{
			radius = Mathf.Clamp((baseRadius + radiusPerAmount * amount) * PlayerStats.GetStat(EStat.SizeMultiplier), 1f, maxRadius);
		}

		public ItemGrandmasSecretTonic(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.GrandmasSecretTonic.ToString();
			procDc = new DamageContainer(0f, damageSource);
			numTimesEnemiesHit = new Dictionary<Collider, float>();
			_tmpKeys = new List<Collider>();
		}

		public override void Init()
		{
			SetStat(new StatModifier { stat = EStat.CritChance, modifyType = EStatModifyType.Flat, modification = critChanceTotal });
		}

		public override void Cleanup()
		{
			// The bonus is installed once in Init, independent of stack count.
			// Queue its recalculation before the last copy leaves the inventory.
			SetStat(new StatModifier { stat = EStat.CritChance, modifyType = EStatModifyType.Flat, modification = 0f });
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
			if (numProcsThisTick >= maxProcsPerTick || dc == null || !dc.crit || dc.enemy == null || !ItemUtility.TryProc(procChance, dc.procCoefficient)) return;
			numProcsThisTick++;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, dc.enemy.GetCenterPosition(), radius, out Collider[] targets);
			for (int i = 0; i < count; i++)
			{
				Collider target = targets[i];
				if (target == null) continue;
				numTimesEnemiesHit.TryGetValue(target, out float accumulated);
				numTimesEnemiesHit[target] = accumulated + dc.damage;
			}
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public override void LateFixedUpdate()
		{
			totalDamage = 0f;
			_tmpKeys.Clear();
			_tmpKeys.AddRange(numTimesEnemiesHit.Keys);
			foreach (Collider target in _tmpKeys)
			{
				if (target == null || EnemyManager.Instance == null || !EnemyManager.Instance.GetEnemy(target, out var enemy) || enemy == null || enemy.gameObject == null) continue;
				procDc.Reuse(0f, damageSource);
				procDc.damage = numTimesEnemiesHit[target] * damageSpreadMultiplier;
				procDc.enemy = enemy;
				enemy.DamageFromPlayerOther(procDc);
				totalDamage += procDc.damage;
				GameObject fx = PoolManager.Instance != null && PoolManager.Instance.grandmaTonicPool != null ? PoolManager.Instance.grandmaTonicPool.Get() : null;
				if (fx != null) { Vector3 offset = Random.insideUnitSphere; offset.y = 0f; fx.transform.position = enemy.GetCenterPosition() + offset * .5f; fx.SetActive(true); }
			}
			numTimesEnemiesHit.Clear();
		}

		public override void Tick()
		{
			numProcsThisTick = 0;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["value1"] = string.Format("{0}%", procChance * 100f), ["value2"] = string.Format("{0}%", damageSpreadMultiplier * 100f) };
		}
	}
}
