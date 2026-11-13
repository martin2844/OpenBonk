using System;
using UnityEngine;
using Assets.Scripts.Utility;

public class SmokeAndShakeObject : MonoBehaviour
{
	[Range(0.5f, 3f)]
	public float shakeStrength = 1f;

	private float readyAtTime;

	private float maxDistanceToPlayer = 6400f;

	private float minSpeed = 10f;

	private float maxSpeed = 40f;

	public static Action<float> A_Impact;

	private void OnCollisionEnter(Collision other)
	{
		if (other == null || other.gameObject.layer == LayerMask.NameToLayer("Player") || readyAtTime > MyTime.time || other.relativeVelocity.magnitude < minSpeed || other.contactCount == 0) return;
		var contact = other.GetContact(0);
		if (EffectManager.Instance == null || EffectManager.Instance.smokeHit == null) return;
		Instantiate(EffectManager.Instance.smokeHit, contact.point, Quaternion.LookRotation(contact.normal));
		readyAtTime = MyTime.time + .5f;
	}
}
