using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonNavigationBackdropAndText : MonoBehaviour, IButton, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	public MaskableGraphic overlay;

	public MaskableGraphic overlay_disabled;

	public MaskableGraphic text;

	public GameObject associatedContent;

	public bool isTabSelectable;

	[Range(0.5f, 2f)]
	public float overlayMultiplier;

	private Vector3 desiredScale;

	private Color textDefaultColor;

	private Color textSelectedColor;

	private Color c_overlayDefault;

	private Color c_overlayHover;

	private Color c_overlaySelected;

	private bool selected;

	private Button button;

	public void Select()
	{
	}

	public void Deselect(IButton newButton)
	{
	}

	public Button GetButton()
	{
		return null;
	}

	public GameObject GetAssociatedContent()
	{
		return null;
	}

	public bool IsTabSelectable()
	{
		return false;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
	}

	public void OnPointerExit(PointerEventData eventData)
	{
	}

	public void OnPointerClick(PointerEventData eventData)
	{
	}

	private void Update()
	{
	}

	public void Enable()
	{
	}

	public void Disable()
	{
	}

	public bool IsEnabled()
	{
		return false;
	}
}
