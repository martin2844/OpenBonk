using System;
using UnityEngine;
using UnityEngine.UI;

public class MyButtonSettingRes : MyButton
{
	public MaskableGraphic background;

	public Color defaultColor;

	public Color hoverColor;

	public static Action A_Clicked;

	public override void StartHover()
	{
		if (background != null) background.color = hoverColor;
		isHovering = true;
	}

	public override void StopHover()
	{
		if (background != null) background.color = defaultColor;
		isHovering = false;
	}

	protected override void OnClick()
	{
		A_Clicked?.Invoke();
	}
}
