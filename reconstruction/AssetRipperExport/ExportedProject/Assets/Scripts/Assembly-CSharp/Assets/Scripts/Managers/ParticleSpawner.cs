using UnityEngine;

namespace Assets.Scripts.Managers
{
	public class ParticleSpawner : MonoBehaviour
	{
		public static GameObject SpawnParticles(GameObject particles, Vector3 position, Quaternion rotation)
		{
			return Instantiate(particles, position, rotation);
		}
	}
}
