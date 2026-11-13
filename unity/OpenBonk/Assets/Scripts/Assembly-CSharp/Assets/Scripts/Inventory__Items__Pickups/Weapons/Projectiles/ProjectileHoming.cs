using Assets.Scripts.Actors.Enemies;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles
{
	public class ProjectileHoming : ProjectileBase
	{
		private Enemy enemyTarget;

		protected override bool TryInit(int projectileIndex)
		{
			enemyTarget = null;
			FindMovementDirection();
			return enemyTarget != null;
		}

		protected override void FindMovementDirection()
		{
			Enemy target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), bounces > 0 ? 1 : 0, weaponBase.weaponData.useVision, null);
			if (target != null)
			{
				enemyTarget = target;
				direction = GetMovementDirection();
			}
			else
			{
				DestroySelf();
				direction = Vector3.zero;
			}
		}

		private bool HasBounces()
		{
			return bounces < maxBounces;
		}

		protected override Vector3 GetMovementDirection()
		{
			return (enemyTarget.GetCenterPosition() - transform.position).normalized;
		}

		protected override void MyFixedUpdate()
		{
		}

		protected override void MyUpdate()
		{
		}

		private void DestroySelf()
		{
			ProjectileDone();
		}
	}
}
