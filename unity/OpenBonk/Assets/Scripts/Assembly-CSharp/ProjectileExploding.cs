using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using UnityEngine;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;

public class ProjectileExploding : ProjectileBasic
{
	public float explosionRadius = 5f;

	private float fxCooldown = 0.06f;

	public GameObject explosionEffect;

	private bool exploded;

	protected override bool TryInit(int projectileIndex)
	{
		exploded = false;
		return base.TryInit(projectileIndex);
	}

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		if (exploded || collider == null || GameManager.Instance == null || (GameManager.Instance.whatIsProjectileRaycast.value & (1 << collider.gameObject.layer)) == 0) return false;
		exploded = true;
		Hitscan(collider);
		ProjectileDone();
		EWeapon weapon = weaponBase.weaponData.eWeapon;
		if (!FxUtility.weaponCooldowns.ContainsKey(weapon)) FxUtility.weaponCooldowns.Add(weapon, 0f);
		if (MyTime.time > FxUtility.weaponCooldowns[weapon])
		{
			FxUtility.weaponCooldowns[weapon] = MyTime.time + fxCooldown;
			if (PoolManager.Instance != null)
			{
				GameObject effect = PoolManager.Instance.GetProjectileDoneFx(weapon, explosionEffect);
				if (effect != null) effect.transform.position = transform.position;
			}
		}
		return true;
	}

	private void Hitscan(Collider collider)
	{
		Enemy direct = null;
		if (collider.gameObject.layer == LayerMask.NameToLayer("Enemy")) EnemyManager.Instance.GetEnemy(collider, out direct);
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, explosionRadius * WeaponUtility.GetAttackSizeMultiplier(weaponBase), out Collider[] targets);
		bool first = true;
		for (int i = 0; i < count; i++)
		{
			Collider targetCollider = targets[i];
			if (!EnemyManager.Instance.GetEnemy(targetCollider, out Enemy enemy)) continue;
			Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			DamageContainer dc = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction);
			if (direct != null && direct != enemy) dc.damage *= 0.75f;
			enemy.DamageFromPlayerWeapon(dc);
			weaponAttack.ProjectileHit(targetCollider.ClosestPoint(transform.position), dc.direction, true, first);
			first = false;
		}
	}
}
