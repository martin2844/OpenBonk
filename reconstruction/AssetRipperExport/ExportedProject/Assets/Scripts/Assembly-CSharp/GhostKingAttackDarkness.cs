using System;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Utility;
using MilkShake;
using UnityEngine;

public class GhostKingAttackDarkness : EnemySpecialAttackPrefab
{
	public GameObject blastSphere;

	public AudioSource audioStart;

	public AudioSource audioCharge;

	public AudioSource audioBlast;

	public AudioSource audioLampSave;

	public ParticleSystem[] chargeParticles;

	public ParticleSystem blastParticles;

	public ShakePreset shakePreset;

	public static Action<float> A_LightIntensity;

	public static Action A_DarknessAttackSetEnemyTarget;

	public static Action A_Explode;

	private float timer;

	private float explodeAtTime;

	private float unchargeTime = 1.5f;

	private float sphereScaleBeforeBlast = 10f;

	private bool hasBlasted;

	private bool hasDamaged;

	private float blastToDamageDelay = 0.1f;

	private bool enemyDied;

	protected override void Init()
	{
		timer = 0f;
		explodeAtTime = specialAttack.attackChargeTime;
		hasBlasted = false;
		hasDamaged = false;
		enemyDied = false;
		foreach (ParticleSystem particle in chargeParticles) { var emission = particle.emission; emission.enabled = true; }
		audioStart.Play();
		audioCharge.Play();
		A_DarknessAttackSetEnemyTarget?.Invoke();
	}

	private void FixedUpdate()
	{
		transform.position = enemy.transform.position;
		foreach (ParticleSystem particle in chargeParticles) particle.transform.position = enemy.GetFeetPosition();
		if (enemy.state == EEnemyState.Idle) A_DarknessAttackSetEnemyTarget?.Invoke();
		timer += enemyDied ? -MyTime.fixedDeltaTime : MyTime.fixedDeltaTime;
		if (hasBlasted)
		{
			float progress = Mathf.Clamp01(timer / unchargeTime);
			A_LightIntensity?.Invoke(UnityEngine.UIElements.Experimental.Easing.InCubic(progress));
			blastSphere.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 500f, progress * 2f);
			if (timer >= blastToDamageDelay && !hasDamaged) BlastDamage();
			if (timer >= unchargeTime) FinishAttack();
		}
		else
		{
			float progress = Mathf.Clamp01(timer / specialAttack.attackChargeTime);
			A_LightIntensity?.Invoke(UnityEngine.UIElements.Experimental.Easing.InSine(1f - progress));
			audioCharge.pitch = 0.75f + 1.25f * progress;
			audioCharge.volume = 0.3f + 0.3f * UnityEngine.UIElements.Experimental.Easing.OutCubic(progress);
			if (timer >= explodeAtTime && !hasBlasted) Blast();
			if (enemyDied && timer <= 0f) FinishAttack();
		}
	}

	private void Blast()
	{
		hasBlasted = true;
		timer = 0f;
		foreach (ParticleSystem particle in chargeParticles) { var emission = particle.emission; emission.enabled = false; particle.Stop(); }
		blastParticles.Play();
		blastSphere.SetActive(true);
		audioCharge.Stop();
		audioBlast.Play();
		PlayerCamera.Instance.shaker.Shake(shakePreset);
	}

	private void BlastDamage()
	{
		A_Explode?.Invoke();
		hasDamaged = true;
		if (GraveyardBossRoom.isPlayerInsideLight) { audioLampSave.Play(); return; }
		var health = MyPlayer.Instance.inventory.playerHealth;
		float damage = enemy.GetHpRatio() < 0.75f ? health.GetCombinedMaxHp() * 2f : health.GetCombinedMaxHp() * 0.75f;
		Vector3 direction = MyPlayer.Instance.transform.position - enemy.transform.position;
		direction.y = 0f;
		health.DamagePlayerExternal(damage, 10f, direction.normalized, true, "Enemy", Assets.Scripts.Actors.DcFlags.BypassAll);
	}

	private void FinishAttack()
	{
		A_LightIntensity?.Invoke(1f);
		blastSphere.SetActive(false);
		gameObject.SetActive(false);
		ReturnToPool();
	}

	protected override void OnEnemyDied(Enemy enemy)
	{
		if (enemy != this.enemy) return;
		foreach (ParticleSystem particle in chargeParticles) { var emission = particle.emission; emission.enabled = false; }
		enemyDied = true;
	}
}
