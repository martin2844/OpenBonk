using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using Assets.Scripts.UI;
using UnityEngine;

public class ShrineLogs : MonoBehaviour
{
	public GameObject prefab;

	private static List<StatModifier> backLog = new List<StatModifier>();

	private static List<StatModifier> shownLog = new List<StatModifier>();

	private bool isInitialized;

	private void Start()
	{
		isInitialized = true;
		foreach (StatModifier stat in shownLog) AddLog(stat, false);
	}

	public static void Reset()
	{
		backLog = new List<StatModifier>();
		shownLog = new List<StatModifier>();
	}

	public void AddLog(StatModifier statModifier, bool isNew = true)
	{
		if (!isInitialized)
		{
			shownLog.Add(statModifier);
			return;
		}
		if (isNew) shownLog.Add(statModifier);
		if (statModifier == null || prefab == null) return;
		bool negative = statModifier.modifyType == EStatModifyType.Multiplication
			? statModifier.modification < 1f : statModifier.modification < 0f;
		string color = negative ? MyColorUtility.requirementMissingColor : MyColorUtility.requirementCompletedColor;
		string description = "<color=" + color + ">" + StatUtility.GetModificationString(statModifier, false) +
			"</color> " + EnumUtility.EnumToReadable(statModifier.stat);
		GameObject entry = Instantiate(prefab, prefab.transform.parent);
		entry.SetActive(true);
		ShrineLogEntry log = entry.GetComponent<ShrineLogEntry>();
		if (log != null) log.Set(description);
		UiUtility.RebuildUi(transform);
	}
}
