using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

public class ProjectileSniper : ProjectileBase
{
	public Vector3 attackDir = Vector3.forward;

	private float bulletSpeed = 800f;

	private float maxDistance;

	private float distTravelled;

	private RaycastHit hitRaycast;

	protected new static readonly RaycastHit[] raycastBuffer = new RaycastHit[EnemyManager.maxNumEnemiesPooled + 1];

	protected override bool TryInit(int projectileIndex)
	{
		if (MyPlayer.Instance == null || weaponBase?.weaponData == null || GameManager.Instance == null) return false;
		attackDir = GetAttackDir(projectileIndex).normalized;
		if (attackDir == Vector3.zero) return false;
		transform.SetPositionAndRotation(GetShootingPosition(), Quaternion.LookRotation(attackDir));
		transform.localScale = Vector3.one * projectileRadius;
		maxDistance = 999f;
		hitRaycast = default;
		if (Physics.Raycast(transform.position, attackDir, out RaycastHit hit, maxDistance, GameManager.Instance.whatIsGroundAndObjects))
		{
			hitRaycast = hit;
			maxDistance = hit.distance;
		}
		distTravelled = 0f;
		Hitscan(weaponBase, projectileRadius);
		return true;
	}

	private Vector3 GetShootingPosition()
	{
		return MyPlayer.Instance.transform.position + Vector3.up * weaponBase.weaponData.spawnOffset.y;
	}

	private Vector3 GetAttackDir(int projectileIndex)
	{
		var player = MyPlayer.Instance;
		Vector3 direction = player.playerRenderer != null ? player.playerRenderer.transform.forward : player.transform.forward;
		if (player.playerInput != null && player.playerInput.IsAiming())
		{
			if (Camera.main == null) return direction;
			Ray ray = Camera.main.ScreenPointToRay(CrosshairUi.GetCrosshairRaycastPosition());
			if (Physics.SphereCast(ray, projectileRadius, out RaycastHit enemyHit, 999f, GameManager.Instance.whatIsEnemy))
			{
				if (EnemyManager.Instance != null && EnemyManager.Instance.GetEnemy(enemyHit.collider, out var enemy) && enemy != null)
				{
					Vector3 target = enemyHit.point;
					target.y = enemy.GetFeetPosition().y;
					return (target - player.GetFeetPosition()).normalized;
				}
				return ray.direction;
			}
			if (Physics.SphereCast(ray, projectileRadius, out RaycastHit worldHit, 999f, GameManager.Instance.whatIsGroundAndObjects))
				return (worldHit.point - GetShootingPosition()).normalized;
			return ray.direction;
		}
		var targetEnemy = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		return targetEnemy != null ? (targetEnemy.GetFeetPosition() - player.GetFeetPosition()).normalized : direction;
	}

	protected override Vector3 GetMovementDirection()
	{
		return attackDir;
	}

	protected override void MyFixedUpdate()
	{
	}

	protected override void MyUpdate()
	{
		if (distTravelled >= maxDistance) return;
		float step = bulletSpeed * MyTime.deltaTime;
		transform.position += attackDir * step;
		distTravelled += step;
		if (distTravelled >= maxDistance && hitRaycast.collider != null && PoolManager.Instance?.bulletHitPool != null)
		{
			GameObject impact = PoolManager.Instance.bulletHitPool.Get();
			if (impact != null) impact.transform.SetPositionAndRotation(hitRaycast.point + hitRaycast.normal * .01f, Quaternion.LookRotation(hitRaycast.normal));
		}
	}

	protected override void FindMovementDirection()
	{
	}

	public void Hitscan(WeaponBase weaponBase, float radius)
	{
		if (GameManager.Instance == null || EnemyManager.Instance == null || MyPlayer.Instance == null || weaponBase == null) return;
		int count = Physics.SphereCastNonAlloc(transform.position, radius, attackDir, raycastBuffer, maxDistance, GameManager.Instance.whatIsEnemy);
		for (int i = 0; i < count; i++)
		{
			Collider collider = raycastBuffer[i].collider;
			if (collider == null || !EnemyManager.Instance.GetEnemy(collider, out var enemy) || enemy == null) continue;
			Vector3 directionToEnemy = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, this, enemy, directionToEnemy));
			if (weaponAttack != null) weaponAttack.ProjectileHit(collider.ClosestPoint(transform.position), GetMovementDirection(), true, true);
		}
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
