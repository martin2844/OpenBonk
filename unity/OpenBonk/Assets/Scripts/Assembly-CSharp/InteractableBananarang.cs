using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.UI.Localization;

public class InteractableBananarang : BaseInteractable
{
	public GameObject fx;

	private bool done;

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		MyAchievements.TryUnlock("a_bananarang");
		MyPlayer.Instance.inventory.weaponInventory.AddWeapon(DataManager.Instance.GetWeapon(EWeapon.Bananarang), null);
		fx.transform.SetParent(null);
		fx.SetActive(true);
		Destroy(gameObject);
		return true;
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "BANANARANG");
	}
}
