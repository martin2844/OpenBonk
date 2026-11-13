using MilkShake;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class DungeonPressurePlate : MonoBehaviour
{
	private bool opened;

	public GameObject blocker;

	public AudioSource sfxPlate;

	public AudioSource sfxBlocker;

	public ShakePreset shakePreset;

	private Vector3 startPos;

	private Vector3 endPos;

	private float progressTimer;

	private float progressDuration = .55f;

	private void OnTriggerEnter(Collider other)
	{
		if (opened || other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
		Move();
	}

	private void Move()
	{
		opened = true;
		startPos = transform.position;
		endPos = startPos + Vector3.down * .25f;
		if (sfxPlate != null) sfxPlate.Play();
	}

	private void FinishMove()
	{
		if (sfxBlocker != null) sfxBlocker.Play();
		if (PlayerCamera.Instance != null && PlayerCamera.Instance.shaker != null && shakePreset != null)
			PlayerCamera.Instance.shaker.Shake(shakePreset);
		if (blocker != null) blocker.transform.position += blocker.transform.up * 5.5f;
	}

	private void Update()
	{
		if (!opened || progressTimer >= 1f) return;
		progressTimer = Mathf.Clamp01(progressTimer + Time.deltaTime / progressDuration);
		transform.position = Vector3.Lerp(startPos, endPos, Easing.InOutQuad(progressTimer));
		if (progressTimer >= 1f) FinishMove();
	}
}
