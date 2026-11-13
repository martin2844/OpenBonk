using UnityEngine;
using Assets.Scripts.Actors.Player;

public class JumpPad : MonoBehaviour
{
	public float force = 35f;

	public Transform spring;

	public Transform direction;

	private float animationTime = -1f;

	private float animationScale = 5f;

	private float animationSpeed = 5f;

	public AudioSource audioSource;

	private Vector3 defaultScale;

	private void Awake()
	{
		if (spring != null) defaultScale = spring.localScale;
	}

	public Vector3 GetForce()
	{
		return (direction != null ? direction.forward : Vector3.up) * force;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (MyPlayer.Instance == null || other.gameObject != MyPlayer.Instance.gameObject) return;
		MyPlayer.Instance.playerMovement.JumpPad(this);
		AnimateSpring();
	}

	private void AnimateSpring()
	{
		if (audioSource != null) audioSource.Play();
		animationTime = 0f;
	}

	private void Update()
	{
		if (animationTime < 0f || animationTime >= 1f || spring == null) return;
		animationTime += Time.deltaTime * animationSpeed;
		if (animationTime > 1f)
		{
			animationTime = 1f;
			spring.localScale = defaultScale;
			return;
		}
		float t = animationTime < 0.5f ? animationTime * 2f : 2f - animationTime * 2f;
		float scale = UnityEngine.UIElements.Experimental.Easing.InOutCubic(t) * animationScale;
		spring.localScale = Vector3.Lerp(spring.localScale, defaultScale + Vector3.up * scale, scale);
	}
}
