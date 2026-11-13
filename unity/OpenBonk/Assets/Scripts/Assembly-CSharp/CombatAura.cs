using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class CombatAura : ConstantAttack
{
	private float defaultRadius;
	public ParticleSystem[] particles;
	private Color[] defaultColors;
	private float radius;
	private float cooldown;
	private float oldRadius;
	private float scaleTimer;
	private float scaleOverTime = 1f;
	private float fadeMultiplier = .25f;
	private float minSizeMultiplier = 1.5f;
	private float maxSizeMultiplier = 8f;
	private float minCooldown = .04f;
	private float nextCheckDamageTime;

	protected override void Init()
	{
		defaultColors = new Color[particles == null ? 0 : particles.Length];
		for (int i = 0; i < defaultColors.Length; i++)
			if (particles[i] != null) defaultColors[i] = particles[i].main.startColor.color;
		defaultRadius = transform.localScale.x;
		UpdateSize();
		UpdateCooldown();
	}

	protected override void OnWeaponStatUpdate(EStat stat, EWeapon weapon)
	{
		if (weaponBase != null && weaponBase.weaponData.eWeapon == weapon) OnStatUpdate(stat);
	}

	protected override void OnStatUpdate(EStat stat)
	{
		if (weaponBase == null) return;
		if (stat == EStat.SizeMultiplier) UpdateSize();
		else if (stat == EStat.AttackSpeed) UpdateCooldown();
	}

	public override float GetAuraRotationSpeed() { return 2f; }

	private void UpdateSize()
	{
		oldRadius = radius;
		scaleTimer = 0f;
		radius = defaultRadius * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		transform.localScale = Vector3.one * radius;
	}

	private void Update()
	{
		if (scaleTimer >= 1f) return;
		scaleTimer = Mathf.Min(1f, scaleTimer + MyTime.deltaTime / scaleOverTime);
		transform.localScale = Vector3.one * Mathf.Lerp(oldRadius, radius, Easing.InOutCirc(scaleTimer));
	}

	private void UpdateCooldown()
	{
		cooldown = Mathf.Max(WeaponUtility.GetWeaponCooldown(weaponBase), minCooldown);
	}

	private void FixedUpdate()
	{
		if (weaponBase == null || MyTime.time < nextCheckDamageTime) return;
		nextCheckDamageTime = MyTime.time + cooldown;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, radius, out Collider[] hits);
		int effects = 0;
		bool useSfx = true;
		for (int i = 0; i < count; i++)
		{
			Collider hit = hits[i];
			if (EnemyManager.Instance == null || !EnemyManager.Instance.GetEnemy(hit, out var enemy) || enemy == null || enemy.IsDeadOrDyingNextFrame()) continue;
			Vector3 direction = (enemy.transform.position - transform.position).normalized;
			var dc = WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction);
			enemy.DamageFromPlayerWeapon(dc);
			if (effects >= 20) continue;
			if (EffectManager.Instance != null && MyPlayer.Instance != null)
				EffectManager.Instance.EnemyHitEffect(hit.ClosestPointOnBounds(MyPlayer.Instance.transform.position), direction, true, weaponBase.weaponData.eWeapon, null, useSfx);
			useSfx = false;
			effects++;
		}
	}
}
