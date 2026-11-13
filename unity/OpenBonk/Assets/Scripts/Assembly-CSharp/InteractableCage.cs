using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class InteractableCage : BaseInteractable
{
	public GameObject fx;

	private bool done;

	public AudioSource audioSource;

	public GameObject monke;

	public LocalizedString localizedString;

	public MeshRenderer meshRenderer;

	public BoxCollider collider;

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		fx.transform.SetParent(null);
		fx.SetActive(true);
		collider.enabled = false;
		meshRenderer.enabled = false;
		audioSource.Stop();
		audioSource.enabled = false;
		monke.SetActive(true);
		MyAchievements.TryUnlock("a_monke");
		return true;
	}

	public override bool CanInteract()
	{
		return HasKey();
	}

	public override string GetInteractString()
	{
		return localizedString.GetLocalizedString();
	}

	private bool HasKey()
	{
		return MyPlayer.Instance != null && MyPlayer.Instance.inventory?.itemInventory != null &&
			MyPlayer.Instance.inventory.itemInventory.GetAmount(EItem.CageKey) > 0;
	}
}
