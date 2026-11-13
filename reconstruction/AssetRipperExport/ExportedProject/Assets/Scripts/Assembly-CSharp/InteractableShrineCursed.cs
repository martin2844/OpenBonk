using UnityEngine;
using Assets.Scripts.UI.Localization;

public class InteractableShrineCursed : BaseInteractable
{
	public GameObject minimapIcon;

	public GameObject alertIcon;

	public bool done;

	public GameObject impactFx;

	public GameObject constantFx;

	public static string debugName = "Boss Curses";

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		impactFx.SetActive(true);
		constantFx.SetActive(true);
		GameManager.Instance.bossCurses++;
		Destroy(minimapIcon);
		Destroy(alertIcon);
		Destroy(this);
		return true;
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "SHRINE_CURSED");
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
