using System;
using System.Collections.Generic;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public class StatInventory
	{
		public Dictionary<EStat, List<StatModifier>> permanentChanges = new Dictionary<EStat, List<StatModifier>>();

		public Dictionary<EStat, List<TemporaryStat>> temporaryChanges = new Dictionary<EStat, List<TemporaryStat>>();

		public Dictionary<string, StatModifier> movingStats = new Dictionary<string, StatModifier>();

		public static Action<EStat> A_StatsChanged;

		private HashSet<EStat> refreshStats = new HashSet<EStat>();

		public void ChangeStat(StatModifier stat, bool permanent, float timeout, bool addToShrineLog)
		{
			if (permanent)
			{
				if (!permanentChanges.ContainsKey(stat.stat)) permanentChanges.Add(stat.stat, new List<StatModifier>());
				permanentChanges[stat.stat].Add(stat);
				if (addToShrineLog && UiManager.Instance != null) UiManager.Instance.shrineLogs.AddLog(stat, true);
			}
			else
			{
				if (!temporaryChanges.ContainsKey(stat.stat)) temporaryChanges.Add(stat.stat, new List<TemporaryStat>());
				temporaryChanges[stat.stat].Add(new TemporaryStat(stat, MyTime.time + timeout));
			}
			refreshStats.Add(stat.stat);
		}

		public void ChangeMovingStat(string name, StatModifier statModifier)
		{
			movingStats[name] = statModifier;
			refreshStats.Add(statModifier.stat);
		}

		public void RemoveMovingStat(string name)
		{
			if (!movingStats.ContainsKey(name)) return;
			EStat stat = movingStats[name].stat;
			movingStats.Remove(name);
			refreshStats.Add(stat);
		}

		public void Tick()
		{
			if (temporaryChanges.Count > 0)
			{
				var emptyStats = new List<EStat>();
				foreach (var entry in temporaryChanges)
				{
					for (int i = entry.Value.Count - 1; i >= 0; i--)
					{
						if (MyTime.time >= entry.Value[i].expirationTime)
						{
							entry.Value.RemoveAt(i);
							refreshStats.Add(entry.Key);
						}
					}
					if (entry.Value.Count == 0) emptyStats.Add(entry.Key);
				}
				foreach (EStat stat in emptyStats) temporaryChanges.Remove(stat);
			}
			if (refreshStats.Count > 0)
			{
				foreach (EStat stat in refreshStats) A_StatsChanged?.Invoke(stat);
				refreshStats = new HashSet<EStat>();
			}
		}
	}
}
