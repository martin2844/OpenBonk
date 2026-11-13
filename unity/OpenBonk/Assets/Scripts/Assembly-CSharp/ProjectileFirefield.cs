using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class ProjectileFirefield : ProjectileBase
{
	private float spawnTime;

	private float aliveTime;

	private float spawnRadius;

	private Vector3 normal;

	public Firefield firefield;

	private string damageSource = EWeapon.Flamewalker.ToString();

	protected override bool TryInit(int projectileIndex)
	{
		if (firefield == null || MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null) return false;
		float radius = 4f * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		transform.position -= MyPlayer.Instance.playerRenderer.transform.forward * (radius * 0.5f);
		firefield.Set(transform.position, MyPlayer.Instance.GetFeetPosition(), radius, GetDuration(), 0f, weaponBase, damageSource);
		return true;
	}

	protected override Vector3 GetMovementDirection()
	{
		return default(Vector3);
	}

	protected override void MyFixedUpdate()
	{
	}

	private void CheckDamage()
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

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		return false;
	}
}
