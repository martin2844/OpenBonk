using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

public class ObjectiveArrow : MonoBehaviour
{
	private Vector3 offset;

	public Transform tutorialArrow;

	public RectTransform canvasRect;

	private float hideAtDistance;

	private float timeout;

	private float targetAtTime;

	private float minTime = 2f;

	private float scaleMultiplier;

	private Vector3 targetSize = Vector3.one;

	private Vector3 fromScale;

	private float timer;

	private float scaleTime = 0.5f;

	public Transform target { get; set; }

	private void Awake()
	{
		tutorialArrow.localScale = Vector3.zero;
		targetSize = Vector3.zero;
		tutorialArrow.gameObject.SetActive(false);
	}

	public void SetTarget(Transform t, Vector3 offset, float hideAtDistance, float timeout, float scaleMultiplier = 1f)
	{
		target = t;
		this.offset = offset;
		this.hideAtDistance = hideAtDistance;
		this.timeout = timeout;
		this.scaleMultiplier = scaleMultiplier;
		targetAtTime = MyTime.time;
		Show();
	}

	public void ClearTarget()
	{
		target = null;
		Hide();
	}

	private void Show()
	{
		tutorialArrow.gameObject.SetActive(true);
		targetSize = Vector3.one;
		fromScale = tutorialArrow.localScale;
		timer = 0f;
	}

	private void Hide()
	{
		targetSize = Vector3.zero;
		fromScale = tutorialArrow.localScale;
		timer = 0f;
	}

	private bool IsHidden()
	{
		return tutorialArrow.localScale.magnitude < 0.001f;
	}

	private void UpdateSize()
	{
		if (timer >= 1f) return;
		timer += Time.deltaTime / scaleTime;
		tutorialArrow.localScale = Vector3.Lerp(fromScale, targetSize, timer) * scaleMultiplier;
	}

	private void Update()
	{
		UpdateSize();
		if (target != null)
		{
			TargetFollowItem();
			if (MyTime.time >= targetAtTime + timeout) Hide();
			if (Vector3.Distance(target.position, MyPlayer.Instance.transform.position) < hideAtDistance && MyTime.time > targetAtTime + minTime) Hide();
		}
		if (IsHidden() && tutorialArrow.gameObject.activeSelf) tutorialArrow.gameObject.SetActive(false);
	}

	private void TargetFollowItem()
	{
		tutorialArrow.gameObject.SetActive(true);
		Camera camera = Camera.main;
		Vector3 world = calculateWorldPosition(target.position + offset, camera);
		Vector2 screen = camera.WorldToScreenPoint(world);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
		Vector2 limit = canvasRect.sizeDelta * 0.425f;
		local.x = Mathf.Clamp(local.x, -limit.x, limit.x);
		local.y = Mathf.Clamp(local.y, -limit.y, limit.y);
		tutorialArrow.localPosition = new Vector3(local.x, local.y, 0f);
	}

	private Vector3 calculateWorldPosition(Vector3 position, Camera camera)
	{
		Vector3 direction = position - camera.transform.position;
		if (Vector3.Dot(camera.transform.forward, direction.normalized) <= 0f)
			return position - 2f * Vector3.Dot(direction, camera.transform.forward) * camera.transform.forward;
		return position;
	}
}
