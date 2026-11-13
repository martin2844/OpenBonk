using UnityEngine;
using UnityEngine.Pool;
using Assets.Scripts.Utility;

public class ReturnToPool : MonoBehaviour
{
	private float timeout;

	private float returnTime;

	public ObjectPool<GameObject> pool;

	public void SetTime(float timeout, ObjectPool<GameObject> pool)
	{
		this.timeout = timeout;
		this.pool = pool;
		gameObject.SetActive(true);
		returnTime = MyTime.time + timeout;
	}

	private void OnEnable()
	{
		returnTime = MyTime.time + timeout;
	}

	private void Update()
	{
		if (MyTime.time < returnTime) return;
		gameObject.SetActive(false);
		pool.Release(gameObject);
	}
}
