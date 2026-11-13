using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MyButtonHat : MyButton, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public HatSelectionPopupWindow hatSelectionPopupWindow;

	public RawImage background;

	public Color defaultColor;

	public Color selectedColor;

	public RawImage i_hatIcon;

	public Texture noHatTexture;

	public HatData hatData;

	public override void StartHover()
	{
		isHovering = true;
		hatSelectionPopupWindow.HoverButton(hatData);
	}

	public override void StopHover()
	{
		isHovering = false;
	}

	protected override void OnClick()
	{
		hatSelectionPopupWindow.ClickButton(hatData);
	}

	public void Set(HatData hatData)
	{
		this.hatData = hatData;
		i_hatIcon.texture = hatData != null ? hatData.icon : noHatTexture;
	}

	public void SetSelected(bool selected)
	{
		background.color = selected ? selectedColor : defaultColor;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		StartHover();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		StopHover();
	}
}
