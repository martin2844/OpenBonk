using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons
{
	public class Firefield : MonoBehaviour
	{
		private float collisionCooldown = 0.4f;

		private float spawnTime;

		private float aliveTime;

		private float spawnRadius;

		private Vector3 normal;

		private WeaponBase weaponBase;

		private float damage;

		private string damageSource;

		private DamageContainer recycledDx = new DamageContainer(0f, DamageContainer.unknownDamageSource);

		private float nextCheckDamageTime;

		private static Dictionary<Collider, int> numTimesEnemiesHitThisTick = new Dictionary<Collider, int>();

		private static bool hasDamage;

		private static float damageThisTick;

		private float visualRadius;

		private float desiredVisualRadius;

		public void Set(Vector3 pos, Vector3 fallbackPos, float radius, float duration, float damage, WeaponBase weaponBase, string damageSource)
		{
			this.damageSource = damageSource;
			this.weaponBase = weaponBase;
			spawnRadius = radius;
			this.damage = Mathf.Clamp(damage, 1f, float.MaxValue);
			transform.position = pos;
			if (Physics.Raycast(new Ray(pos + Vector3.up, Vector3.down), out RaycastHit hit, 9999f, GameManager.Instance.whatIsGroundAndObjects))
			{
				normal = hit.normal.normalized;
				transform.position = hit.point + hit.normal * 0.05f;
				transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized, hit.normal);
			}
			else transform.position = fallbackPos + Vector3.up * 0.05f;
			transform.localScale = Vector3.zero;
			spawnTime = MyTime.time;
			aliveTime = duration;
		}

		protected void FixedUpdate()
		{
			if (!IsWeaponAttack() && MyTime.time > spawnTime + aliveTime)
			{
				gameObject.SetActive(false);
				PoolManager.Instance.firefieldPool.Release(gameObject);
				return;
			}
			CheckDamage();
		}

		private bool IsWeaponAttack()
		{
			return weaponBase != null;
		}

		private void LateUpdate()
		{
			TryPopDamage();
			if (damageThisTick > 0f) damageThisTick = 0f;
		}

		private void CheckDamage()
		{
			if (nextCheckDamageTime > MyTime.time) return;
			nextCheckDamageTime = MyTime.time + collisionCooldown;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, GetHitboxRadius(), out Collider[] enemies);
			for (int i = 0; i < count; i++)
			{
				Collider collider = enemies[i];
				numTimesEnemiesHitThisTick.TryGetValue(collider, out int hits);
				numTimesEnemiesHitThisTick[collider] = hits + 1;
				hasDamage = true;
			}
		}

		private void TryPopDamage()
		{
			if (!hasDamage) return;
			hasDamage = false;
			bool firstHit = true;
			foreach (var entry in numTimesEnemiesHitThisTick)
			{
				if (!EnemyManager.Instance.GetEnemy(entry.Key, out var enemy)) continue;
				Vector3 direction = enemy.transform.position - Assets.Scripts.Actors.Player.MyPlayer.Instance.transform.position;
				direction.y = 0f;
				DamageContainer dc;
				if (IsWeaponAttack())
				{
					dc = WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction);
				}
				else
				{
					recycledDx.Reuse(0f, damageSource);
					recycledDx.damage = damage;
					recycledDx.element = EElement.Fire;
					recycledDx.enemy = enemy;
					dc = recycledDx;
				}
				dc.damage *= entry.Value;
				enemy.DamageFromPlayerWeapon(dc);
				damageThisTick += dc.damage;
				Vector3 effectPosition = enemy.GetCenterPosition() + Random.insideUnitSphere * 0.5f;
				if (EffectManager.Instance != null) EffectManager.Instance.EnemyHitEffect(effectPosition, direction, enemy != null, EWeapon.Flamewalker, null, firstHit);
				firstHit = false;
			}
			numTimesEnemiesHitThisTick.Clear();
		}

		private float GetEffectiveRadius()
		{
			return (1f - (MyTime.time - spawnTime) / aliveTime) * spawnRadius;
		}

		private float GetHitboxRadius()
		{
			return GetEffectiveRadius() + 0.5f;
		}

		protected void Update()
		{
			transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * GetEffectiveRadius(), Time.deltaTime * 18f);
		}

		private void OnDrawGizmosSelected()
		{
		}
	}
}
