using UnityEngine;
using Assets.Scripts.Utility;

public class DisableAfterTime : MonoBehaviour
{
	public float disableDelay;

	private float disableAtTime;

	private void OnEnable()
	{
		disableAtTime = MyTime.time + disableDelay;
	}

	private void Update()
	{
		if (MyTime.time > disableAtTime) gameObject.SetActive(false);
	}
}
