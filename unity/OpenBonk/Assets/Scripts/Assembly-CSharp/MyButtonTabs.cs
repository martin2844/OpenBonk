using UnityEngine;
using UnityEngine.UI;

public class MyButtonTabs : MyButton
{
	public MaskableGraphic background;

	public MaskableGraphic text;

	public GameObject associatedContent;

	public Color selectedColor;

	public Color defaultColor;

	private bool selected;

	private void Start()
	{
		if (!selected) Deselect(this);
	}

	public void Select()
	{
		if (associatedContent != null) associatedContent.SetActive(true);
		selected = true;
		background.color = selectedColor;
	}

	public void Deselect(MyButtonTabs newButton)
	{
		if (associatedContent != null && newButton.associatedContent != null) associatedContent.SetActive(false);
		selected = false;
		background.color = defaultColor;
		isHovering = false;
	}

	public override void StartHover()
	{
		if (selected) return;
		background.color = Color.Lerp(defaultColor, selectedColor, 0.5f);
		isHovering = true;
	}

	public override void StopHover()
	{
		if (!selected) background.color = defaultColor;
		isHovering = false;
	}

	protected override void OnClick()
	{
	}
}
