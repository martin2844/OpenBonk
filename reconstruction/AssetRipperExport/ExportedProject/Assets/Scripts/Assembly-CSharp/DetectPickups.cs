using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public class DetectPickups : MonoBehaviour
{
	public SphereCollider collider;

	private void Start()
	{
		if (GameManager.Instance != null && GameManager.Instance.GetPlayerInventory() != null)
			UpdateRadius();
		else
			MyPlayer.A_PlayerInventoryInitialized += OnInventoryInit;
		PlayerStatsNew.A_StatUpdate += OnStatUpdated;
	}

	private void OnInventoryInit(PlayerInventory obj)
	{
		UpdateRadius();
	}

	private void OnDestroy()
	{
		MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInit;
		PlayerStatsNew.A_StatUpdate -= OnStatUpdated;
	}

	private void OnStatUpdated(EStat stat)
	{
		if (stat == EStat.PickupRange) UpdateRadius();
	}

	private void UpdateRadius()
	{
		if (collider != null && PlayerStats.HasStats())
			collider.radius = PlayerStats.GetStat(EStat.PickupRange);
	}
}
