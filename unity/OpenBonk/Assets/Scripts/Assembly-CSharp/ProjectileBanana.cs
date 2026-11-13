using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class ProjectileBanana : ProjectileBase
{
	public Transform renderer;

	public TrailRenderer trailRenderer;

	private float trailStartWidth;

	private Vector3 startDirection;

	private Vector3 movementVelocity;

	private Dictionary<Collider, float> enemyHitCooldowns = new Dictionary<Collider, float>();

	private float hitCooldown = 0.24f;

	private float readyToCollectTime;

	private float sqrCollectDistance;

	private float maxSpeed;

	private float returnTime;

	private Vector3 dirToPlayer;

	public Rigidbody rb;

	private float nextCheckDamageTime;

	private static Dictionary<Collider, int> numTimesEnemiesHitThisTick = new Dictionary<Collider, int>();

	private static bool hasDamage;

	private static float damageThisTick;

	private float distToPlayer;

	private bool isCloseToPlayer;

	protected override bool TryInit(int projectileIndex)
	{
		if (trailRenderer != null)
		{
			if (trailStartWidth <= 0f) trailStartWidth = trailRenderer.startWidth;
			trailRenderer.Clear();
			trailRenderer.startWidth = trailStartWidth * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		}
		readyToCollectTime = MyTime.time + 0.5f;
		hitEnemies.Clear();
		enemyHitCooldowns.Clear();
		var enemy = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (enemy == null) return false;
		startDirection = (enemy.GetCenterPosition() - transform.position).normalized;
		returnTime = 0f;
		maxSpeed = projectileSpeed * 55f;
		movementVelocity = startDirection * maxSpeed;
		rb.MovePosition(transform.position);
		sqrCollectDistance = projectileRadius * projectileRadius * 0.25f;
		return true;
	}

	protected override Vector3 GetMovementDirection()
	{
		return movementVelocity;
	}

	protected override void MyFixedUpdate()
	{
		CheckHitPlayer();
		CheckRadiusDamage();
		dirToPlayer = (MyPlayer.Instance.transform.position - transform.position).normalized;
		returnTime = Mathf.Clamp01(returnTime + MyTime.fixedDeltaTime * 0.7f);
		movementVelocity = Vector3.Lerp(startDirection, dirToPlayer, returnTime) * maxSpeed;
	}

	protected override void StepMovement()
	{
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] enemies);
		for (int i = 0; i < count; i++) CheckCollision(enemies[i], Vector3.zero);
		Vector3 direction = movementVelocity.normalized;
		if (IsUsingSphereCast() && Physics.SphereCast(transform.position, projectileRadius, direction, out RaycastHit hit, projectileSpeed * MyTime.fixedDeltaTime, GameManager.Instance.whatIsProjectileRaycast))
			CheckCollision(hit.collider, hit.normal);
		rb.velocity = movementVelocity;
	}

	private bool IsUsingSphereCast()
	{
		return WeaponUtility.GetAttackQuantity(weaponBase) < 7 || WeaponUtility.GetAttackSizeMultiplier(weaponBase) < 2.5f;
	}

	private void CheckRadiusDamage()
	{
		if (MyTime.time < nextCheckDamageTime) return;
		nextCheckDamageTime = MyTime.time + hitCooldown;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] enemies);
		for (int i = 0; i < count; i++)
		{
			Collider collider = enemies[i];
			if (enemyHitCooldowns.TryGetValue(collider, out float lastHit) && MyTime.time - lastHit < hitCooldown) continue;
			numTimesEnemiesHitThisTick.TryGetValue(collider, out int hits);
			numTimesEnemiesHitThisTick[collider] = hits + 1;
			hasDamage = true;
		}
	}

	private void LateUpdate()
	{
		TryPopDamage();
	}

	private void TryPopDamage()
	{
		if (!hasDamage) return;
		hasDamage = false;
		int effects = 0;
		foreach (var pair in numTimesEnemiesHitThisTick)
		{
			if (!EnemyManager.Instance.GetEnemy(pair.Key, out var enemy)) continue;
			Vector3 direction = enemy.transform.position - MyPlayer.Instance.transform.position;
			direction.y = 0f;
			var dc = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, -dirToPlayer);
			dc.damage *= pair.Value;
			enemy.DamageFromPlayerWeapon(dc);
			damageThisTick += dc.damage;
			enemyHitCooldowns[pair.Key] = MyTime.time;
			if (effects <= 4)
				EffectManager.Instance.EnemyHitEffect(pair.Key.ClosestPoint(transform.position), direction, enemy, weaponBase.weaponData.eWeapon, null, effects == 0);
			effects++;
		}
		numTimesEnemiesHitThisTick.Clear();
	}

	private void CheckHitPlayer()
	{
		if (MyTime.time < readyToCollectTime) return;
		distToPlayer = (MyPlayer.Instance.transform.position - transform.position).sqrMagnitude;
		if (distToPlayer < sqrCollectDistance) ProjectileDone();
	}

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		return collider.gameObject.layer == LayerMask.NameToLayer("Enemy") && HitEnemy(collider);
	}

	private bool HitEnemy(Collider collider)
	{
		if (enemyHitCooldowns.TryGetValue(collider, out float lastHit) && MyTime.time - lastHit < hitCooldown) return false;
		if (!EnemyManager.Instance.GetEnemy(collider, out var enemy)) return false;
		var dc = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, -dirToPlayer);
		enemy.DamageFromPlayerWeapon(dc);
		damageThisTick += dc.damage;
		weaponAttack.ProjectileHit(collider.ClosestPoint(transform.position), GetMovementDirection(), true, true);
		enemyHitCooldowns[collider] = MyTime.time;
		return true;
	}

	protected override void MyUpdate()
	{
		renderer.Rotate(Vector3.up, Time.deltaTime * 1200f, Space.Self);
	}

	protected override void FindMovementDirection()
	{
	}
}
