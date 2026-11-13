using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

public class ProjectileMines : ProjectileBase
{
	public Rigidbody rb;

	private float checkInterval = 0.5f;

	private float nextCheckTime;

	private float checkRadius = 2f;

	private float scaleInTime = 0.5f;

	private float spawnedAtTime;

	private float scaleMultiplier;

	protected override bool TryInit(int projectileIndex)
	{
		if (rb == null || MyPlayer.Instance == null) return false;
		spawnedAtTime = MyTime.time;
		transform.localScale = Vector3.zero;
		scaleMultiplier = Mathf.Clamp(1f + (WeaponUtility.GetAttackSizeMultiplier(weaponBase) - 1f) * 0.3f, 1f, 3f);
		checkRadius = 2f * scaleMultiplier;
		rb.MovePosition(MyPlayer.Instance.transform.position);
		return true;
	}

	private void Update()
	{
		if (transform.localScale.x >= scaleMultiplier) return;
		float progress = Mathf.Clamp01((MyTime.time - spawnedAtTime) / scaleInTime);
		transform.localScale = Vector3.one * (scaleMultiplier * UnityEngine.UIElements.Experimental.Easing.OutQuad(progress));
	}

	protected override void MyFixedUpdate()
	{
		if (MyTime.time < spawnedAtTime + scaleInTime || MyTime.time < nextCheckTime) return;
		nextCheckTime = MyTime.time + checkInterval;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, checkRadius, out Collider[] colliders);
		for (int i = 0; i < count; i++)
		{
			if (EnemyManager.Instance.GetEnemy(colliders[i], out var enemy) && !enemy.IsDead()) { Explode(); return; }
		}
	}

	private void Explode()
	{
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] colliders);
		for (int i = 0; i < count; i++)
		{
			Collider collider = colliders[i];
			if (!EnemyManager.Instance.GetEnemy(collider, out var enemy)) continue;
			Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction));
			weaponAttack.ProjectileHit(collider.ClosestPoint(transform.position), GetMovementDirection(), true, true);
		}
		if (PoolManager.Instance != null && PoolManager.Instance.explosionPool != null)
		{
			GameObject effect = PoolManager.Instance.explosionPool.Get();
			if (effect != null) { effect.transform.position = transform.position; effect.transform.localScale = Vector3.one * projectileRadius; }
		}
		ProjectileDone();
	}

	private void Timeout()
	{
	}

	protected override void CheckSpawnCollision()
	{
	}

	protected override Vector3 GetMovementDirection()
	{
		return default(Vector3);
	}

	protected override void MyUpdate()
	{
	}

	protected override void FindMovementDirection()
	{
	}

	protected override void StepMovement()
	{
	}
}
