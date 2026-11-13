using TMPro;
using UnityEngine;
using Assets.Scripts.UI.Mouse;

public class ToolTipMouse : MonoBehaviour
{
	public GameObject parent;

	public TextMeshProUGUI t_tooltip;

	private bool opening;

	private float lerpValue;

	private float scaleTime = .1f;

	private void Awake()
	{
		ToolTipsText.A_OpenTooltip += OpenTooltip;
		ToolTipsText.A_CloseTooltip += CloseTooltip;
		if (parent != null) parent.transform.localScale = Vector3.zero;
	}

	private void OnDestroy()
	{
		ToolTipsText.A_OpenTooltip -= OpenTooltip;
		ToolTipsText.A_CloseTooltip -= CloseTooltip;
	}

	private void OpenTooltip(string linkID, Vector2 position)
	{
		lerpValue = -1f / scaleTime;
		if (parent == null) return;
		parent.transform.localScale = Vector3.zero;
		if (t_tooltip != null) t_tooltip.text = Tooltip.GetIdInfo(linkID);
		parent.SetActive(true);
		opening = true;
		parent.transform.localScale = Vector3.zero;
		parent.transform.position = position;
	}

	private void CloseTooltip(string linkID)
	{
		opening = false;
	}

	private void Update()
	{
		lerpValue = Mathf.Clamp(lerpValue + (opening ? 1f : -1f) * Time.deltaTime / scaleTime, -1f, 1f);
		if (parent == null) return;
		parent.transform.localScale = Vector3.one * Mathf.Clamp01(lerpValue);
		if (!opening && lerpValue <= 0f) parent.SetActive(false);
	}
}
