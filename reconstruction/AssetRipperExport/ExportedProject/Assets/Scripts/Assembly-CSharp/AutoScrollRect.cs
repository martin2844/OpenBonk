using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AutoScrollRect : MonoBehaviour
{
	public ScrollRect scrollRect;

	public RectTransform viewport;

	public RectTransform content;

	public GameObject lastSelected;

	private void Update()
	{
		if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return;
		GameObject selected = EventSystem.current.currentSelectedGameObject;
		if (!selected.transform.IsChildOf(content) || selected == lastSelected) return;
		EnsureVisible(selected.GetComponent<RectTransform>());
		lastSelected = selected;
	}

	private void EnsureVisible(RectTransform target, float padding = 20f)
	{
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);
		Vector3[] viewportCorners = new Vector3[4];
		Vector3[] targetCorners = new Vector3[4];
		viewport.GetWorldCorners(viewportCorners);
		target.GetWorldCorners(targetCorners);
		float viewportTop = viewport.InverseTransformPoint(viewportCorners[1]).y;
		float viewportBottom = viewport.InverseTransformPoint(viewportCorners[0]).y;
		float targetTop = viewport.InverseTransformPoint(targetCorners[1]).y;
		float targetBottom = viewport.InverseTransformPoint(targetCorners[0]).y;
		float offset = 0f;
		if (targetTop > viewportTop - padding) offset = targetTop - (viewportTop - padding);
		else if (targetBottom < viewportBottom + padding) offset = targetBottom - (viewportBottom + padding);
		content.anchoredPosition -= new Vector2(0f, offset);
		if (SelectionArrow.Instance != null) SelectionArrow.Instance.Refresh();
	}

	public void ManualRefresh()
	{
		if (lastSelected != null) EnsureVisible(lastSelected.GetComponent<RectTransform>());
	}
}
