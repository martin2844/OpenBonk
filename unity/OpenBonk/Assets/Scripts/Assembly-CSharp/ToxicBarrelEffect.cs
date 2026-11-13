using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class ToxicBarrelEffect : MonoBehaviour
{
	public EffectPlayer effectPlayer;

	private void Awake()
	{
		ItemToxicBarrel.A_OnUse += OnUse;
		ItemBase.A_ItemAdded += Refresh;
		MyPlayer.A_PlayerInventoryInitialized += OnInventory;
	}

	private void OnDestroy()
	{
		ItemToxicBarrel.A_OnUse -= OnUse;
		ItemBase.A_ItemAdded -= Refresh;
		MyPlayer.A_PlayerInventoryInitialized -= OnInventory;
	}

	private void Start()
	{
		Refresh(null);
	}

	private void OnInventory(PlayerInventory inv)
	{
		Refresh(null);
	}

	private void Refresh(ItemBase obj)
	{
		if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || MyPlayer.Instance.inventory.itemInventory == null || effectPlayer == null) return;
		effectPlayer.gameObject.SetActive(MyPlayer.Instance.inventory.itemInventory.GetAmount(EItem.ToxicBarrel) > 0);
	}

	public void OnUse(float radius)
	{
		if (effectPlayer == null) return;
		effectPlayer.transform.localScale = Vector3.one * radius;
		effectPlayer.Play();
	}
}
