using System;

public class MyButtonUnlock : MyButtonNormal
{
	public UnlockContainer unlockContainer;

	public static Action<UnlockContainer> A_Clicked;

	public override void StartHover()
	{
		isHovering = true;
	}

	public override void StopHover()
	{
		isHovering = false;
	}

	protected override void OnClick()
	{
		if (UnityEngine.Time.time - selectedAtTime >= .15f) A_Clicked?.Invoke(unlockContainer);
	}

	private new void Update()
	{
		base.Update();
		if (isHovering && UnityEngine.Time.time - selectedAtTime >= .05f && MyInputManager.GetButtonDown(MyInputManager.UISubmit) && unlockContainer != null) unlockContainer.ToggleActivation();
	}
}
