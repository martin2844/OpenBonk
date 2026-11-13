using System.Collections.Generic;
using UnityEngine;

public class SkatingAnimations : MonoBehaviour
{
	public Animator animator;

	private PlayerMovement playerMovement;

	public AudioSource skateSfx;

	private string lastAnimationName;

	private float maxVolume = .15f;

	private float skateVolumeBoost = 1f;

	private float idleThreshold = 1f;

	private Queue<float> speedChangeQueue = new Queue<float>();

	private Queue<float> rotationChangeQueue = new Queue<float>();

	private float previousSpeed;

	private Vector3 previousForward;

	private int averageFrameCount = 10;

	private float speedChangeThreshold = .06f;

	private float rotationChangeThreshold = 2f;

	private float nextKickTime;

	private float minKickInterval = .5f;

	private float maxKickInterval = 1.5f;

	private void Awake()
	{
		playerMovement = transform.root.GetComponent<PlayerMovement>();
	}

	private void Update()
	{
		if (playerMovement == null) return;
		float speed = playerMovement.GetVelocity().magnitude;
		if (playerMovement.IsTouchingGround() && speed > idleThreshold)
		{
			if (!skateSfx.isPlaying) { skateSfx.volume = 0f; skateSfx.Play(); }
			float volume = speed < 15f ? maxVolume * ((speed - 1f) / 15f) : maxVolume;
			skateSfx.volume = Mathf.Lerp(skateSfx.volume, volume * skateVolumeBoost, Time.deltaTime * 15f);
		}
		else if (skateSfx.isPlaying)
		{
			skateSfx.volume = Mathf.Lerp(skateSfx.volume, 0f, Time.deltaTime * 15f);
			if (skateSfx.volume <= .005f) skateSfx.Stop();
		}
		skateVolumeBoost = Mathf.Lerp(skateVolumeBoost, 1f, Time.deltaTime * 8f);
	}

	public void Kick()
	{
		skateVolumeBoost = 3f;
	}

	private void FixedUpdate()
	{
		if (playerMovement == null) return;
		Rigidbody body = playerMovement.GetRb();
		float speed = body.velocity.magnitude;
		speedChangeQueue.Enqueue(speed - previousSpeed);
		if (speedChangeQueue.Count > averageFrameCount) speedChangeQueue.Dequeue();
		float speedChange = 0f;
		foreach (float value in speedChangeQueue) speedChange += value;
		speedChange /= speedChangeQueue.Count;
		bool kicking = speedChange > speedChangeThreshold;
		if (speedChange > -.02f && GameManager.Instance != null &&
		    Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 1.5f, GameManager.Instance.whatIsGround))
			kicking |= Vector3.Angle(Vector3.up, hit.normal) > 5f && body.velocity.y > 1f;
		previousSpeed = speed;
		Vector3 forward = transform.forward;
		rotationChangeQueue.Enqueue(Vector3.Angle(previousForward, forward));
		if (rotationChangeQueue.Count > averageFrameCount) rotationChangeQueue.Dequeue();
		float rotationChange = 0f;
		foreach (float value in rotationChangeQueue) rotationChange += value;
		rotationChange /= rotationChangeQueue.Count;
		if (rotationChange > rotationChangeThreshold) kicking = true;
		previousForward = forward;
		Vector3 wish = playerMovement.GetWishDir();
		if (Time.time >= nextKickTime && wish != Vector3.zero)
		{
			nextKickTime = Time.time + Random.Range(minKickInterval, maxKickInterval);
			kicking = true;
		}
		animator.SetBool("idle", wish == Vector3.zero && body.velocity.magnitude < idleThreshold);
		animator.SetBool("kicking", kicking);
	}
}
