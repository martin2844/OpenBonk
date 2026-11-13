using System.Collections.Generic;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public class CoolGhostsAttack : ConstantAttack
{
	public GameObject projectilePrefab;

	private List<GameObject> projectiles;

	private int maxProjectiles;

	private List<float> angles;

	private float radius;

	protected override void Init()
	{
	}

	private void RefreshProjectiles()
	{
	}

	private void FixedUpdate()
	{
	}

	private void EnsureAngles(int count)
	{
	}

	protected override void OnWeaponStatUpdate(EStat stat, EWeapon weapon)
	{
	}

	protected override void OnStatUpdate(EStat stat)
	{
	}

	public override float GetAuraRotationSpeed()
	{
		return 0f;
	}

	public GameObject GetNewProjectile()
	{
		return null;
	}

	public void RemoveProjectile(GameObject projectile)
	{
	}
}
