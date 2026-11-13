using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using UnityEngine;

public class ProjectileMelee : ProjectileBase
{
	public class MeleeHit
	{
		public Vector3 pos;

		public Vector3 dir;

		public MeleeHit(Vector3 pos, Vector3 dir)
		{
			this.pos = pos;
			this.dir = dir;
		}
	}

	public Vector3 colliderOffset = new Vector3(0f, 0f, 0.5f);

	public float testMultiplier = 1f;

	private float forwardOffset;

	private float upOffset;

	private List<MeleeHit> effectHits = new List<MeleeHit>();

	private bool useAudio = true;

	protected override bool TryInit(int projectileIndex)
	{
		forwardOffset = weaponBase.weaponData.spawnOffset.z * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		MyUpdate();
		CheckZone(weaponBase, projectileRadius, weaponAttack.prefabHit);
		useAudio = true;
		return true;
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
		Transform player = MyPlayer.Instance.transform;
		Transform renderer = MyPlayer.Instance.playerRenderer.transform;
		transform.position = player.position + renderer.forward * forwardOffset + Vector3.up * weaponBase.weaponData.spawnOffset.y;
		transform.rotation = renderer.rotation;
	}

	protected override void FindMovementDirection()
	{
	}

	public void CheckZone(WeaponBase weaponBase, float radius, GameObject hitEffect = null)
	{
		effectHits = new List<MeleeHit>();
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, radius, out Collider[] colliders);
		bool scheduledEffect = false;
		for (int i = 0; i < count; i++)
		{
			Collider collider = colliders[i];
			if (!EnemyManager.Instance.GetEnemy(collider, out var enemy)) continue;
			Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			var damage = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction);
			enemy.DamageFromPlayerWeapon(damage);
			effectHits.Add(new MeleeHit(collider.ClosestPoint(transform.position), damage.direction));
			if (hitEffect != null && !scheduledEffect)
			{
				Invoke(nameof(SpawnEffect), 0.1f);
				scheduledEffect = true;
			}
		}
	}

	private void SpawnEffect()
	{
		foreach (MeleeHit hit in effectHits)
		{
			weaponAttack.ProjectileHit(hit.pos, hit.dir, true, useAudio);
			useAudio = false;
		}
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
