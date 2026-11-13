using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine.UIElements.Experimental;

public class StealWeaponWui : MonoBehaviour
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

	private float floatAbovePlayerHeadTime = 1f;

	private float moveTime = 1f;

	public void Set(UnlockableBase unlockable, Transform target, Vector3 targetOffset, float hoverTime, float moveTime, float scale, bool useScaleDown = false)
	{
		icon.texture = unlockable.GetIcon();
		this.target = target;
		this.targetOffset = targetOffset;
		transform.position = MyPlayer.Instance.transform.position;
		transform.localScale = Vector3.zero;
		this.scale = scale;
		this.useScaleDown = useScaleDown;
		timer = moveUpTimer = 0f;
		floatAbovePlayerHeadTime = hoverTime;
		moveUpTime = Mathf.Min(0.75f, hoverTime);
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
			if (target == null) { Destroy(gameObject); return; }
			timer = Mathf.Clamp01(timer + MyTime.deltaTime / moveTime);
			float t = Easing.InOutQuad(timer);
			transform.position = Vector3.Lerp(abovePlayer, target.position + targetOffset, t);
			transform.localScale = Vector3.Lerp(Vector3.one * scale, Vector3.zero, t);
			if (timer >= 1f) Destroy(gameObject);
		}
	}
}
