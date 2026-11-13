using UnityEngine;
using UnityEngine.UI;

public class CreditsWindow : MonoBehaviour
{
	private float nextUpdateTime;

	private int holdingDir;

	private float cooldownTimer;

	public ScrollRect scrollRect;

	private float multiplier;

	private void Update()
	{
		float input = Input.GetAxisRaw("Vertical");
		int direction = input > .2f ? 1 : input < -.2f ? -1 : 0;
		if (direction != holdingDir) ResetCooldown();
		holdingDir = direction;
		if (nextUpdateTime > Time.time) return;
		nextUpdateTime = Time.time + cooldownTimer;
		multiplier *= 1.015f;
		cooldownTimer = Mathf.Clamp(cooldownTimer * .6f, .01f, .35f);
		if (holdingDir != 0 && scrollRect != null)
			scrollRect.verticalNormalizedPosition += holdingDir * .01f * multiplier;
	}

	private void ResetCooldown()
	{
		multiplier = 1f;
		cooldownTimer = .3f;
		nextUpdateTime = Time.time;
	}
}
