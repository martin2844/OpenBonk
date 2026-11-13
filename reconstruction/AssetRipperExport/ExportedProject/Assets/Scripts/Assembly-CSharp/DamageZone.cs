using Assets.Scripts.Inventory__Items__Pickups.Weapons;
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
	}

	private void UpdateSize()
	{
	}

	private void FixedUpdate()
	{
	}

	private void OnDrawGizmosSelected()
	{
	}
}
