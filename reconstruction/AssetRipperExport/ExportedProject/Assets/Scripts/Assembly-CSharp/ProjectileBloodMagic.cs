using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

public class ProjectileBloodMagic : ProjectileBase
{
	private Enemy target;

	protected override bool TryInit(int projectileIndex)
	{
		target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null || MyPlayer.Instance == null || EnemyManager.Instance == null) return false;
		Vector3 offset = Random.insideUnitSphere;
		offset.y = 0f;
		transform.position = target.GetFeetPosition() + offset * (target.meshRadius * 0.4f);
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] colliders);
		for (int i = 0; i < count; i++)
		{
			if (!EnemyManager.Instance.GetEnemy(colliders[i], out Enemy enemy) || enemy.IsDead()) continue;
			Vector3 hitDirection = enemy.GetCenterPosition() - MyPlayer.Instance.transform.position;
			enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, this, enemy, hitDirection));
			weaponAttack.ProjectileHit(enemy.collider.ClosestPoint(MyPlayer.Instance.transform.position), GetMovementDirection(), true, true);
		}
		return true;
	}

	protected override void CheckSpawnCollision()
	{
	}

	protected override Vector3 GetMovementDirection()
	{
		return default(Vector3);
	}

	protected override void MyFixedUpdate()
	{
	}

	protected override void MyUpdate()
	{
		if (target != null && !target.IsDead()) transform.position = target.GetFeetPosition();
	}

	protected override void FindMovementDirection()
	{
	}

	protected override void StepMovement()
	{
	}
}
