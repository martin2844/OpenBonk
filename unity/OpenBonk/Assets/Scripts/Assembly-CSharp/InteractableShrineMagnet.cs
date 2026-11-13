using UnityEngine;
using Assets.Scripts.UI.Localization;

public class InteractableShrineMagnet : BaseInteractable
{
	public GameObject minimapIcon;

	public GameObject shrineIcon;

	private bool done;

	public GameObject interactFx;

	public static string debugName = "Magnet Shrines";

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		interactFx.SetActive(true);
		PickupManager.Instance.PickupAllXp();
		Destroy(minimapIcon);
		Destroy(shrineIcon);
		Destroy(this);
		return true;
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "SHRINE_MAGNET");
	}

	public override bool ShowInDebug()
	{
		return true;
	}

	public override string GetDebugName()
	{
		return debugName;
	}
}
