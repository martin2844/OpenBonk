using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI;
using UnityEngine;

public class StatsUi : MonoBehaviour
{
	public Transform rootTransformToRefresh;
	public GameObject entryPrefab;
	public GameObject spacerPrefab;
	private List<StatEntry> entries;
	private int[] spacers = { 7, 13, 19, 23 };
	private List<EStat> statsToShow = new List<EStat> {
		EStat.MaxHealth, EStat.HealthRegen, EStat.Overheal, EStat.Shield, EStat.Armor, EStat.Evasion,
		EStat.Lifesteal, EStat.Thorns, EStat.DamageMultiplier, EStat.CritChance, EStat.CritDamage,
		EStat.AttackSpeed, EStat.Projectiles, EStat.ProjectileBounces, EStat.SizeMultiplier,
		EStat.ProjectileSpeedMultiplier, EStat.DurationMultiplier, EStat.EliteDamageMultiplier,
		EStat.KnockbackMultiplier, EStat.MoveSpeedMultiplier, EStat.ExtraJumps, EStat.JumpHeight,
		EStat.Luck, EStat.Difficulty, EStat.PickupRange, EStat.XpIncreaseMultiplier,
		EStat.GoldIncreaseMultiplier, EStat.SilverIncreaseMultiplier, EStat.EliteSpawnIncrease,
		EStat.PowerupBoostMultiplier, EStat.PowerupChance
	};

	private void Awake() { PlayerStatsNew.A_StatUpdate += OnStatUpdate; }
	private void OnDestroy() { PlayerStatsNew.A_StatUpdate -= OnStatUpdate; }
	private void OnEnable() { Refresh(); }
	private void OnStatUpdate(EStat stat) { if (gameObject.activeInHierarchy) Refresh(); }

	private void TryInit()
	{
		if (entries != null) return;
		entries = new List<StatEntry>();
		int spacerIndex = 0;
		for (int i = 0; i < statsToShow.Count; i++)
		{
			entries.Add(Instantiate(entryPrefab, entryPrefab.transform.parent).GetComponent<StatEntry>());
			if (spacerIndex < spacers.Length && i >= spacers[spacerIndex])
			{
				Instantiate(spacerPrefab, entryPrefab.transform.parent);
				spacerIndex++;
			}
		}
		entryPrefab.SetActive(false);
		spacerPrefab.SetActive(false);
	}

	private void Refresh()
	{
		if (!PlayerStats.HasStats()) return;
		TryInit();
		for (int i = 0; i < statsToShow.Count; i++) entries[i].Set(statsToShow[i]);
		StartCoroutine(DelayedRebuild());
	}

	private IEnumerator DelayedRebuild()
	{
		yield return null;
		if (rootTransformToRefresh != null) UiUtility.RebuildUi(rootTransformToRefresh);
	}

	public static string FormatStat(EStat stat, float value, bool preciseExternalPassive = false)
	{
		if (preciseExternalPassive && (stat == EStat.DurationMultiplier || stat == EStat.PowerupChance))
			return string.Format("{0:N2}x", value);
		switch (stat)
		{
			case EStat.Armor: case EStat.Evasion: case EStat.AttackSpeed: case EStat.Lifesteal:
			case EStat.CritChance: case EStat.FallDamageReduction: case EStat.Luck: case EStat.Difficulty:
				return string.Format("{0:N0}%", value * 100f);
			case EStat.SizeMultiplier: case EStat.DurationMultiplier: case EStat.ProjectileSpeedMultiplier:
			case EStat.DamageMultiplier: case EStat.EliteDamageMultiplier: case EStat.KnockbackMultiplier:
			case EStat.GoldIncreaseMultiplier: case EStat.EliteSpawnIncrease: case EStat.PowerupBoostMultiplier:
			case EStat.PowerupChance: case EStat.SilverIncreaseMultiplier:
				return string.Format("{0:N1}x", value);
			case EStat.CritDamage: return string.Format("{0:N1}x", value * 2f);
			case EStat.MoveSpeedMultiplier: return string.Format("{0:N2}x", value);
			case EStat.XpIncreaseMultiplier: return string.Format("{0:N1}x", Mathf.Min(value, 10f));
			default: return string.Format("{0:N0}", value);
		}
	}
}
