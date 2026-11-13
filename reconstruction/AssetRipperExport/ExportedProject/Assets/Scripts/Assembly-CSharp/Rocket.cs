using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public class Rocket : MonoBehaviour
{
	private float damage;

	private float spawnedAtTime;

	private float projectileSpeed;

	public float projectileRadius;

	private float upTime = 0.5f;

	private float expirationTime;

	private WeaponBase weaponBase;

	private bool useGenericPool;

	private string damageSource;

	public Action A_ProjectileDone;

	private Vector3 startDirection;

	private Vector3 lastDirection;

	private Enemy targetEnemy;

	private Vector3 currentDir;

	private float nextFindTime;

	protected static readonly RaycastHit[] raycastBuffer = new RaycastHit[1];

	private float procCoefficient;

	private DamageContainer reuseDc = new DamageContainer(0f, "");

	public void Set(Vector3 pos, float damage, float procCoefficient, WeaponBase weaponBase, bool useGenericPool, string damageSource)
	{
		this.damageSource = damageSource;
		gameObject.SetActive(true);
		this.useGenericPool = useGenericPool;
		this.weaponBase = weaponBase;
		this.damage = damage;
		float playerSpeed = PlayerStats.GetStat(EStat.ProjectileSpeedMultiplier);
		projectileSpeed = (weaponBase != null ? WeaponUtility.GetProjectileSpeed(weaponBase) : playerSpeed) * 0.45f;
		upTime = 0.5f / playerSpeed;
		transform.position = pos;
		spawnedAtTime = MyTime.time;
		expirationTime = MyTime.time + 3f;
		CheckSpawnCollision();
		lastDirection = Vector3.up;
		this.procCoefficient = procCoefficient;
		startDirection = (Vector3.up + UnityEngine.Random.insideUnitSphere * 0.5f).normalized;
	}

	private void FixedUpdate()
	{
		if (MyTime.paused) return;
		StepMovement();
		CheckTimeout();
	}

	private Vector3 GetMovementDirection()
	{
		if (targetEnemy == null || targetEnemy.IsDead()) FindTarget();
		if (targetEnemy != null)
		{
			float rise = 1f - Mathf.Clamp01((MyTime.time - spawnedAtTime) / upTime);
			Vector3 desired = (targetEnemy.GetCenterPosition() - transform.position).normalized;
			currentDir = Vector3.Lerp(currentDir, desired, Time.fixedDeltaTime * 10f * projectileSpeed);
			lastDirection = Vector3.Lerp(currentDir, startDirection, rise).normalized;
		}
		return lastDirection;
	}

	public void SetTarget(Enemy targetEnemy)
	{
		this.targetEnemy = targetEnemy;
	}

	private void FindTarget()
	{
		if (MyTime.time < nextFindTime) return;
		nextFindTime = MyTime.time + 0.5f;
		if (GameManager.Instance != null && EnemyManager.Instance != null)
			targetEnemy = EnemyTargeting.GetRandomEnemyInRadius(transform.position, 30f, true, null);
	}

	protected virtual void StepMovement()
	{
		if (GameManager.Instance == null) return;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] nearby);
		bool collided = false;
		for (int i = 0; i < count; i++)
		{
			// The original repeatedly tests the first overlap in this loop.
			collided = CheckCollision(nearby[0], Vector3.zero);
			if (!gameObject.activeSelf) return;
		}
		Vector3 direction = GetMovementDirection().normalized;
		if (!collided && direction != Vector3.zero && Physics.SphereCastNonAlloc(transform.position, projectileRadius,
			direction, raycastBuffer, projectileSpeed, GameManager.Instance.whatIsProjectileRaycast,
			QueryTriggerInteraction.Ignore) > 0)
		{
			CheckCollision(raycastBuffer[0].collider, raycastBuffer[0].normal);
			if (!gameObject.activeSelf) return;
		}
		if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
		transform.position += direction * projectileSpeed;
	}

	protected virtual void CheckSpawnCollision()
	{
		if (GameManager.Instance == null) return;
		if (EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] nearby) > 0)
			CheckCollision(nearby[0], Vector3.zero);
	}

	protected virtual bool CheckCollision(Collider collider, Vector3 normal)
	{
		if (collider == null) return false;
		int layer = collider.gameObject.layer;
		if (layer == LayerMask.NameToLayer("Ground") || layer == LayerMask.NameToLayer("Object"))
		{
			ProjectileDone();
			return true;
		}
		return layer == LayerMask.NameToLayer("Enemy") && HitEnemy(collider, normal);
	}

	private bool HitEnemy(Collider collider, Vector3 normal)
	{
		if (!gameObject.activeSelf || EnemyManager.Instance == null ||
			!EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy == null || enemy.IsDead()) return false;
		DamageContainer dc;
		if (weaponBase != null)
		{
			if (MyPlayer.Instance == null) return false;
			Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			dc = WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction);
		}
		else
		{
			reuseDc.Reuse(procCoefficient, damageSource);
			reuseDc.damage = damage;
			reuseDc.enemy = enemy;
			dc = reuseDc;
		}
		enemy.DamageFromPlayerWeapon(dc);
		ProjectileDone();
		return true;
	}

	private void HitOther(Collider collider, Vector3 normal)
	{
		ProjectileDone();
	}

	private void CheckTimeout()
	{
		if (gameObject.activeInHierarchy && MyTime.time >= expirationTime) ProjectileDone();
	}

	protected void ProjectileDone()
	{
		if (!gameObject.activeSelf) return;
		var pool = PoolManager.Instance;
		if (pool != null && pool.explosionPool != null)
		{
			GameObject explosion = pool.explosionPool.Get();
			if (explosion != null)
			{
				explosion.transform.position = transform.position + transform.forward;
				explosion.SetActive(true);
			}
		}
		if (useGenericPool)
		{
			gameObject.SetActive(false);
			if (pool != null && pool.rocketPool != null) pool.rocketPool.Release(gameObject);
		}
		A_ProjectileDone?.Invoke();
	}
}
