using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class IceAura : ConstantAttack
{
	private float defaultRadius;
	public AttackMuzzle attackMuzzle;
	private float radius;
	private float cooldown;
	private float oldRadius;
	private float scaleTimer;
	private float scaleOverTime = 1f;
	private float minCooldown = .06f;
	public ParticleSystem psRing;
	public ParticleSystem psSmoke;
	public ParticleSystem psSnow;
	private ParticleSystem.EmissionModule emissionSmoke;
	private ParticleSystem.EmissionModule emissionSnow;
	private bool inited;
	private float nextCheckDamageTime;

	protected override void Init()
	{
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
		if (attackMuzzle != null) attackMuzzle.Set(1, cooldown);
		RefreshParticles();
	}

	private void RefreshParticles()
	{
		InitParticles();
		float t = Mathf.Clamp01((.4f - cooldown) / .34f);
		if (psSmoke != null) emissionSmoke.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(Mathf.Lerp(11f, 2f, t))) });
		if (psSnow != null) emissionSnow.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(Mathf.Lerp(15f, 2f, t))) });
	}

	private void InitParticles()
	{
		if (inited) return;
		if (psSmoke != null) emissionSmoke = psSmoke.emission;
		if (psSnow != null) emissionSnow = psSnow.emission;
		inited = true;
	}

	private float GetFreezeTime() { return weaponBase.GetValue(EStat.DurationMultiplier); }

	private void FixedUpdate()
	{
		if (weaponBase == null || MyTime.time < nextCheckDamageTime) return;
		nextCheckDamageTime = MyTime.time + cooldown;
		if (attackMuzzle != null) attackMuzzle.Play();
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, radius, out Collider[] hits);
		bool useSfx = true;
		for (int i = 0; i < count; i++)
		{
			Collider hit = hits[i];
			if (EnemyManager.Instance == null || !EnemyManager.Instance.GetEnemy(hit, out var enemy) || enemy == null || enemy.IsDeadOrDyingNextFrame()) continue;
			Vector3 direction = (enemy.transform.position - transform.position).normalized;
			var dc = WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction);
			enemy.DamageFromPlayerWeapon(dc);
			enemy.AddDebuff(EDebuff.Freeze, dc, GetFreezeTime());
			if (EffectManager.Instance != null && MyPlayer.Instance != null)
				EffectManager.Instance.EnemyHitEffect(hit.ClosestPoint(MyPlayer.Instance.transform.position), direction, true, weaponBase.weaponData.eWeapon, null, useSfx);
			useSfx = false;
		}
	}
}
