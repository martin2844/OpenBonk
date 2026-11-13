using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;

public class ProjectileRocket : ProjectileBase
{
	public Rocket rocket;

	private string damageSource = EWeapon.Rockets.ToString();

	private void Awake()
	{
		if (rocket != null) rocket.A_ProjectileDone += OnRocketDone;
	}

	protected override bool TryInit(int projectileIndex)
	{
		if (rocket == null || weaponBase == null || weaponBase.weaponData == null) return false;
		Enemy target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null) return false;
		rocket.Set(transform.position, 0f, 0f, weaponBase, false, damageSource);
		rocket.SetTarget(target);
		return true;
	}

	protected override Vector3 GetMovementDirection()
	{
		return default(Vector3);
	}

	private void OnDestroy()
	{
		if (rocket != null) rocket.A_ProjectileDone -= OnRocketDone;
	}

	private void OnRocketDone()
	{
		ProjectileDone();
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

	protected override void StepMovement()
	{
	}
}
