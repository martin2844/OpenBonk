using System;
using UnityEngine;

public class MyButtonToggle : MyButton
{
	public Action<bool> A_Toggled;

	public GameObject toggleIcon;

	public void Set(bool on)
	{
		toggleIcon.SetActive(on);
	}

	public bool IsOn()
	{
		return toggleIcon.activeInHierarchy;
	}

	public override void StartHover()
	{
	}

	public override void StopHover()
	{
	}

	protected override void OnClick()
	{
		toggleIcon.SetActive(!toggleIcon.activeInHierarchy);
		A_Toggled?.Invoke(toggleIcon.activeInHierarchy);
	}
}
