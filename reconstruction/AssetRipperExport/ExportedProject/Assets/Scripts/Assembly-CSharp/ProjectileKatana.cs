using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

public class ProjectileKatana : ProjectileBase
{
	public float testMultiplier = 1f;

	protected override bool TryInit(int projectileIndex)
	{
		float range = 8f * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		var target = EnemyTargeting.GetEnemy(transform.position, range, projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null || target.collider == null || MyPlayer.Instance == null) return false;
		Vector3 point = MyPlayer.Instance.transform.position + Random.insideUnitSphere * (target.meshRadius * 0.5f);
		transform.position = target.collider.ClosestPoint(point);
		CheckZone(weaponBase, projectileRadius, weaponAttack.prefabHit);
		return true;
	}

	public void CheckZone(WeaponBase weaponBase, float radius, GameObject hitEffect = null)
	{
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, projectileRadius, out Collider[] colliders);
		for (int i = 0; i < count; i++)
		{
			if (!EnemyManager.Instance.GetEnemy(colliders[i], out var enemy) || enemy.IsDead()) continue;
			Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction));
		}
	}

	protected override Vector3 GetMovementDirection()
	{
		return transform.forward;
	}

	protected override void MyFixedUpdate()
	{
	}

	protected override void MyUpdate()
	{
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
}
