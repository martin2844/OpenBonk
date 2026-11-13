using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class GoldFilterUI : MonoBehaviour
{
	public CanvasGroup canvas;
	public RawImage goldenUi;
	public Transform lines1;
	public Transform lines2;
	private float timeLeft;

	private void Awake()
	{
		Pickup.A_PickupTriggered += OnPickup;
	}

	private void OnDestroy()
	{
		Pickup.A_PickupTriggered -= OnPickup;
	}

	private void OnPickup(Pickup pickup)
	{
		if (pickup != null && pickup.ePickup == EPickup.Stonks) timeLeft = PowerupConstants.GetStonksTime();
	}

	private void Update()
	{
		if (timeLeft <= 0f) return;
		timeLeft = Mathf.Max(0f, timeLeft - MyTime.deltaTime);
		if (canvas != null) canvas.alpha = timeLeft > 1f ? Mathf.Lerp(canvas.alpha, 1f, MyTime.deltaTime * 5f) : 1f - Mathf.Clamp01(1f - timeLeft);
		if (goldenUi != null)
		{
			Color color = goldenUi.color;
			color.a = 0.25f - Mathf.Abs(Mathf.Repeat(MyTime.time * 0.25f, 0.2f) - 0.1f);
			goldenUi.color = color;
		}
		if (lines1 != null)
		{
			lines1.Rotate(Vector3.forward, 20f * MyTime.deltaTime);
			float target = 2.5f - Mathf.Abs(Mathf.Repeat(MyTime.time, 1f) - 0.5f);
			float factor = Easing.InOutQuad((MyTime.time * 0.1f) % 1f);
			lines1.localScale = Vector3.one * Mathf.Lerp(lines1.localScale.x, target, factor);
		}
		if (lines2 != null) lines2.Rotate(Vector3.forward, -20f * MyTime.deltaTime);
	}
}
