using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Actors.Enemies
{
	public class EnemySpawnParticles : MonoBehaviour
	{
		public RandomSfx audio;

		public ParticleSystem ps;

		public void Set(Enemy enemy)
		{
			gameObject.SetActive(true);
			transform.position = enemy.GetFeetPosition();
			ps.Play();
			audio.Play();
			Invoke(nameof(Release), 2f);
		}

		public void Release()
		{
			gameObject.SetActive(false);
			PoolManager.Instance.enemySpawnFxPool.Release(gameObject);
		}
	}
}
