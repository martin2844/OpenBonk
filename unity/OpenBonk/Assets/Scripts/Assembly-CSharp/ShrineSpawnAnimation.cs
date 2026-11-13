using MilkShake;
using UnityEngine;
using Assets.Scripts.Utility;
using UnityEngine.UIElements.Experimental;

public class ShrineSpawnAnimation : MonoBehaviour
{
	public float offset;

	private float moveOverTime = 3f;

	public ShakePreset shakePreset;

	private Vector3 fromPos;

	private Vector3 toPos;

	private bool started;

	private float timer;

	public void Activate()
	{
		started = true;
		gameObject.SetActive(true);
		transform.position += Vector3.down * offset;
		fromPos = transform.position;
		toPos = transform.position + Vector3.up * offset;
		if (PlayerCamera.Instance != null && PlayerCamera.Instance.shaker != null && shakePreset != null)
			PlayerCamera.Instance.shaker.Shake(shakePreset);
	}

	private void FixedUpdate()
	{
		if (!started || timer >= moveOverTime) return;
		timer = Mathf.Min(timer + MyTime.fixedDeltaTime, moveOverTime);
		transform.position = Vector3.Lerp(fromPos, toPos, Easing.OutCirc(timer / moveOverTime));
	}
}
