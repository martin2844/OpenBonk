using System.Collections.Generic;
using System;
using Assets.Scripts.UI;
using UnityEngine;

public class ShrineInformationUi : MonoBehaviour
{
	public Transform parent;

	public GameObject prefab;

	private List<ShrineInformationPrefab> entries;

	private int ticksUntilInit = 5;

	private void Awake()
	{
		CurrentSettings.A_SettingUpdated += OnSettingUpdate;
		MapGenerationController.A_GenerationComplete += OnMapGenerated;
		InteractablesStatus.A_InteractableUsed += OnInteractableStatusUpdate;
		InteractablesStatus.A_InteractableSpawned += OnInteractableSpawned;
		GameManager.A_DungeonStarted += Refresh;
		GameManager.A_DungeonEnded += Refresh;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdate;
		MapGenerationController.A_GenerationComplete -= OnMapGenerated;
		InteractablesStatus.A_InteractableUsed -= OnInteractableStatusUpdate;
		InteractablesStatus.A_InteractableSpawned -= OnInteractableSpawned;
		GameManager.A_DungeonStarted -= Refresh;
		GameManager.A_DungeonEnded -= Refresh;
	}

	private void Start()
	{
		CheckVisible();
	}

	private void OnInteractableSpawned(string debugName)
	{
		ticksUntilInit = 5;
	}

	private void FixedUpdate()
	{
		if (ticksUntilInit > 0) ticksUntilInit--;
		if (ticksUntilInit == 0) { ticksUntilInit = -1; Init(); }
	}

	private void OnMapGenerated()
	{
		CheckVisible();
		Refresh();
	}

	private void Init()
	{
		if (entries != null) foreach (var entry in entries) if (entry != null) Destroy(entry.gameObject);
		if (prefab == null) return;
		prefab.SetActive(true);
		entries = new List<ShrineInformationPrefab>();
		var sorted = new List<KeyValuePair<string, InteractablesStatus.InteractableStatusContainer>>(InteractablesStatus.interactablesByName);
		sorted.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
		foreach (var pair in sorted)
		{
			var instance = Instantiate(prefab, prefab.transform.parent);
			var row = instance.GetComponent<ShrineInformationPrefab>();
			if (row == null) { Destroy(instance); continue; }
			row.Set(pair.Value, pair.Key);
			entries.Add(row);
		}
		prefab.SetActive(false);
		Refresh();
	}

	private void Rebuild()
	{
		UiUtility.RebuildUi(transform);
	}

	private void OnInteractableStatusUpdate(string name)
	{
		Refresh();
	}

	private void Refresh()
	{
		if (parent == null || !parent.gameObject.activeSelf || entries == null) return;
		foreach (var entry in entries) if (entry != null) entry.Refresh();
		Rebuild();
	}

	private void CheckVisible()
	{
		if (parent == null || SaveManager.Instance?.config?.cfGameSettings == null) return;
		parent.gameObject.SetActive(SaveManager.Instance.config.cfGameSettings.debug_shrines == 1);
		if (parent.gameObject.activeSelf) Refresh();
	}

	private void OnSettingUpdate(string name, object oldVal, object newVal)
	{
		if (name == "debug_shrines") CheckVisible();
	}
}
