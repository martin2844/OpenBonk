using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyButtonMap : MyButton
{
	public GameObject hoverOverlay;

	public TextMeshProUGUI t_name;

	public RawImage i_icon;

	public MapData mapData;

	public static Action<MyButtonMap> A_Confirm;

	public static Action<MyButtonMap> A_Select;

	public void SetMap(MapData data)
	{
	}

	public override void StartHover()
	{
	}

	public override void StopHover()
	{
	}

	protected override void OnClick()
	{
	}

	private new void Update()
	{
	}
}
