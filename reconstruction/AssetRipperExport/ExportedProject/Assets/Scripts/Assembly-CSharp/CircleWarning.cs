using System;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

public class CircleWarning : MonoBehaviour
{
	public float warningTime = 2f;
	public Transform filler;
	private float defaultProjectorSize = 0.52f;
	private float timer;
	private Action finishAction;
	private Vector3 desiredScale;

	public void Set(float radius, float warningTime, Action finishAction)
	{
		this.finishAction = finishAction;
		desiredScale = Vector3.one * (radius * 2f);
		filler.localScale = Vector3.zero;
		this.warningTime = warningTime;
		timer = 0f;
	}

	private void Update()
	{
		if (timer >= 1f || MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze)) return;
		timer = Mathf.Clamp01(timer + MyTime.deltaTime / warningTime);
		filler.localScale = desiredScale * (1f - (1f - timer) * (1f - timer));
		if (timer >= 1f) { finishAction?.Invoke(); ReleaseToPool(); }
	}

	public void ReleaseToPool()
	{
		PoolManager.Instance.warningSpherePool.Release(gameObject);
	}
}
