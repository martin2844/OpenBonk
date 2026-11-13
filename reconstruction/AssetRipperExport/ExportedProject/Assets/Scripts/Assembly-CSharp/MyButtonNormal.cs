using UnityEngine;
using UnityEngine.UI;

public class MyButtonNormal : MyButton
{
	public MaskableGraphic background;

	public Color defaultColor;

	public Color hoverColor;

	private bool colorInited;

	public override void StartHover()
	{
		SetColor(hoverColor);
		background.color = hoverColor;
		isHovering = true;
	}

	public override void StopHover()
	{
		SetColor(defaultColor);
		background.color = defaultColor;
		isHovering = false;
	}

	private void SetColor(Color c)
	{
		if (!colorInited)
		{
			defaultColor = background.color;
			hoverColor = Color.Lerp(defaultColor, Color.black, .2f);
			colorInited = true;
		}
		background.color = c;
	}

	protected override void OnClick()
	{
	}
}
