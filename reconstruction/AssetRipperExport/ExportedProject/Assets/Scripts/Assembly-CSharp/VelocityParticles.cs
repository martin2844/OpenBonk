using UnityEngine;

public class VelocityParticles : MonoBehaviour
{
	public ParticleSystem[] particleSystems;

	private ParticleSystem.VelocityOverLifetimeModule[] velocityModules;

	private Vector3 velocity;

	private Vector3 previousPos;

	private void Start()
	{
	}

	private void Update()
	{
	}
}
