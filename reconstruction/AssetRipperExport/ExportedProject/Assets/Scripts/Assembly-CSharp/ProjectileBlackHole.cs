using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class ProjectileBlackHole : ProjectileBase
{
	public float collisionCooldown = 0.3f;

	private float forwardOffset;

	private float upOffset;

	private Vector3 pushDir;

	private Vector3 defaultSize;

	private float startFadeTime;

	private float maxSize = 35f;

	private Vector3 desiredPosition;

	private Vector3 startPosition;

	private float moveTime = 2f;

	private float nextCheckDamageTime;

	private HashSet<Enemy> suckedEnemies = new HashSet<Enemy>();

	private float moveTimer;

	private Vector3 startScaleSize;

	protected override bool TryInit(int projectileIndex)
	{
		if (defaultSize == Vector3.zero) defaultSize = transform.localScale;
		if (transform.localScale.x > maxSize) transform.localScale = Vector3.one * maxSize;
		Enemy target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null) return false;
		Vector3 toEnemy = (target.GetFeetPosition() - MyPlayer.Instance.GetFeetPosition()).normalized;
		forwardOffset = 8f + projectileRadius * 1.1f;
		transform.position = MyPlayer.Instance.transform.position;
		desiredPosition = transform.position + toEnemy * forwardOffset;
		moveTimer = 0f;
		startPosition = transform.position;
		nextCheckDamageTime = 0f;
		projectileRadius = Mathf.Min(projectileRadius, maxSize * 0.5f);
		startFadeTime = expirationTime - 0.3f;
		startScaleSize = Vector3.zero;
		if (Physics.Raycast(transform.position, toEnemy, out RaycastHit hit, forwardOffset, GameManager.Instance.whatIsGroundAndObjects))
			desiredPosition = hit.point - transform.forward * projectileRadius;
		return true;
	}

	protected override Vector3 GetMovementDirection()
	{
		return transform.forward;
	}

	protected override void MyFixedUpdate()
	{
		CheckDamage();
	}

	private void CheckDamage()
	{
		if (nextCheckDamageTime > MyTime.time) return;
		nextCheckDamageTime = MyTime.time + collisionCooldown;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] colliders);
		bool useSfx = true;
		for (int i = 0; i < count; i++)
		{
			Collider collider = colliders[i];
			if (!EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDead()) continue;
			Vector3 damageDirection = enemy.transform.position - MyPlayer.Instance.transform.position;
			damageDirection.y = 0f;
			DamageContainer damage = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, damageDirection);
			damage.knockback = 0f;
			enemy.DamageFromPlayerWeapon(damage);
			if (!enemy.IsStageBoss()) { enemy.enemyMovement.Suck(transform); suckedEnemies.Add(enemy); }
			EffectManager.Instance.EnemyHitEffect(collider.ClosestPoint(transform.position), damageDirection, enemy, weaponBase.weaponData.eWeapon, null, useSfx);
			useSfx = false;
		}
	}

	private void OnDisable()
	{
		foreach (Enemy enemy in suckedEnemies)
			if (enemy != null) enemy.enemyMovement.StopSuck();
		suckedEnemies.Clear();
	}

	protected override void MyUpdate()
	{
		if (moveTimer >= 1f) return;
		moveTimer = Mathf.Min(1f, moveTimer + MyTime.deltaTime / moveTime);
		transform.position = Vector3.Lerp(startPosition, desiredPosition, UnityEngine.UIElements.Experimental.Easing.OutQuad(moveTimer));
		if (MyTime.time > startFadeTime)
		{
			if (startScaleSize == Vector3.zero) startScaleSize = transform.localScale;
			transform.localScale = Vector3.Lerp(startScaleSize, Vector3.zero, (MyTime.time - startFadeTime) / (expirationTime - startFadeTime));
		}
	}

	protected override void FindMovementDirection()
	{
	}

	private float GetRadius()
	{
		return projectileRadius;
	}

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		return false;
	}

	protected override void StepMovement()
	{
	}

	protected override void CheckSpawnCollision()
	{
	}

	private void OnDrawGizmosSelected()
	{
	}
}
