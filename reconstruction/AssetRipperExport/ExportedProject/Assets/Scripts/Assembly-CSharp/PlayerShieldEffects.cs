using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEngine;

public class PlayerShieldEffects : MonoBehaviour
{
	public ParticleSystem shieldBreakFx;

	public ParticleSystem shieldChargeFx;

	public AudioSource shieldBreakSfx;

	public AudioSource shieldChargeSfx;

	private bool shieldBroken;

	private void Awake()
	{
		PlayerHealth.A_TakeDamage += OnDamage;
		PlayerHealth.A_Heal += OnHeal;
	}

	private void OnDestroy()
	{
		PlayerHealth.A_TakeDamage -= OnDamage;
		PlayerHealth.A_Heal -= OnHeal;
	}

	private void OnDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
	{
		if (!shieldDamage || ph == null || ph.shield > 0f) return;
		if (shieldBreakFx != null) shieldBreakFx.Play();
		if (shieldBreakSfx != null) shieldBreakSfx.Play();
		shieldBroken = true;
	}

	private void OnHeal(PlayerHealth ph, float amount, bool isShield)
	{
		if (!shieldBroken || ph == null || ph.shield <= 0f) return;
		if (shieldChargeFx != null) shieldChargeFx.Play();
		if (shieldChargeSfx != null) shieldChargeSfx.Play();
		shieldBroken = false;
	}
}
