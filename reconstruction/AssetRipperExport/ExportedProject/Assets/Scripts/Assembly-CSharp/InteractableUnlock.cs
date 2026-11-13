using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEngine;
using UnityEngine.UI;

public class InteractableUnlock : BaseInteractable
{
	public UnlockableBase unlock;

	public GameObject fx;

	public RawImage icon;

	private bool done;

	public bool useUnlock;

	private new void Start()
	{
		base.Start();
		if (icon != null && unlock != null) icon.texture = unlock.GetIcon();
	}

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		if (useUnlock && unlock != null && unlock.GetUnlockRequirement() != null)
			MyAchievements.TryUnlock(unlock.GetUnlockRequirement().internalName);
		var player = MyPlayer.Instance;
		if (player != null && unlock is WeaponData weapon && player.inventory?.weaponInventory != null && DataManager.Instance != null)
			player.inventory.weaponInventory.AddWeapon(DataManager.Instance.GetWeapon(weapon.eWeapon), null);
		else if (player != null && unlock is TomeData tome && player.inventory?.tomeInventory != null)
			player.inventory.tomeInventory.AddTome(tome, tome.GetUpgradeOffer(0), 0);
		else if (player != null && unlock is ItemData item && player.inventory?.itemInventory != null)
			player.inventory.itemInventory.AddItem(item.eItem);
		else if (player != null && unlock is HatData hat && player.playerRenderer != null)
			player.playerRenderer.SetHat(hat);
		if (fx != null) { fx.transform.SetParent(null); fx.SetActive(true); }
		Destroy(gameObject);
		return true;
	}

	public override string GetInteractString()
	{
		return unlock != null ? unlock.GetName() : string.Empty;
	}
}
