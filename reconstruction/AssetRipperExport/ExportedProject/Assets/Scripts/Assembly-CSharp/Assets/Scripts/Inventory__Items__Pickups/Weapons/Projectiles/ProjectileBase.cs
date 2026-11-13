using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Assets.Scripts.Managers;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles
{
	public abstract class ProjectileBase : MonoBehaviour
	{
		public WeaponBase weaponBase;

		public float damageMultiplier = 1f;

		protected WeaponAttack weaponAttack;

		public float projectileRadius = 0.5f;

		private float baseProjectileRadius;

		public Vector3 direction;

		public int bounces;

		public int maxBounces;

		protected bool timedOut;

		protected float expirationTime;

		protected float projectileSpeed;

		protected static readonly RaycastHit[] raycastBuffer = new RaycastHit[EnemyManager.maxNumEnemiesPooled + 1];

		protected Collider lastHitEnemy;

		protected HashSet<Collider> hitEnemies = new HashSet<Collider>();

		public void Set(WeaponBase weaponBase, WeaponAttack weaponAttack, int projectileIndex)
		{
			if (baseProjectileRadius <= 0f) baseProjectileRadius = projectileRadius;
			gameObject.SetActive(true);
			bounces = 0;
			maxBounces = (int)weaponBase.GetValue(EStat.ProjectileBounces) + (int)PlayerStats.GetStat(EStat.ProjectileBounces);
			timedOut = false;
			lastHitEnemy = null;
			hitEnemies.Clear();
			this.damageMultiplier = weaponAttack.damageMultiplier;
			this.weaponBase = weaponBase;
			this.weaponAttack = weaponAttack;
			expirationTime = MyTime.time + WeaponUtility.GetDuration(weaponBase);
			projectileSpeed = weaponBase.GetValue(EStat.ProjectileSpeedMultiplier) * PlayerStats.GetStat(EStat.ProjectileSpeedMultiplier);
			projectileRadius = baseProjectileRadius * weaponAttack.GetSize();
			if (TryInit(projectileIndex))
			{
				weaponAttack.SuccessfullySpawnedProjectile(this);
				CheckSpawnCollision();
			}
			else ProjectileDone();
		}

		protected float GetDuration()
		{
			return WeaponUtility.GetDuration(weaponBase);
		}

		protected abstract bool TryInit(int projectileIndex);

		private void FixedUpdate()
		{
			if (MyTime.paused) return;
			MyFixedUpdate();
			StepMovement();
			CheckTimeout();
		}

		protected virtual void CheckSpawnCollision()
		{
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] enemies);
			if (count > 0) CheckCollision(enemies[0], Vector3.zero);
		}

		protected abstract Vector3 GetMovementDirection();

		protected virtual void StepMovement()
		{
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] enemies);
			bool hit = false;
			for (int i = 0; i < count; i++)
			{
				if (CheckCollision(enemies[i], Vector3.zero)) { hit = true; break; }
			}
			Vector3 movement = GetMovementDirection().normalized;
			if (!hit && Physics.SphereCastNonAlloc(transform.position, projectileRadius, movement, raycastBuffer, projectileSpeed, GameManager.Instance.whatIsProjectileRaycast, QueryTriggerInteraction.Ignore) > 0)
				CheckCollision(raycastBuffer[0].collider, raycastBuffer[0].normal);
			transform.position += movement * projectileSpeed;
		}

		private void Update()
		{
			if (!MyTime.paused) MyUpdate();
		}

		private void CheckTimeout()
		{
			if (gameObject.activeInHierarchy && !timedOut && MyTime.time >= expirationTime)
			{
				timedOut = true;
				ProjectileDone();
			}
		}

		protected void ProjectileDone()
		{
			if (!gameObject.activeInHierarchy) return;
			gameObject.SetActive(false);
			weaponAttack.ProjectileDone(this);
		}

		protected abstract void MyFixedUpdate();

		protected abstract void MyUpdate();

		protected virtual bool CheckCollision(Collider collider, Vector3 normal)
		{
			int layer = collider.gameObject.layer;
			if (layer == LayerMask.NameToLayer("Ground") || layer == LayerMask.NameToLayer("Object"))
			{
				HitOther(collider, normal);
				return true;
			}
			if (layer == LayerMask.NameToLayer("Enemy")) return HitEnemy(collider, normal);
			return false;
		}

		protected virtual bool HitEnemy(Collider collider, Vector3 normal)
		{
			if (collider == lastHitEnemy || hitEnemies.Contains(collider)) return false;
			if (!EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDead()) return false;
			lastHitEnemy = collider;
			enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, this, enemy, GetMovementDirection().normalized));
			if (enemy.IsDead()) lastHitEnemy = null;
			weaponAttack.ProjectileHit(collider.ClosestPoint(transform.position), GetMovementDirection(), true, true);
			if (bounces++ >= maxBounces) ProjectileDone();
			if (weaponBase.weaponData.amplificationMode == EAmplificationMode.Pierce) hitEnemies.Add(collider);
			else if (weaponBase.weaponData.amplificationMode == EAmplificationMode.Bounce && weaponBase.weaponData.canBounce)
			{
				FindMovementDirection();
				if (direction == Vector3.zero) ProjectileDone();
			}
			return true;
		}

		protected abstract void FindMovementDirection();

		protected virtual void HitOther(Collider collider, Vector3 normal)
		{
			weaponAttack.ProjectileHit(collider.ClosestPointOnBounds(transform.position), GetMovementDirection(), false, true);
			if (!weaponBase.weaponData.canBounce || bounces++ >= maxBounces)
			{
				ProjectileDone();
				return;
			}
			Vector3 movement = GetMovementDirection();
			transform.position -= movement * projectileSpeed;
			if (normal == Vector3.zero && Physics.Raycast(transform.position, movement.normalized, out RaycastHit hit, 999f, GameManager.Instance.whatIsProjectileRaycast)) normal = hit.normal;
			if (normal != Vector3.zero) direction = Vector3.Reflect(movement, normal);
			else transform.position += movement * projectileSpeed;
		}
	}
}
