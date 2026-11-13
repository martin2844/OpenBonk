using MilkShake;
using UnityEngine;
using Assets.Scripts.Managers;

public class PlayerFx : MonoBehaviour
{
	public GameObject jumpFx;

	public GameObject landFx;

	public PlayerMovement playerMovement;

	public ShakePreset landingShake;

	private void Awake()
	{
		PlayerMovement.A_Landed += OnLanded;
		PlayerMovement.A_Jumped += OnJumped;
	}

	private void OnDestroy()
	{
		PlayerMovement.A_Landed -= OnLanded;
		PlayerMovement.A_Jumped -= OnJumped;
	}

	private void OnJumped(PlayerMovement obj)
	{
		if (playerMovement == null || playerMovement.feet == null || playerMovement.GetRb() == null || jumpFx == null) return;
		GameObject effect = ParticleSpawner.SpawnParticles(jumpFx, playerMovement.feet.position, jumpFx.transform.rotation);
		ParticleSystem particles = effect.GetComponent<ParticleSystem>();
		if (particles == null) return;
		var velocity = particles.velocityOverLifetime;
		Vector3 playerVelocity = playerMovement.GetRb().velocity;
		velocity.x = playerVelocity.x * 0.5f;
		velocity.z = playerVelocity.z * 0.5f;
	}

	private void OnLanded(float fallSpeed)
	{
		if (fallSpeed < 8f || playerMovement == null || playerMovement.feet == null || playerMovement.GetRb() == null || landFx == null) return;
		GameObject effect = ParticleSpawner.SpawnParticles(landFx, playerMovement.feet.position, Quaternion.LookRotation(Vector3.up));
		ParticleSystem particles = effect.GetComponent<ParticleSystem>();
		if (particles == null) return;
		var velocity = particles.velocityOverLifetime;
		Vector3 playerVelocity = playerMovement.GetRb().velocity;
		velocity.x = playerVelocity.x * 1.4f;
		velocity.z = playerVelocity.z * 1.4f;
		if (PlayerCamera.Instance != null && PlayerCamera.Instance.shaker != null && landingShake != null)
			PlayerCamera.Instance.shaker.Shake(landingShake);
	}
}
