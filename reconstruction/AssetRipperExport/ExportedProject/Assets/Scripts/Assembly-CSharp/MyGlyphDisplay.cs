#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyGlyphDisplay : MonoBehaviour
{
	public Image i_glyph;

	public TextMeshProUGUI t_text;

	private RectTransform rectTransform;

	public GameObject hoverOverlay;

	public bool autoHeight;

	public bool autoWidth;

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	private ActionElementMap elementMap;
#endif
	private string nativeAction;
#if UNITY_ANDROID && !UNITY_EDITOR
	private bool lastTouchMode;
	private string lastLocale;
	private static bool TouchMode => SaveManager.Instance?.config?.cfControlSettings != null && SaveManager.Instance.config.cfControlSettings.touch_controls != 0;
	private static string CurrentLocale => UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
	private void Update()
	{
		if (!string.IsNullOrEmpty(nativeAction) && (lastTouchMode != TouchMode || lastLocale != CurrentLocale)) RefreshNativeAction();
	}
#endif

	private void OnEnable()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		KeyListener.A_MapChanged += RefreshNativeAction;
		RefreshNativeAction();
#endif
	}

	private void OnDisable()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		KeyListener.A_MapChanged -= RefreshNativeAction;
#endif
	}

	private void RefreshNativeAction()
	{
		if (!string.IsNullOrEmpty(nativeAction)) ShowNativeAction(nativeAction);
	}

	public void Hover(bool isHovering)
	{
		if (hoverOverlay != null) hoverOverlay.SetActive(isHovering);
	}

	public void SetAction(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		nativeAction = action;
		ShowNativeAction(action);
#endif
	}

	// Rewired's desktop controller maps are not available in the Android build.
	// Show the bindings actually used by AndroidNativeInput until the Android
	// input settings can supply controller-specific glyphs.
	internal void ShowNativeAction(string action)
	{
		string label = null;
#if UNITY_ANDROID && !UNITY_EDITOR
		lastTouchMode = TouchMode;
		lastLocale = CurrentLocale;
		string touchLabel = lastTouchMode ? AndroidControlText.TouchAction(action) : null;
		if (touchLabel != null) label = "[" + touchLabel + "]";
		else
#endif
		if (string.Equals(action, "Interact", System.StringComparison.OrdinalIgnoreCase)) label = AndroidControllerBindings.DisplayForAction(action) + " / E";
		else if (string.Equals(action, "Jump", System.StringComparison.OrdinalIgnoreCase) || string.Equals(action, "Jump Bhop", System.StringComparison.OrdinalIgnoreCase)) label = AndroidControllerBindings.DisplayForAction(action) + " / Space";
		else if (string.Equals(action, "Slide", System.StringComparison.OrdinalIgnoreCase)) label = AndroidControllerBindings.DisplayForAction(action) + " / Ctrl";
		else if (string.Equals(action, "Aim", System.StringComparison.OrdinalIgnoreCase)) label = AndroidControllerBindings.DisplayForAction(action) + " / Right Mouse";
		else if (string.Equals(action, "MapOverlay", System.StringComparison.OrdinalIgnoreCase)) label = AndroidControllerBindings.DisplayForAction(action) + " / Tab";
		else if (string.Equals(action, "QuickReset", System.StringComparison.OrdinalIgnoreCase)) label = AndroidControllerBindings.DisplayForAction(action) + " / R";
		if (i_glyph != null) { i_glyph.enabled = false; i_glyph.gameObject.SetActive(false); }
		if (t_text != null)
		{
			t_text.gameObject.SetActive(label != null);
			t_text.enabled = label != null;
			t_text.text = label ?? string.Empty;
			if (label != null)
			{
				float width = t_text.GetPreferredValues(label).x + 30f;
				t_text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
				var root = transform as RectTransform;
				var fitter = GetComponent<ContentSizeFitter>();
				if (fitter != null) fitter.enabled = false;
				if (root != null)
				{
					root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
					LayoutRebuilder.MarkLayoutForRebuild(root);
					if (root.parent is RectTransform parent) LayoutRebuilder.MarkLayoutForRebuild(parent);
				}
			}
		}
	}

#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
	public void Set(ActionElementMap elementMap)
	{
	}
#endif

	private void RefreshGlyphSize()
	{
	}
}
