using Assets.Scripts.Menu.Shop;
using TMPro;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory.Stats;
using UnityEngine.UI;

public class KillsAndGoldCounter : MonoBehaviour
{
	public TextMeshProUGUI t_gold;

	public TextMeshProUGUI t_kills;

	public TextMeshProUGUI t_silver;

	public TextMeshProUGUI t_chestPrice;

	private string killsString;

	private bool queuedKillsUpdate;

	private bool queuedGoldUpdate;

	private void Start()
	{
		UpdateKillCounter();
		UpdateGoldCounter();
		OnSilverChange(0);
		OnChestPriceIncreased();
	}

	private void Awake()
	{
		ConfigureCounterLayout();
		killsString = TrackStats.GetStatString(EMyStat.kills);
		MyPlayer.A_PlayerInventoryInitialized += OnInventoryInitialized;
		RunStats.A_StatChange += OnRunStatChanged;
		PlayerInventory.A_GoldChange += OnGoldIncrease;
		ProgressionSaveFile.A_SilverChanged += OnSilverChange;
		MoneyUtility.A_ChestPriceIncreased += OnChestPriceIncreased;
		PlayerStatsNew.A_StatUpdate += OnStatsUpdated;
		SpawnPlayerPortal.A_PortalClosed += OnSpawnFinished;
	}

	// The exported horizontal group otherwise lays out the timer and counters
	// using stale child widths, allowing their visible glyphs to overlap.
	private void ConfigureCounterLayout()
	{
		var row = t_kills != null && t_kills.transform.parent != null ? t_kills.transform.parent.parent : null;
		var group = row != null ? row.GetComponent<HorizontalLayoutGroup>() : null;
		if (group == null) return;
		group.childControlWidth = true;
		group.childForceExpandWidth = false;
		if (row is RectTransform rect) LayoutRebuilder.MarkLayoutForRebuild(rect);
	}

	private void OnDestroy()
	{
		MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInitialized;
		RunStats.A_StatChange -= OnRunStatChanged;
		PlayerInventory.A_GoldChange -= OnGoldIncrease;
		ProgressionSaveFile.A_SilverChanged -= OnSilverChange;
		MoneyUtility.A_ChestPriceIncreased -= OnChestPriceIncreased;
		PlayerStatsNew.A_StatUpdate -= OnStatsUpdated;
		SpawnPlayerPortal.A_PortalClosed -= OnSpawnFinished;
	}

	private void OnInventoryInitialized(PlayerInventory inv)
	{
		UpdateGoldCounter();
	}

	private void Update()
	{
		if (queuedKillsUpdate) UpdateKillCounter();
		if (queuedGoldUpdate) UpdateGoldCounter();
	}

	private void OnRunStatChanged(string stat, float value)
	{
		if (stat == killsString) queuedKillsUpdate = true;
	}

	private void OnGoldIncrease(PlayerInventory inv, int amount)
	{
		queuedGoldUpdate = true;
	}

	private void UpdateKillCounter()
	{
		queuedKillsUpdate = false;
		if (t_kills != null) t_kills.text = string.Format("<size=120%><sprite name=skull></size> {0:N0}", RunStats.GetStat(EMyStat.kills));
	}

	private void UpdateGoldCounter()
	{
		queuedGoldUpdate = false;
		if (t_gold != null) t_gold.text = string.Format("<size=110%><sprite name=gold></size> {0:N0}", GameManager.Instance?.GetPlayerInventory()?.goldInt ?? 0);
	}

	private void OnSilverChange(int delta)
	{
		if (t_silver != null) t_silver.text = string.Format("<size=110%><sprite name=silver></size> {0:N0}", SaveManager.Instance?.progression?.silver ?? 0);
	}

	private void OnChestPriceIncreased()
	{
		if (t_chestPrice != null) t_chestPrice.text = string.Format("<size=115%><sprite name=chest></size><sprite name=gold>{0}", MoneyUtility.GetChestPrice());
	}

	private void OnSpawnFinished()
	{
		OnChestPriceIncreased();
		Invoke(nameof(OnChestPriceIncreased), 0.1f);
	}

	private void OnStatsUpdated(EStat stat)
	{
		if (stat == EStat.ChestPriceMultiplier && PlayerStats.HasStats()) OnChestPriceIncreased();
	}
}
