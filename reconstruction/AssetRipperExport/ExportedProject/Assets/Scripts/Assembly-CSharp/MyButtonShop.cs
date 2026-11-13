using System;

public class MyButtonShop : MyButtonNormal
{
	public ShopContainer shopContainer;

	public static Action<ShopContainer> A_Clicked;

	public static Action<ShopContainer> A_Select;

	public override void StartHover()
	{
		if (shopContainer != null && shopContainer.data != null) A_Select?.Invoke(shopContainer);
	}

	public override void StopHover()
	{
	}

	protected override void OnClick()
	{
		if (shopContainer != null && shopContainer.data != null && selectedAtTime <= UnityEngine.Time.time - .15f) A_Clicked?.Invoke(shopContainer);
	}
}
