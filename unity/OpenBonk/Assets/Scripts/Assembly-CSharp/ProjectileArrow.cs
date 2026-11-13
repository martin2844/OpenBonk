using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class ProjectileArrow : ProjectileBase
{
	public TrailRenderer trailRenderer;

	private float upOffset;

	private Vector3 pushDir;

	private float trailStartWidth;

	private static Vector3 baseDir;

	private float speedReduction;

	private float nextCheckDamageTime;

	protected override bool TryInit(int projectileIndex)
	{
		if (trailRenderer != null)
		{
			if (trailStartWidth <= 0f) trailStartWidth = trailRenderer.startWidth;
			trailRenderer.Clear();
			trailRenderer.startWidth = trailStartWidth * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		}
		projectileSpeed = ProjectileUtility.GetArrowSpeed(weaponBase);
		speedReduction = ProjectileUtility.GetArrowSpeedReduction(weaponBase);
		if (projectileIndex == 0)
		{
			baseDir = Vector3.zero;
			float range = projectileSpeed * projectileSpeed / (2f * speedReduction);
			var target = EnemyTargeting.GetEnemy(transform.position, range, 0, true, null);
			if (target == null) return false;
			baseDir = (target.GetFeetPosition() - MyPlayer.Instance.GetFeetPosition()).normalized;
		}
		if (baseDir.sqrMagnitude < 0.0000000001f) return false;
		Vector3 normal = MyPlayer.Instance.playerMovement.DashSurfaceNormal;
		Vector3 shotDirection = Quaternion.AngleAxis(GetAngle(projectileIndex, WeaponUtility.GetAttackQuantity(weaponBase)), normal) * baseDir;
		transform.rotation = Quaternion.LookRotation(shotDirection);
		transform.position = MyPlayer.Instance.transform.position;
		return true;
	}

	private float GetAngle(int projectileIndex, int maxIndex)
	{
		if (maxIndex < 2) return 0f;
		float spread = Mathf.Clamp(maxIndex * 2.5f, 0f, 357.5f);
		return Mathf.Lerp(-spread, spread, Mathf.Clamp01((float)projectileIndex / (maxIndex - 1)));
	}

	protected override Vector3 GetMovementDirection()
	{
		return transform.forward;
	}

	protected override void MyFixedUpdate()
	{
		if (projectileSpeed > 0f) projectileSpeed = Mathf.Clamp(projectileSpeed - speedReduction, 0f, 99f);
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

	private void OnDrawGizmosSelected()
	{
	}
}
