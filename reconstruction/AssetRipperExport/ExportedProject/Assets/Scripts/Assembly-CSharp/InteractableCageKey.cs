using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class InteractableCageKey : BaseInteractable
{
	public GameObject fx;

	private bool done;

	public EItem eItem;

	public InteractableCageKey()
	{
		showOutline = true;
	}

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		MyPlayer.Instance.inventory.itemInventory.AddItem(eItem);
		fx.transform.SetParent(null);
		fx.SetActive(true);
		Destroy(gameObject);
		return true;
	}

	public override string GetInteractString()
	{
		return DataManager.Instance.GetItem(eItem).GetName();
	}
}
