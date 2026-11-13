using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Managers;

public class SelectionArrow : MonoBehaviour
{
	public RectTransform rectTransform;

	private RectTransform selectedRect;

	public Image renderer;

	public RectTransform mask;

	private Transform defaultParent;

	public static SelectionArrow Instance;

	public float sizeOffset = 30f;

	private Vector2 newSize;

	private float fps = 5f;

	private float nextUpdateTime;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		defaultParent = transform.parent;
		ButtonManager.A_ButtonHover += OnButtonHover;
		CurrentSettings.A_ResolutionChanged += OnResolutionChanged;
		Window.A_WindowOpenedFirstTime += Refresh;
	}

	private void Start()
	{
		Invoke(nameof(FirstSelection), 0.02f);
	}

	private void FirstSelection()
	{
		OnButtonHover(ButtonManager.GetCurrentButton());
	}

	private void OnDestroy()
	{
		ButtonManager.A_ButtonHover -= OnButtonHover;
		CurrentSettings.A_ResolutionChanged -= OnResolutionChanged;
		Window.A_WindowOpenedFirstTime -= Refresh;
		if (Instance == this) Instance = null;
	}

	private void OnButtonHover(MyButton btn)
	{
		if (btn == null) return;
		OnButtonHover(btn.GetComponent<RectTransform>());
		if (mask == null) return;
		var scroll = btn.GetComponentInParent<ScrollRect>();
		if (scroll != null)
		{
			var viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
			if (viewport == null) return;
			mask.position = viewport.position;
			mask.rotation = viewport.rotation;
			mask.sizeDelta = viewport.rect.size;
		}
		else if (defaultParent is RectTransform parent)
		{
			mask.localPosition = Vector3.zero;
			mask.sizeDelta = parent.rect.size;
		}
	}

	private void OnButtonHover(RectTransform button)
	{
		if (button == null) return;
		selectedRect = button;
		UpdatePosition();
		if (rectTransform != null) rectTransform.sizeDelta = newSize;
	}

	private void UpdatePosition()
	{
		if (selectedRect == null || rectTransform == null) return;
		var size = selectedRect.sizeDelta;
		var scale = selectedRect.localScale;
		newSize = new Vector2(size.x * scale.x + sizeOffset, size.y * scale.y + sizeOffset);
		rectTransform.position = selectedRect.TransformPoint(selectedRect.rect.center);
	}

	private void Update()
	{
		CheckVisibility();
		if (renderer == null || !renderer.enabled) return;
		UpdatePosition();
		if (Time.time < nextUpdateTime || fps <= 0f || rectTransform == null) return;
		nextUpdateTime = Time.time + 1f / fps;
		rectTransform.sizeDelta = newSize + Vector2.one * (1f + 5f * Mathf.Sin(Time.time * 6f));
	}

	private void CheckVisibility()
	{
		if (renderer != null) renderer.enabled = selectedRect != null;
	}

	public void Refresh()
	{
		OnButtonHover(selectedRect);
	}

	public void Hide()
	{
		selectedRect = null;
		CheckVisibility();
	}

	private void OnResolutionChanged(int i)
	{
		Refresh();
	}

	private void OnWindowOpened(Window window)
	{
		Refresh();
	}
}
