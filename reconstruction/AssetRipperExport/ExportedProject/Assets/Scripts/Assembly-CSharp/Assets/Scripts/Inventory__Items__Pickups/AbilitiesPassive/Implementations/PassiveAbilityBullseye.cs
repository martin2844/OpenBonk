using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine.Localization;
using UnityEngine;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Extra;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityBullseye : PassiveAbility
	{
		private float critDamagePerLevel = .01f;

		public const int MAX_MARKERS = 20;

		private float markDuration = 30f;

		private float markCooldown = 5f;

		private float markReadyAtTime;

		private float explosionRadius = 10f;

		private float maxExplosionRadius = 40f;

		private float explosionDamage = 1f;

		private static float minCooldown = .75f;

		private static float maxCooldown = 5f;

		private float cooldownReductionPerLevel = .04f;

		private static Dictionary<Enemy, float> markedEnemies;

		public static string damageSource = "Bullseye";

		private DamageContainer reuseDc = new DamageContainer(0f, EPassive.Bullseye.ToString());

		private bool isExplosionDamage;

		public override void Init()
		{
			Reset();
			GameManager.A_StageStarted += OnStageStarted;
			Enemy.A_EnemySpawned += OnEnemySpawned;
			Enemy.A_EnemyReleasedFromPool += OnEnemyReleasedFromPool;
			Enemy.A_Damage += OnEnemyDamaged;
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Cleanup()
		{
			GameManager.A_StageStarted -= OnStageStarted;
			Enemy.A_EnemySpawned -= OnEnemySpawned;
			Enemy.A_EnemyReleasedFromPool -= OnEnemyReleasedFromPool;
			Enemy.A_Damage -= OnEnemyDamaged;
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		private void OnLevelup(int level)
		{
			markCooldown = Mathf.Clamp(maxCooldown - level * cooldownReductionPerLevel, minCooldown, maxCooldown);
			SetStat(new StatModifier { stat = EStat.CritDamage, modifyType = EStatModifyType.Flat, modification = level * critDamagePerLevel });
		}

		private void OnStageStarted()
		{
			Reset();
		}

		private void OnEnemySpawned(Enemy enemy)
		{
			if (markedEnemies == null || MyTime.time < markReadyAtTime) return;
			GameObject marker = PoolManager.Instance.bullseyePool.Get();
			if (marker == null) return;
			markedEnemies.Add(enemy, MyTime.time + markDuration);
			markReadyAtTime = MyTime.time + markCooldown;
			marker.GetComponent<BullseyeMarker>().Set(enemy, markDuration);
		}

		private void OnEnemyDamaged(Enemy enemy, DamageContainer dc)
		{
			if (enemy == null || dc == null || dc.enemy == null || isExplosionDamage || !IsMarkedEnemy(enemy)) return;
			markedEnemies.Remove(enemy);
			float radius = Mathf.Min(explosionRadius * (1f + (PlayerStats.GetStat(EStat.SizeMultiplier) - 1f) * .5f), maxExplosionRadius);
			isExplosionDamage = true;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, dc.enemy.GetCenterPosition(), radius, out Collider[] targets);
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out var target)) continue;
				reuseDc.Reuse(0f, damageSource);
				reuseDc.damage = explosionDamage * dc.damage;
				reuseDc.enemy = target;
				target.DamageFromPlayerOther(reuseDc);
			}
			isExplosionDamage = false;
			GameObject fx = PoolManager.Instance.explosionPool.Get();
			if (fx != null)
			{
				fx.SetActive(true);
				Vector3 offset = Random.insideUnitSphere; offset.y = 0f;
				fx.transform.position = dc.enemy.GetCenterPosition() + offset * .3f;
				fx.transform.localScale = Vector3.one * radius;
			}
			AudioManager.Instance.Bullseye();
		}

		private void OnEnemyReleasedFromPool(Enemy enemy)
		{
			markedEnemies?.Remove(enemy);
		}

		private void Reset()
		{
			markedEnemies = new Dictionary<Enemy, float>();
			markReadyAtTime = MyTime.time + markCooldown;
		}

		public static bool IsMarkedEnemy(Enemy enemy)
		{
			return markedEnemies != null && markedEnemies.TryGetValue(enemy, out float expiry) && expiry > MyTime.time;
		}

		public override void Tick()
		{
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Bullseye;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.CritDamage) ?? "",
				["value1"] = string.Format("{0}%", critDamagePerLevel * 100f)
			});
		}
	}
}
