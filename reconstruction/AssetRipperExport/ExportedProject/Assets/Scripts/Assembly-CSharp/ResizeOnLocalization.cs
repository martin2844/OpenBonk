using UnityEngine;
using UnityEngine.Localization.Components;

public class ResizeOnLocalization : MonoBehaviour
{
	public Transform rebuildTransform;

	public LocalizeStringEvent localizeStringEvent;

	public TextSizer textSizer;

	public ButtonTextWrapper buttonTextWrapper;

	private bool expectingRebuild;

	private bool expectingRefresh;

	private void Start()
	{
		if (localizeStringEvent != null) localizeStringEvent.StringReference.StringChanged += OnLocalizedStringChanged;
	}

	private void OnDestroy()
	{
		if (localizeStringEvent != null) localizeStringEvent.StringReference.StringChanged -= OnLocalizedStringChanged;
	}

	private void OnEnable()
	{
		if (expectingRebuild) Invoke(nameof(Rebuild), 0f);
		if (expectingRefresh) Invoke(nameof(DelayedRefresh), 0f);
	}

	private void OnLocalizedStringChanged(string updatedString)
	{
		if (gameObject.activeInHierarchy) Invoke(nameof(DelayedRefresh), 0f);
		else expectingRefresh = true;
	}

	private void DelayedRefresh()
	{
		expectingRefresh = false;
		if (textSizer != null) { textSizer.Refresh(); textSizer.Recalculate(); }
		if (buttonTextWrapper != null) buttonTextWrapper.Refresh();
		if (rebuildTransform != null)
		{
			if (gameObject.activeInHierarchy) Invoke(nameof(Rebuild), 0f);
			else expectingRebuild = true;
		}
	}

	private void Rebuild()
	{
		expectingRebuild = false;
		if (rebuildTransform != null) Assets.Scripts.UI.UiUtility.RebuildUi(rebuildTransform);
	}

	private void OnValidate()
	{
		if (localizeStringEvent == null) localizeStringEvent = GetComponentInChildren<LocalizeStringEvent>();
		if (textSizer == null) textSizer = GetComponentInChildren<TextSizer>();
		if (buttonTextWrapper == null) buttonTextWrapper = GetComponentInChildren<ButtonTextWrapper>();
	}
}
