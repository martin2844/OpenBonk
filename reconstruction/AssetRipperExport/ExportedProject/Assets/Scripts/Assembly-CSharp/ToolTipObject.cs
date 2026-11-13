using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipObject : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[TextArea]
	public string text;

	private float hoverTimeForShow = 0.25f;

	private float startedHoveringTime;

	private bool hovering;

	private bool showed;

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (string.IsNullOrWhiteSpace(text) || hovering) return;
		hovering = true;
		showed = false;
		startedHoveringTime = Time.time;
	}

	private void Update()
	{
		if (!hovering || showed || Time.time - startedHoveringTime <= hoverTimeForShow) return;
		showed = true;
		if (ToolTip.Instance != null) ToolTip.Instance.SetTip(text);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		CloseTooltip();
	}

	private void CloseTooltip()
	{
		if (!hovering && !showed) return;
		hovering = false;
		showed = false;
		if (ToolTip.Instance != null) ToolTip.Instance.HideTip();
	}

	public void OnDisable()
	{
		CloseTooltip();
	}
}
