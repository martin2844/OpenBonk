using MilkShake;
using UnityEngine;

public class GraveyardBossMetalGate : MonoBehaviour
{
	public Transform uiArrowTarget;

	public AudioSource sfxOpen;

	public ShakePreset shakePreset;

	private bool isOpen;

	private float timer;

	private float moveOverTime = 2f;

	private Vector3 startPos;

	private Vector3 endPos;

	public void Open()
	{
		if (isOpen) return;
		isOpen = true;
		if (UiManager.Instance != null && UiManager.Instance.objectiveArrow != null)
			UiManager.Instance.objectiveArrow.SetTarget(uiArrowTarget, Vector3.up * 4f, 6f, 60f, 1f);
		startPos = transform.position;
		endPos = startPos + Vector3.up * 12f;
		if (sfxOpen != null) sfxOpen.Play();
		if (PlayerCamera.Instance != null && PlayerCamera.Instance.shaker != null && shakePreset != null)
			PlayerCamera.Instance.shaker.Shake(shakePreset);
	}

	private void Update()
	{
		if (!isOpen || timer >= moveOverTime) return;
		timer += Time.deltaTime;
		float progress = Mathf.Clamp01(timer / moveOverTime);
		transform.position = Vector3.Lerp(startPos, endPos, UnityEngine.UIElements.Experimental.Easing.InOutQuad(progress));
	}
}
