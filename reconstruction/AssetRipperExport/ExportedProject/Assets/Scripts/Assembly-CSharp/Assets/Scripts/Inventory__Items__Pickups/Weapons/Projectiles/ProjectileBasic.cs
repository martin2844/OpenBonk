using UnityEngine;
using Assets.Scripts.Actors.Enemies;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles
{
	public class ProjectileBasic : ProjectileBase
	{
		protected GameObject currentTarget;

		protected override bool TryInit(int projectileIndex)
		{
			currentTarget = null;
			Enemy target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
			if (target == null) return false;
			direction = (target.GetCenterPosition() - transform.position).normalized;
			if (direction.sqrMagnitude >= 1e-10f) transform.rotation = Quaternion.LookRotation(direction);
			transform.position += transform.forward * weaponBase.weaponData.spawnOffset.z;
			return true;
		}

		protected override void FindMovementDirection()
		{
			Enemy target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), 1, weaponBase.weaponData.useVision, null);
			direction = target != null ? (target.GetCenterPosition() - transform.position).normalized : Vector3.zero;
		}

		protected override Vector3 GetMovementDirection()
		{
			return direction.normalized;
		}

		protected override void MyFixedUpdate()
		{
		}

		protected override void MyUpdate()
		{
		}

		private void OnCollisionEnter(Collision collision)
		{
			CheckCollision(collision.collider, collision.contactCount > 0 ? collision.contacts[0].normal : Vector3.zero);
		}

		private void OnTriggerEnter(Collider collider)
		{
			CheckCollision(collider, Vector3.zero);
		}
	}
}
