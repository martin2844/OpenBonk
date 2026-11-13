using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

public class DeathStatsUi : MonoBehaviour
{
	public TextMeshProUGUI t_stats;

	public TextMeshProUGUI t_gold;

	public TextMeshProUGUI t_unlocks;

	public TextMeshProUGUI t_silver;

	public TextMeshProUGUI t_characterName;

	public RawImage i_character;

	public Transform weaponsParent;

	public Transform tomesParent;

	public Transform itemsParent;

	public GameObject inventoryItemPrefab;

	private void Start()
	{
		PlayerInventory inventory = GameManager.Instance.GetPlayerInventory();
		int kills = RunStats.GetStat(EMyStat.kills);
		int minutes = Mathf.FloorToInt(MyTime.runTimer / 60f);
		int seconds = Mathf.FloorToInt(MyTime.runTimer % 60f);
		string level = LocalizationUtility.GetLocalizedString("Game_HUD", "LEVEL", new Dictionary<string, string> { ["level"] = inventory.GetCharacterLevel().ToString() });
		if (t_stats != null) t_stats.text = string.Format("{0}: {1:N0}\n", LocalizationUtility.GetLocalizedString("Game_RoundOver", "SUMMARY_KILLS"), kills)
			+ LocalizationUtility.GetLocalizedString("Game_RoundOver", "SUMMARY_TIME") + ": " + string.Format("{0}:{1:00}", minutes, seconds) + "\n" + level + "\n";
		if (t_characterName != null) t_characterName.text = inventory.characterData.GetName();
		if (i_character != null) i_character.texture = inventory.characterData.GetIcon();
		if (t_gold != null) t_gold.text = string.Format("<sprite name=\"gold\"> +{0} {1}!", RunStats.GetStat(EMyStat.goldEarned), LocalizationUtility.GetLocalizedString("Other", "GOLD"));
		if (t_silver != null) t_silver.text = string.Format("<sprite name=\"silver\"> +{0} {1}!", RunStats.GetStat(EMyStat.silverEarned), LocalizationUtility.GetLocalizedString("Other", "SILVER"));
		int unlocks = RunStats.achievements.Count;
		if (t_unlocks != null) t_unlocks.text = unlocks > 0 ? string.Format("<sprite name=\"unlock\" tint> {0} {1}!", unlocks, LocalizationUtility.GetLocalizedString("Main Menu", "UNLOCKS")) : "";
		ShowInventory();
	}

	private void ShowInventory()
	{
		if (inventoryItemPrefab == null || DataManager.Instance == null) return;
		PlayerInventory inventory = GameManager.Instance.GetPlayerInventory();
		foreach (var weapon in inventory.weaponInventory.weapons.Values)
			AddInventoryEntry(weaponsParent, weapon.weaponData);
		foreach (var tome in inventory.tomeInventory.tomeLevels.Keys)
			AddInventoryEntry(tomesParent, DataManager.Instance.GetTome(tome));
		if (inventory.tomeInventory.tomeLevels.Count == 0 && tomesParent != null)
			tomesParent.gameObject.SetActive(false);
		foreach (var item in inventory.itemInventory.items.Keys)
			AddInventoryEntry(itemsParent, DataManager.Instance.GetItem(item));
	}

	private void AddInventoryEntry(Transform parent, Assets.Scripts.Saves___Serialization.Progression.Achievements.UnlockableBase item)
	{
		if (parent == null || item == null) return;
		GameObject entry = Instantiate(inventoryItemPrefab, parent);
		var ui = entry.GetComponent<InventoryItemPrefabUI>();
		if (ui != null) ui.SetItem(item);
		entry.SetActive(true);
	}
}
