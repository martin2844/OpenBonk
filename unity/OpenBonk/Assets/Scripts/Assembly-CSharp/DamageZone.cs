using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
	private WeaponBase weaponBase;

	public float cooldown;

	public TrailRenderer trailRenderer;

	public float defaultRadius;

	private float radius;

	private float nextCheckDamageTime;

	public void Set(WeaponBase weaponBase)
	{
		this.weaponBase = weaponBase;
		UpdateSize();
	}

	private void UpdateSize()
	{
		float multiplier = WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		radius = defaultRadius * multiplier;
		transform.localScale = Vector3.one * multiplier;
		if (trailRenderer != null) trailRenderer.startWidth = multiplier;
	}

	private void FixedUpdate()
	{
		if (nextCheckDamageTime > MyTime.time) return;
		nextCheckDamageTime = MyTime.time + cooldown;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, radius, out Collider[] targets);
		for (int i = 0; i < count; i++)
		{
			if (!EnemyManager.Instance.GetEnemy(targets[i], out Enemy enemy)) continue;
			Vector3 direction = (enemy.transform.position - transform.position).normalized;
			enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction));
			EffectManager.Instance.EnemyHitEffect(targets[i].ClosestPoint(transform.position), direction,
				enemy, weaponBase.weaponData.eWeapon, null, i == 0);
		}
	}

	private void OnDrawGizmosSelected()
	{
	}
}
