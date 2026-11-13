using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class ProjectileHeroSword : ProjectileBase
{
	public GameObject movingProjectile;
	private Vector3 movingProjectileDir;
	private Vector3 hitboxPos;
	private Quaternion movingProjectileRotation;
	private float movingProjectileDuration = .3f;
	private float startTime;
	public Vector3 colliderOffset = new Vector3(0f, 0f, .5f);
	public float testMultiplier = 1f;
	private float forwardOffset;
	private float upOffset;
	private Vector3 posOffset;
	private float actualProjectileSpeed;
	private new HashSet<Collider> hitEnemies = new HashSet<Collider>();
	private Vector3 movingProjectilePosition;

	protected override bool TryInit(int projectileIndex)
	{
		if (weaponBase == null || weaponBase.weaponData == null || weaponAttack == null || MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null || movingProjectile == null) return false;
		var target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		Vector3 shotDirection = MyPlayer.Instance.playerRenderer.transform.forward;
		if (target != null) shotDirection = (target.GetFeetPosition() - MyPlayer.Instance.GetFeetPosition()).normalized;
		Vector3 up = MyPlayer.Instance.playerRenderer.transform.up;
		shotDirection = Vector3.ProjectOnPlane(new Vector3(shotDirection.x, 0f, shotDirection.z), up);
		forwardOffset = weaponBase.weaponData.spawnOffset.z * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		posOffset = shotDirection * forwardOffset + Vector3.up * weaponBase.weaponData.spawnOffset.y;
		transform.position = MyPlayer.Instance.transform.position + posOffset;
		if (shotDirection != Vector3.zero) transform.rotation = Quaternion.LookRotation(shotDirection, up);
		movingProjectile.transform.SetPositionAndRotation(transform.position, transform.rotation);
		movingProjectilePosition = movingProjectile.transform.position;
		movingProjectileDir = shotDirection;
		movingProjectileRotation = movingProjectile.transform.rotation;
		hitboxPos = transform.position;
		actualProjectileSpeed = (1f + (WeaponUtility.GetAttackSizeMultiplier(weaponBase) - 1f) * .5f) * projectileSpeed;
		startTime = MyTime.time;
		hitEnemies.Clear();
		CheckZone(weaponBase, projectileRadius, weaponAttack.prefabHit);
		return true;
	}

	public void CheckZone(WeaponBase weaponBase, float radius, GameObject hitEffect = null)
	{
		Vector3 offset = transform.right * colliderOffset.x + transform.up * colliderOffset.y + transform.forward * colliderOffset.z;
		Vector3 center = transform.position + offset * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, center, radius, out Collider[] colliders);
		for (int i = 0; i < count; i++) CheckColliderCustom(colliders[i], 1f);
	}

	protected override void MyFixedUpdate()
	{
		if (MyTime.time > startTime + movingProjectileDuration || GameManager.Instance == null) return;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, hitboxPos, projectileRadius, out Collider[] colliders);
		for (int i = 0; i < count; i++) CheckColliderCustom(colliders[i], .5f);
		float step = actualProjectileSpeed * MyTime.fixedDeltaTime;
		count = Physics.SphereCastNonAlloc(hitboxPos, projectileRadius, movingProjectileDir.normalized, raycastBuffer, step, GameManager.Instance.whatIsEnemy, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < count; i++) CheckColliderCustom(raycastBuffer[i].collider, .5f);
		hitboxPos += movingProjectileDir * step;
	}

	private void CheckColliderCustom(Collider collider, float damageMultiplier)
	{
		if (collider == null || hitEnemies.Contains(collider) || EnemyManager.Instance == null || MyPlayer.Instance == null) return;
		if (!EnemyManager.Instance.GetEnemy(collider, out var enemy) || enemy == null) return;
		hitEnemies.Add(collider);
		Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
		var dc = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction);
		dc.damage *= damageMultiplier;
		enemy.DamageFromPlayerWeapon(dc);
		if (weaponAttack != null) weaponAttack.ProjectileHit(collider.ClosestPointOnBounds(transform.position), movingProjectileDir, true, true);
	}

	protected override void MyUpdate()
	{
		if (MyPlayer.Instance != null) transform.position = MyPlayer.Instance.transform.position + posOffset;
		movingProjectilePosition += movingProjectileDir * actualProjectileSpeed * MyTime.deltaTime;
		if (movingProjectile != null) movingProjectile.transform.SetPositionAndRotation(movingProjectilePosition, movingProjectileRotation);
	}
	protected override Vector3 GetMovementDirection() { return transform.forward; }
	private float GetRadius() { return projectileRadius; }
	protected override void StepMovement() { }
	protected override void CheckSpawnCollision() { }
	protected override bool CheckCollision(Collider collider, Vector3 normal) { return false; }
	protected override void FindMovementDirection() { }
}
