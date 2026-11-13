using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;

public class ServerFeed : MonoBehaviour
{
	public GameObject serverFeedPrefab;

	public Transform content;

	private int numMaxPrefabs = 13;

	private readonly List<ServerFeedPrefab> activePrefabs = new List<ServerFeedPrefab>();

	private void Awake()
	{
		ItemInventory.A_ItemAdded += OnItemAdded;
	}

	private void OnDestroy()
	{
		ItemInventory.A_ItemAdded -= OnItemAdded;
	}

	public void SetFeed(string text, float duration, Texture icon = null)
	{
		ServerFeedPrefab entry;
		if (activePrefabs.Count < numMaxPrefabs)
		{
			entry = Instantiate(serverFeedPrefab, content).GetComponent<ServerFeedPrefab>();
			activePrefabs.Add(entry);
		}
		else
		{
			entry = activePrefabs[0];
			activePrefabs.RemoveAt(0);
			activePrefabs.Add(entry);
			entry.transform.SetAsLastSibling();
			entry.StopAllCoroutines();
		}
		entry.SetFeed(text, duration, TimeoutPrefab, icon);
	}

	private void TimeoutPrefab(ServerFeedPrefab prefab)
	{
	}

	private void OnItemAdded(EItem eItem)
	{
		if (SaveManager.Instance.config.cfGameSettings.show_item_feed == 0) return;
		var item = DataManager.Instance.GetItem(eItem);
		SetFeed("+1 " + item.GetName(), 5f, item.GetIcon());
	}
}
