using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine.UIElements.Experimental;

public class ReturnWeaponWui : MonoBehaviour
{
	public RawImage icon;

	private Transform target;

	private Vector3 targetOffset;

	private float scale;

	private bool useScaleDown;

	private int phase;

	private float timer;

	private float moveUpTimer;

	private float moveUpTime;

	private float floatAbovePlayerHeadTime = 1.5f;

	private float scaleDownTime = 1f;

	public void Set(UnlockableBase unlockable)
	{
		icon.texture = unlockable.GetIcon();
		transform.position = MyPlayer.Instance.transform.position;
		transform.localScale = Vector3.zero;
		timer = moveUpTimer = 0f;
		scale = 1.5f;
		moveUpTime = Mathf.Min(0.75f, floatAbovePlayerHeadTime);
	}

	private void Update()
	{
		if (MyPlayer.Instance == null) { Destroy(gameObject); return; }
		Vector3 playerPosition = MyPlayer.Instance.transform.position;
		Vector3 abovePlayer = playerPosition + Vector3.up * MyPlayer.Instance.height;
		if (phase == 0)
		{
			transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, Time.deltaTime * 4f);
			timer = Mathf.Clamp01(timer + MyTime.deltaTime / floatAbovePlayerHeadTime);
			moveUpTimer = Mathf.Clamp01(moveUpTimer + MyTime.deltaTime / moveUpTime);
			transform.position = playerPosition + Vector3.up * MyPlayer.Instance.height * Easing.InOutCirc(moveUpTimer);
			if (timer >= 1f) { timer = 0f; phase++; }
		}
		else if (phase == 1)
		{
			timer = Mathf.Clamp01(timer + MyTime.deltaTime / scaleDownTime);
			transform.position = Vector3.Lerp(abovePlayer, playerPosition, Easing.InOutCirc(timer));
			transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 4f);
			if (timer >= 1f) Destroy(gameObject);
		}
	}
}
