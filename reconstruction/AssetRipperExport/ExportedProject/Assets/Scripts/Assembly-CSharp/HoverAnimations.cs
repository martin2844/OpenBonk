using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

public class HoverAnimations : MonoBehaviour
{
	public Transform player;

	public AudioSource audioHoverLoop;

	public AudioSource audioSpin;

	public Animator animator;

	private Vector3 defaultPos;

	private Vector3 defaultRotation;

	private float currentPitch;

	private float nextLandingReadyTime;

	private float landingInterval = .25f;

	private float minLandingSpeed = 4f;

	private float maxLandingSpeed = 20f;

	public float sinSpeed = 5f;

	public float height = .5f;

	private float currentLandingOffset;

	private float landingOffset;

	private float minLandingOffset = 1f;

	private float maxLandingOffset = 2.3f;

	public float landingResetSpeed = 5f;

	public float landingSpeed = 8f;

	private float lastVelY;

	private float currentLean;

	private float targetLean;

	public float maxLeanAngle = 25f;

	public float leanSpeed = 5f;

	public float maxSpeedForLean = 40f;

	private float newLean;

	private void Start()
	{
		defaultPos = player.localPosition;
		defaultRotation = player.localEulerAngles;
	}

	private void Update()
	{
		if (MyPlayer.Instance == null) return;
		var movement = MyPlayer.Instance.playerMovement;
		float desiredPitch = 1f + Mathf.Clamp01(movement.GetSpeedHorizontal() / 60f) * 6f;
		currentPitch = Mathf.Lerp(currentPitch, desiredPitch, MyTime.deltaTime * 2f);
		float pulse = .15f - Mathf.Abs(Mathf.Repeat(MyTime.time * .6f, .3f) - .15f);
		audioHoverLoop.pitch = currentPitch + pulse;
		float desiredSpinVolume = !movement.grounded && !movement.IsCrouching() ? .09f : 0f;
		audioSpin.volume = Mathf.Lerp(audioSpin.volume, desiredSpinVolume, MyTime.deltaTime * 7f);
	}

	private void OnLanded(float speed)
	{
		if (MyTime.time < nextLandingReadyTime) return;
		nextLandingReadyTime = MyTime.time + landingInterval;
		if (speed < minLandingSpeed) return;
		float amount = Mathf.InverseLerp(minLandingSpeed, maxLandingSpeed, speed);
		landingOffset = Mathf.Lerp(minLandingOffset, maxLandingOffset, amount);
		currentPitch -= Mathf.Max(.2f, amount * .5f);
	}

	private void LateUpdate()
	{
		float bob = (Mathf.Sin(MyTime.time * sinSpeed) * .5f + .5f) * height;
		player.localPosition = defaultPos + Vector3.up * bob + Vector3.down * currentLandingOffset;
		landingOffset = Mathf.Lerp(landingOffset, 0f, MyTime.deltaTime * landingResetSpeed);
		currentLandingOffset = Mathf.Lerp(currentLandingOffset, landingOffset, MyTime.deltaTime * landingSpeed);
		LeanRotation();
	}

	private void LeanRotation()
	{
		if (MyPlayer.Instance == null) return;
		Vector3 velocity = MyPlayer.Instance.playerMovement.GetVelocity();
		velocity.y = 0f;
		float speed = velocity.magnitude;
		targetLean = speed < .01f ? 0f : player.InverseTransformDirection(velocity.normalized).x * maxLeanAngle * Mathf.Clamp01(speed / maxSpeedForLean);
		currentLean = Mathf.Lerp(currentLean, targetLean, MyTime.deltaTime * leanSpeed);
		player.localRotation = Quaternion.Euler(defaultRotation.x, defaultRotation.y, defaultRotation.z + currentLean);
	}

	private void FixedUpdate()
	{
		if (MyPlayer.Instance == null) return;
		float velocityY = MyPlayer.Instance.playerMovement.GetVelocity().y;
		float change = velocityY - lastVelY;
		if (Mathf.Abs(change) > minLandingSpeed) OnLanded(change);
		lastVelY = velocityY;
	}
}
