using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using System;
using System.Collections.Generic;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.UI.Mouse;
using Assets.Scripts.Utility;

public class UnlocksExtraInformation : MonoBehaviour
{
	public TextMeshProUGUI t_title;

	public TextMeshProUGUI t_info;

	public TextMeshProUGUI t_infoNumbers;

	public LocalizedString titleStringWeapons;

	public LocalizedString titleStringCharacters;

	private PlayerInventory dummyInventory;

	public void TrySetInfo(UnlockableBase unlockable)
	{
		gameObject.SetActive(true);
		if (t_infoNumbers != null) t_infoNumbers.gameObject.SetActive(false);
		if (unlockable is WeaponData weapon) SetInfoWeapon(weapon);
		else if (unlockable is CharacterData character) SetCharacterInformation(character);
		else gameObject.SetActive(false);
	}

	public void SetInfoWeapon(WeaponData weaponData)
	{
		if (weaponData == null) return;
		var names = new List<string> { LocalizationUtility.GetStatName(EStat.AttackSpeed) };
		if (weaponData.upgradeData != null && weaponData.upgradeData.upgradeModifiers != null)
			foreach (var modifier in weaponData.upgradeData.upgradeModifiers)
				if (modifier != null) names.Add(LocalizationUtility.GetStatName(modifier.stat));
		names.Sort((a, b) => string.Compare(a, b, StringComparison.Ordinal));
		if (t_title != null) t_title.text = titleStringWeapons != null && !titleStringWeapons.IsEmpty ? titleStringWeapons.GetLocalizedString() : string.Empty;
		if (t_info != null) t_info.text = string.Concat(names.ConvertAll(name => "- " + name + "\n"));
	}

	public void SetCharacterInformation(CharacterData characterData)
	{
		if (characterData == null) { gameObject.SetActive(false); return; }
		if (t_infoNumbers != null) { t_infoNumbers.gameObject.SetActive(true); t_infoNumbers.text = string.Empty; }
		if (t_info != null) t_info.text = string.Empty;
		if (dummyInventory != null) { dummyInventory.Cleanup(); dummyInventory = null; }
		dummyInventory = new PlayerInventory(characterData, true);
		var stats = new List<EStat> { EStat.MaxHealth, EStat.MoveSpeedMultiplier, EStat.JumpHeight, EStat.PickupRange };
		var negative = new HashSet<EStat>();
		if (characterData.statModifiers != null)
		{
			foreach (var modifier in characterData.statModifiers)
			{
				if (modifier == null) continue;
				if (ChallengesUi.IsNegativeModifier(modifier)) negative.Add(modifier.stat);
				if (!stats.Contains(modifier.stat)) stats.Add(modifier.stat);
			}
		}
		stats.Remove(EStat.FallDamageReduction);
		if (t_title != null) t_title.text = titleStringCharacters != null && !titleStringCharacters.IsEmpty ? titleStringCharacters.GetLocalizedString() : string.Empty;
		foreach (var stat in stats)
		{
			float raw = dummyInventory.playerStats.GetRawStat(stat);
			string formatted = StatsUi.FormatStat(stat, raw) ?? string.Empty;
			if (raw != PlayerStatsNew.GetBaseValue(stat))
			{
				var modifier = characterData.statModifiers?.Find(m => m != null && m.stat == stat);
				if (modifier != null)
				{
					string color = negative.Contains(stat) ? MyColorUtility.negativeColorString : MyColorUtility.positiveColorString;
					string delta = StatUtility.GetModificationString(modifier.modifyType, modifier.stat, modifier.modification, false, true);
					formatted += " <color=" + color + ">(" + StatUtility.EncapsulateNumber(delta, color) + ")</color>";
				}
			}
			if (t_info != null) t_info.text += Tooltip.GetTooltipString(stat, "#FFFFFF") + ": \n";
			if (t_infoNumbers != null) t_infoNumbers.text += formatted + "\n";
		}
	}

	private void OnDestroy()
	{
		if (dummyInventory != null) { dummyInventory.Cleanup(); dummyInventory = null; }
	}
}
