using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Pool;

public class ItemProjectile : MonoBehaviour
{
	public bool ignoreGroundCollision;

	public float projectileSpeed = 0.5f;

	public float projectileRadius = 0.5f;

	private float damage;

	protected float spawnedAtTime;

	private float finalProjectileSpeed;

	private float upTime = 0.4f;

	protected float expirationTime;

	private string damageSource;

	protected int projectileIndex;

	protected int projectilesCount;

	private float range;

	private Vector3 startDirection;

	private Vector3 lastDirection;

	private Enemy targetEnemy;

	private Vector3 currentDir;

	private float nextFindTime;

	protected static readonly RaycastHit[] raycastBuffer = new RaycastHit[1];

	private float procCoefficient;

	private DamageContainer reuseDc = new DamageContainer(1f, "");

	public ObjectPool<GameObject> projectilePool;

	public GameObject projectileHitEffect;

	public void Set(Vector3 pos, float damage, float procCoefficient, string damageSource, ObjectPool<GameObject> projectilePool, int projectileIndex, int totalProjectiles, float duration, float range)
	{
		this.damageSource = damageSource;
		gameObject.SetActive(true);
		this.damage = damage;
		this.projectilePool = projectilePool;
		this.projectileIndex = projectileIndex;
		projectilesCount = totalProjectiles;
		this.range = range;
		float speed = PlayerStats.GetStat(EStat.ProjectileSpeedMultiplier);
		finalProjectileSpeed = projectileSpeed * speed;
		upTime = 0.5f / speed;
		transform.position = pos;
		spawnedAtTime = MyTime.time;
		expirationTime = spawnedAtTime + duration * PlayerStats.GetStat(EStat.DurationMultiplier);
		CheckSpawnCollision();
		this.procCoefficient = procCoefficient;
		startDirection = (Vector3.up + Random.insideUnitSphere * 0.85f).normalized;
		lastDirection = startDirection;
		Init();
	}

	public void AddDamage(float damage)
	{
		this.damage += damage;
	}

	protected virtual void Init()
	{
	}

	private void FixedUpdate()
	{
		if (MyTime.paused) return;
		Step();
		CheckTimeout();
	}

	protected virtual void Step()
	{
		StepAttackMovement();
	}

	protected virtual Vector3 GetMovementDirection()
	{
		if (targetEnemy == null || targetEnemy.IsDead()) FindTarget();
		if (targetEnemy != null)
		{
			float rise = 1f - Mathf.Clamp01((MyTime.time - spawnedAtTime) / upTime);
			Vector3 desired = (targetEnemy.GetCenterPosition() - transform.position).normalized;
			currentDir = Vector3.Lerp(currentDir, desired, Time.fixedDeltaTime * 10f * finalProjectileSpeed);
			lastDirection = Vector3.Lerp(currentDir, startDirection, rise).normalized;
		}
		return lastDirection;
	}

	private void FindTarget()
	{
		if (MyTime.time < nextFindTime) return;
		nextFindTime = MyTime.time + 0.5f;
		if (GameManager.Instance != null && EnemyManager.Instance != null)
		{
			targetEnemy = EnemyTargeting.GetRandomEnemyInRadius(transform.position, range * 0.5f, projectileIndex == 0, null);
			if (targetEnemy == null)
				targetEnemy = EnemyTargeting.GetRandomEnemyInRadius(transform.position, range * 0.5f, projectileIndex == 0, null);
		}
		if (targetEnemy == null && MyPlayer.Instance != null)
			lastDirection = (MyPlayer.Instance.transform.position + Random.insideUnitSphere * 8f - transform.position).normalized;
	}

	protected virtual void StepAttackMovement()
	{
		if (GameManager.Instance == null) return;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] nearby);
		bool collided = false;
		for (int i = 0; i < count; i++)
		{
			// The original checks the first overlap on every iteration.
			collided = CheckCollision(nearby[0], Vector3.zero);
			if (!gameObject.activeSelf) return;
		}
		Vector3 direction = GetMovementDirection().normalized;
		if (!collided && direction != Vector3.zero && Physics.SphereCastNonAlloc(transform.position, projectileRadius,
			direction, raycastBuffer, finalProjectileSpeed, GameManager.Instance.whatIsProjectileRaycast,
			QueryTriggerInteraction.Ignore) > 0)
		{
			CheckCollision(raycastBuffer[0].collider, raycastBuffer[0].normal);
			if (!gameObject.activeSelf) return;
		}
		if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
		transform.position += direction * finalProjectileSpeed;
	}

	protected virtual void CheckSpawnCollision()
	{
		if (GameManager.Instance != null && EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] nearby) > 0)
			CheckCollision(nearby[0], Vector3.zero);
	}

	protected virtual bool CheckCollision(Collider collider, Vector3 normal)
	{
		if (!gameObject.activeSelf || collider == null) return false;
		int layer = collider.gameObject.layer;
		if (!ignoreGroundCollision && (layer == LayerMask.NameToLayer("Ground") || layer == LayerMask.NameToLayer("Object")))
		{
			ProjectileDone();
			return true;
		}
		return layer == LayerMask.NameToLayer("Enemy") && HitEnemy(collider, normal);
	}

	private bool HitEnemy(Collider collider, Vector3 normal)
	{
		if (EnemyManager.Instance == null || !EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy == null || enemy.IsDead()) return false;
		reuseDc = WeaponUtility.GetDamageContainer(reuseDc, damage, procCoefficient, damageSource, currentDir, enemy);
		enemy.DamageFromPlayerOther(reuseDc);
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

	protected virtual void ProjectileDone()
	{
		if (!gameObject.activeSelf) return;
		if (EffectManager.Instance != null && PoolManager.Instance != null)
			EffectManager.Instance.EnemyHitEffect(transform.position, GetMovementDirection(), true, damageSource, null, true);
		gameObject.SetActive(false);
		if (projectilePool != null) projectilePool.Release(gameObject);
	}
}
