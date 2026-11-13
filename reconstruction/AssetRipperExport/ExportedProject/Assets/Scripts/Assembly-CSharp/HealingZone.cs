using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Utility;

public class HealingZone : MonoBehaviour
{
	private float healInterval = .5f;

	private float nextHealTime;

	private float radius;

	private float healPerInterval;

	private float defaultHealingPerInterval = 1f;

	private void Awake()
	{
		ItemInventory.A_ItemAdded += OnItemChanged;
		ItemInventory.A_ItemRemoved += OnItemChanged;
	}

	private void OnDestroy()
	{
		ItemInventory.A_ItemAdded -= OnItemChanged;
		ItemInventory.A_ItemRemoved -= OnItemChanged;
	}

	private void Start()
	{
		RefreshStats();
		if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 2f, GameManager.Instance.whatIsGround))
			transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
	}

	private void FixedUpdate()
	{
		if (MyTime.time < nextHealTime) return;
		nextHealTime = MyTime.time + healInterval;
		if (Vector3.Distance(MyPlayer.Instance.transform.position, transform.position) <= radius)
			MyPlayer.Instance.inventory.playerHealth.Heal(healPerInterval, true);
	}

	private void OnItemChanged(EItem item)
	{
		if (item == EItem.Beacon) RefreshStats();
	}

	private void OnItemChanged(EItem arg1, bool arg2)
	{
		if (arg1 == EItem.Beacon) RefreshStats();
	}

	private void RefreshStats()
	{
		var items = MyPlayer.Instance.inventory.itemInventory;
		if (items.GetAmount(EItem.Beacon) <= 0) { gameObject.SetActive(false); return; }
		radius = 5f;
		healPerInterval = 1f;
		if (items.GetItem(EItem.Beacon) is ItemBeacon beacon)
		{
			radius = beacon.GetRadius();
			healPerInterval = beacon.GetHealingPerInterval();
		}
		transform.localScale = Vector3.one * radius;
	}

	private float GetRadius()
	{
		return radius;
	}

	private float GetHealAmount()
	{
		return healPerInterval;
	}
}
