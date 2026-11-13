using UnityEngine;
using Assets.Scripts.Utility;

public class Boulder : MonoBehaviour
{
	public Rigidbody rb;

	private float defaultSize = 2f;

	public float frictionStrength = 0.5f;

	public AudioSource audio;

	private float minSpeedVolume = 2f;

	private float maxVolumeSpeed = 20f;

	private void Start()
	{
		rb.mass *= transform.localScale.x / defaultSize;
	}

	private void FixedUpdate()
	{
		if (rb.velocity.sqrMagnitude > 0.001f)
			rb.AddForce(rb.velocity.normalized * frictionStrength * MyTime.fixedDeltaTime * -50f);
		if (rb.angularVelocity.sqrMagnitude > 0.001f)
			rb.AddTorque(rb.angularVelocity.normalized * frictionStrength * -0.2f * MyTime.fixedDeltaTime * 50f);
	}

	private void Update()
	{
		if (MyTime.paused) return;
		audio.volume = Mathf.Clamp01((rb.velocity.magnitude - minSpeedVolume) / (maxVolumeSpeed - minSpeedVolume)) * 0.85f;
		if (audio.volume <= 0f && audio.isPlaying) audio.Stop();
		if (audio.volume > 0f && !audio.isPlaying) audio.Play();
	}
}
