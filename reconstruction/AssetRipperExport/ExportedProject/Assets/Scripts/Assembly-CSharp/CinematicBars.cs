using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class CinematicBars : MonoBehaviour
{
	public RectTransform topBar;

	public RectTransform bottomBar;

	public CanvasGroup canvasGroup;

	private float barHeight;

	private float moveTime = 1f;

	private float moveTimer;

	private bool isShowing;

	private void Awake()
	{
		if (topBar != null) barHeight = topBar.rect.height + 1f;
	}

	public void InstaShow()
	{
		isShowing = true;
		moveTimer = 1f;
		if (topBar != null) topBar.gameObject.SetActive(true);
		if (bottomBar != null) bottomBar.gameObject.SetActive(true);
		if (topBar != null) topBar.anchoredPosition = Vector2.zero;
		if (bottomBar != null) bottomBar.anchoredPosition = Vector2.zero;
	}

	public void Show()
	{
		if (isShowing) return;
		isShowing = true;
		moveTimer = 0f;
		if (topBar != null) topBar.anchoredPosition = new Vector2(0f, barHeight);
		if (bottomBar != null) bottomBar.anchoredPosition = new Vector2(0f, -barHeight);
		if (topBar != null) topBar.gameObject.SetActive(true);
		if (bottomBar != null) bottomBar.gameObject.SetActive(true);
	}

	public void Hide()
	{
		if (!isShowing) return;
		isShowing = false;
		moveTimer = 0f;
		if (topBar != null) topBar.anchoredPosition = Vector2.zero;
		if (bottomBar != null) bottomBar.anchoredPosition = Vector2.zero;
	}

	public bool IsShowing()
	{
		return topBar != null && topBar.gameObject.activeSelf;
	}

	private void Update()
	{
		if (moveTimer > 1f) return;
		moveTimer += Mathf.Clamp01(Time.deltaTime / moveTime);
		float progress = Mathf.Clamp01(moveTimer);
		float eased = isShowing ? Easing.OutCirc(progress) : Easing.InOutCubic(progress);
		float offset = barHeight * (isShowing ? 1f - eased : eased);
		if (topBar != null) topBar.anchoredPosition = new Vector2(0f, offset);
		if (bottomBar != null) bottomBar.anchoredPosition = new Vector2(0f, -offset);
		if (canvasGroup != null) canvasGroup.alpha = isShowing ? eased : 1f - eased;
		if (!isShowing && eased >= 1f)
		{
			if (topBar != null) topBar.gameObject.SetActive(false);
			if (bottomBar != null) bottomBar.gameObject.SetActive(false);
		}
	}
}
