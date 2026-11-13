using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using UnityEngine;

public class ProjectileBluetooth : ProjectileBase
{
	protected Enemy target;

	private GameObject lastTarget;

	protected override bool TryInit(int projectileIndex)
	{
		lastHitEnemy = null;
		transform.position += transform.forward * weaponBase.weaponData.spawnOffset.z;
		target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null) return false;
		lastTarget = target.gameObject;
		direction = (target.GetCenterPosition() - transform.position).normalized;
		if (direction.sqrMagnitude >= 1e-10f) transform.rotation = Quaternion.LookRotation(direction);
		return true;
	}

	protected override void FindMovementDirection()
	{
		target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), 1, weaponBase.weaponData.useVision, lastTarget);
		if (target == null) { direction = Vector3.zero; return; }
		direction = (target.GetCenterPosition() - transform.position).normalized;
		lastTarget = target.gameObject;
	}

	protected override Vector3 GetMovementDirection()
	{
		return target != null ? (target.GetCenterPosition() - transform.position).normalized : Vector3.zero;
	}

	protected override void StepMovement()
	{
		if (target == null || target.IsDead())
		{
			FindMovementDirection();
			if (target == null) { ProjectileDone(); return; }
		}
		Vector3 toTarget = target.GetCenterPosition() - transform.position;
		Vector3 step = GetMovementDirection() * projectileSpeed;
		if (toTarget.sqrMagnitude <= projectileRadius)
		{
			HitTarget();
			if (!gameObject.activeInHierarchy) return;
		}
		Vector3 movement = step.sqrMagnitude > toTarget.sqrMagnitude ? toTarget : step;
		transform.position += movement;
		transform.rotation = movement.sqrMagnitude < 1e-10f ? Quaternion.identity : Quaternion.LookRotation(movement);
	}

	private void HitTarget()
	{
		Vector3 movement = GetMovementDirection();
		target.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, this, target, movement.normalized));
		weaponAttack.ProjectileHit(target.collider.ClosestPointOnBounds(transform.position), GetMovementDirection(), true, true);
		if (bounces++ >= maxBounces) { ProjectileDone(); return; }
		if (weaponBase.weaponData.amplificationMode == EAmplificationMode.Bounce && weaponBase.weaponData.canBounce)
		{
			FindMovementDirection();
			if (direction == Vector3.zero) ProjectileDone();
		}
	}

	protected override bool HitEnemy(Collider collider, Vector3 normal)
	{
		return false;
	}

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		return false;
	}

	protected override void CheckSpawnCollision()
	{
	}

	protected override void HitOther(Collider collider, Vector3 normal)
	{
	}

	protected override void MyUpdate()
	{
	}

	protected override void MyFixedUpdate()
	{
	}
}
