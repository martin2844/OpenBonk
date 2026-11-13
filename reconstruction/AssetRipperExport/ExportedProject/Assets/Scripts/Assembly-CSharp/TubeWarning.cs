using System;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

public class TubeWarning : MonoBehaviour
{
	public ParticleSystem ps;
	private float timer;
	private float fixedTimer;
	private float warningTime;
	private Action completeAction;
	private bool done;

	public void Set(float radius, float length, float time, Action completeAction)
	{
		gameObject.SetActive(true);
		ps.Stop();
		ps.time = 0f;
		ps.Play();
		var main = ps.main;
		main.startLifetime = time;
		ps.transform.localScale = new Vector3(radius, radius, length);
		timer = 0f;
		warningTime = time;
		this.completeAction = completeAction;
		done = false;
	}

	private void Update()
	{
		ps.Simulate(MyTime.deltaTime, true, false, false);
		if (timer >= 1f || GameManager.Instance.IsTimeFreeze()) return;
		timer = Mathf.Clamp01(timer + MyTime.deltaTime / warningTime);
		if (timer >= 1f) { completeAction?.Invoke(); ReleaseToPool(); }
	}

	public void ReleaseToPool()
	{
		PoolManager.Instance.warningTubePool.Release(gameObject);
	}
}
