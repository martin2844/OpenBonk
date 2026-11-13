using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using UnityEngine;

public class ProjectileLightningBolt : ProjectileBase
{
	protected override bool TryInit(int projectileIndex)
	{
		var target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null || MyPlayer.Instance == null) return false;
		var damage = WeaponUtility.GetDamageContainer(weaponBase, this, target, target.GetCenterPosition() - MyPlayer.Instance.transform.position);
		damage.element = EElement.Lightning;
		WeaponUtility.LightningStrike(target, maxBounces, damage, 7f * WeaponUtility.GetAttackSizeMultiplier(weaponBase), 0.5f);
		return true;
	}

	protected override Vector3 GetMovementDirection()
	{
		return Vector3.zero;
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
