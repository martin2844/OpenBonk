using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class ProjectileCringeSword : ProjectileBase
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
		Transform playerTransform = MyPlayer.Instance.playerRenderer.transform;
		forwardOffset = weaponBase.weaponData.spawnOffset.z * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		posOffset = playerTransform.forward * forwardOffset + Vector3.up * weaponBase.weaponData.spawnOffset.y;
		transform.SetPositionAndRotation(MyPlayer.Instance.transform.position + posOffset, playerTransform.rotation);
		Quaternion reverseRotation = Quaternion.AngleAxis(180f, playerTransform.up) * playerTransform.rotation;
		Vector3 reverseDirection = reverseRotation * Vector3.forward;
		movingProjectile.transform.SetPositionAndRotation(transform.position + reverseDirection * forwardOffset + Vector3.up * weaponBase.weaponData.spawnOffset.y, reverseRotation);
		movingProjectilePosition = movingProjectile.transform.position;
		hitboxPos = movingProjectilePosition;
		movingProjectileDir = reverseDirection.normalized;
		movingProjectileRotation = movingProjectile.transform.rotation;
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
		var health = MyPlayer.Instance.inventory != null ? MyPlayer.Instance.inventory.playerHealth : null;
		if (health == null) return;
		int hp = health.GetCombinedHp();
		float ratio = Mathf.Clamp01((float)hp / health.GetCombinedMaxHp());
		float healthMultiplier = hp == 1 ? 3f : Mathf.Lerp(.4f, 2.25f, 1f - Easing.OutQuad(ratio));
		dc.damage *= damageMultiplier * healthMultiplier;
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
