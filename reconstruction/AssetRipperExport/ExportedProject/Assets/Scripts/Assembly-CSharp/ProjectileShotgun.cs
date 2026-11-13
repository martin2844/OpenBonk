using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public class ProjectileShotgun : ProjectileBase
{
	public ParticleSystem psBullets;

	private ParticleSystem.EmissionModule psBulletsEmission;

	public float testMultiplier = 1f;

	private float forwardOffset;

	private float upOffset;

	private Vector3 attackDir;

	protected override bool TryInit(int projectileIndex)
	{
		if (MyPlayer.Instance == null || weaponBase?.weaponData == null || GameManager.Instance == null) return false;
		attackDir = GetAttackDir();
		if (attackDir == Vector3.zero) return false;
		forwardOffset = weaponBase.weaponData.spawnOffset.z;
		transform.SetPositionAndRotation(GetShootingPosition() + attackDir * forwardOffset, Quaternion.LookRotation(attackDir));
		SetBurstCount();
		CheckZone(weaponBase);
		return true;
	}

	private Vector3 GetShootingPosition()
	{
		return MyPlayer.Instance.transform.position + Vector3.up * weaponBase.weaponData.spawnOffset.y;
	}

	private Vector3 GetAttackDir()
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
					Vector3 target = enemyHit.point; target.y = enemy.GetFeetPosition().y;
					return (target - player.GetFeetPosition()).normalized;
				}
				return ray.direction;
			}
			if (Physics.SphereCast(ray, projectileRadius, out RaycastHit worldHit, 999f, GameManager.Instance.whatIsGroundAndObjects))
				return (worldHit.point - GetShootingPosition()).normalized;
			return ray.direction;
		}
		var targetEnemy = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), 0, weaponBase.weaponData.useVision, null);
		return targetEnemy != null ? (targetEnemy.GetFeetPosition() - player.GetFeetPosition()).normalized : direction;
	}

	private void SetBurstCount()
	{
		if (psBullets == null) return;
		psBulletsEmission = psBullets.emission;
		if (psBulletsEmission.burstCount > 0)
		{
			var burst = psBulletsEmission.GetBurst(0);
			burst.count = Mathf.Clamp((int)(PlayerStats.GetStat(EStat.Projectiles) + weaponBase.GetValue(EStat.Projectiles)) + 2, 1, 20);
			psBulletsEmission.SetBurst(0, burst);
		}
		psBullets.Play();
	}

	private float GetRange()
	{
		return projectileRadius;
	}

	public void CheckZone(WeaponBase weaponBase)
	{
		if (weaponBase == null || MyPlayer.Instance == null || EnemyManager.Instance == null) return;
		foreach (var collider in global::Utility.RaycastUtility.ConeCastNew(transform.position, attackDir, projectileRadius, 35f))
		{
			if (collider == null || !EnemyManager.Instance.GetEnemy(collider, out var enemy) || enemy == null) continue;
			float distance = Vector3.Distance(collider.ClosestPointOnBounds(transform.position), MyPlayer.Instance.transform.position);
			if (distance > projectileRadius) continue;
			float falloff = Mathf.InverseLerp(projectileRadius, projectileRadius * .2f, distance);
			Vector3 hitDirection = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			var damage = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, hitDirection);
			damage.damage = Mathf.Max(1f, damage.damage * falloff * (1f + .25f * GetRawQuantity()));
			enemy.DamageFromPlayerWeapon(damage);
			if (weaponAttack != null) weaponAttack.ProjectileHit(collider.ClosestPoint(transform.position), damage.direction, true, true);
		}
	}

	private int GetRawQuantity()
	{
		return (int)(PlayerStats.GetStat(EStat.Projectiles) + weaponBase.GetValue(EStat.Projectiles)) - 1;
	}

	private float GetRadius()
	{
		return projectileRadius;
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
	}

	protected override void FindMovementDirection()
	{
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
