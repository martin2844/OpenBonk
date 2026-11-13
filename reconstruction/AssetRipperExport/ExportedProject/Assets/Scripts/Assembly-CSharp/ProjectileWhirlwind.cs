using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class ProjectileWhirlwind : ProjectileBase
{
	public Rigidbody rb;

	private Dictionary<Collider, float> enemyHitCooldowns = new Dictionary<Collider, float>();

	private float hitCooldown = 0.5f;

	private Vector3 startDirection;

	private float maxSpeed;

	private float speed;

	private float nextCheckDamageTime;

	protected override bool TryInit(int projectileIndex)
	{
		if (MyPlayer.Instance == null || rb == null || weaponBase?.weaponData == null) return false;
		hitEnemies.Clear();
		enemyHitCooldowns.Clear();
		transform.position = MyPlayer.Instance.GetFeetPosition();
		Enemy enemy = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (enemy == null) return false;
		startDirection = (enemy.GetCenterPosition() - transform.position).normalized;
		if (startDirection.y > 0f) startDirection.y = 0f;
		maxSpeed = projectileSpeed;
		speed = projectileSpeed;
		rb.MovePosition(transform.position);
		return true;
	}

	protected override Vector3 GetMovementDirection()
	{
		return startDirection;
	}

	protected override void MyFixedUpdate()
	{
		CheckRadiusDamage();
	}

	private Vector3 GetRaycastPosition()
	{
		return transform.position + Vector3.up * projectileRadius;
	}

	protected override void StepMovement()
	{
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, GetRaycastPosition(), projectileRadius, out Collider[] enemies);
		bool hit = false;
		for (int i = 0; i < count; i++)
		{
			if (CheckCollision(enemies[i], Vector3.zero)) { hit = true; break; }
		}
		Vector3 movement = GetMovementDirection().normalized;
		if (!hit && Physics.SphereCastNonAlloc(GetRaycastPosition(), projectileRadius, movement, raycastBuffer, projectileSpeed * MyTime.fixedDeltaTime, GameManager.Instance.whatIsProjectileRaycast, QueryTriggerInteraction.Ignore) > 0)
			CheckCollision(raycastBuffer[0].collider, raycastBuffer[0].normal);
		rb.velocity = GetMovementDirection() * speed;
	}

	private void CheckRadiusDamage()
	{
		if (nextCheckDamageTime > MyTime.time) return;
		nextCheckDamageTime = MyTime.time + hitCooldown;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, rb.position, projectileRadius, out Collider[] enemies);
		for (int i = 0; i < count; i++) HitEnemy(enemies[i]);
	}

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		return collider != null && collider.gameObject.layer == LayerMask.NameToLayer("Enemy") && HitEnemy(collider);
	}

	private bool HitEnemy(Collider collider)
	{
		if (enemyHitCooldowns.TryGetValue(collider, out float lastHit) && MyTime.time - lastHit < hitCooldown) return false;
		if (!EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDead()) return false;
		Vector3 hitDirection = enemy.transform.position - MyPlayer.Instance.transform.position;
		hitDirection.y = 0f;
		enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, this, enemy, hitDirection));
		weaponAttack.ProjectileHit(collider.ClosestPoint(transform.position), GetMovementDirection(), true, true);
		enemyHitCooldowns[collider] = MyTime.time;
		return true;
	}

	protected override void MyUpdate()
	{
	}

	protected override void FindMovementDirection()
	{
	}
}
