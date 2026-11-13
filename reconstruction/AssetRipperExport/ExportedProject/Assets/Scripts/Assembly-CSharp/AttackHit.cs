using UnityEngine;
using UnityEngine.Pool;

public class AttackHit : MonoBehaviour
{
	public float timeout;

	public RandomSfx randomSfx;

	public ParticleSystem ps;

	public ObjectPool<GameObject> pool;

	public AudioClip enemyHitSfx;

	public AudioClip wallHitSfx;

	public void Play(bool hitEnemy, bool useSfx)
	{
		gameObject.SetActive(true);
		if (timeout <= 0f) throw new System.Exception("Attack hit timeout must be positive.");
		if (useSfx && randomSfx != null && randomSfx.s != null && randomSfx.s.enabled)
		{
			if (randomSfx.sounds != null && randomSfx.sounds.Length > 0)
				randomSfx.sounds[0] = hitEnemy ? enemyHitSfx : wallHitSfx;
			randomSfx.Play();
		}
		ps.Play();
		Invoke(nameof(ReleaseToPool), timeout);
	}

	private void ReleaseToPool()
	{
		pool.Release(gameObject);
	}

	private void OnValidate()
	{
	}
}
