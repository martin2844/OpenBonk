using System;
using System.Collections.Generic;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public class PlayerStatsNew
	{
		private static readonly float[] baseValues =
		{
			100f, 10f, 0f, 0f, 0f, 0.01f, 0f, 0f, 1f, 1f, 1f, 1f, 1f, 0f, 0f,
			1f, 0f, 0f, 0.01f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 7f, 0f, 0f, 5f,
			0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 0f, 0f, 0f,
			0f, 0f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f
		};
		public Dictionary<EStat, float> stats;

		public Dictionary<EStat, float> rawStats;

		public Dictionary<EStat, StatComponents> statValuesMap;

		private PlayerInventory playerInventory;

		public static Action<EStat> A_StatUpdate;

		private HashSet<EStat> queuedUpdateStats = new HashSet<EStat>();

		private Dictionary<EStat, HashSet<EShopItem>> statToShopItems;

		private bool ignoreShopItems;

		private readonly List<EStat> statsToUpdateBuffer = new List<EStat>();

		public PlayerStatsNew(PlayerInventory inventory, bool ignoreShopItems = false)
		{
			this.ignoreShopItems = ignoreShopItems;
			playerInventory = inventory;
			stats = new Dictionary<EStat, float>();
			rawStats = new Dictionary<EStat, float>();
			statValuesMap = new Dictionary<EStat, StatComponents>();
			statToShopItems = new Dictionary<EStat, HashSet<EShopItem>>();
			foreach (EStat stat in Enum.GetValues(typeof(EStat)))
			{
				float baseValue = GetBaseValue(stat);
				stats.Add(stat, baseValue);
				rawStats.Add(stat, baseValue);
				statToShopItems[stat] = new HashSet<EShopItem>();
				statValuesMap[stat] = new StatComponents();
			}
			foreach (EStat stat in Enum.GetValues(typeof(EStat))) UpdateStat(stat);
			PlayerStatusEffects.A_StatusModifiedStat += QueueUpdateStat;
			TomeInventory.A_TomeUpgrade += QueueUpdateStatTome;
			StatInventory.A_StatsChanged += QueueUpdateStat;
			ItemInventory.A_StatsChanged += QueueUpdateStat;
			PassiveAbility.A_StatModified += QueueUpdateStat;
		}

		public void OnDestroy()
		{
			PlayerStatusEffects.A_StatusModifiedStat -= QueueUpdateStat;
			TomeInventory.A_TomeUpgrade -= QueueUpdateStatTome;
			StatInventory.A_StatsChanged -= QueueUpdateStat;
			ItemInventory.A_StatsChanged -= QueueUpdateStat;
			PassiveAbility.A_StatModified -= QueueUpdateStat;
		}

		public void Tick()
		{
			TryPopStatUpdatesQueue();
		}

		private void TryPopStatUpdatesQueue()
		{
			if (queuedUpdateStats.Count == 0) return;
			statsToUpdateBuffer.Clear();
			statsToUpdateBuffer.AddRange(queuedUpdateStats);
			queuedUpdateStats.Clear();
			foreach (EStat stat in statsToUpdateBuffer) UpdateStat(stat);
		}

		public void ForceUpdateStats()
		{
			TryPopStatUpdatesQueue();
		}

		private void QueueUpdateStatTome(ETome eTome, EStat stat)
		{
			queuedUpdateStats.Add(stat);
		}

		private void QueueUpdateStat(EStat stat)
		{
			queuedUpdateStats.Add(stat);
		}

		private void UpdateStat(EStat stat)
		{
			float flat = 0f, addition = 0f, multiplication = 1f;
			foreach (StatModifier modifier in playerInventory.characterData.statModifiers)
				if (modifier.stat == stat) ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			if (playerInventory.tomeInventory.statToTomes.ContainsKey(stat))
				foreach (ETome tome in playerInventory.tomeInventory.statToTomes[stat])
					ApplyStatModifier(playerInventory.tomeInventory.tomeUpgrade[tome], 1, ref flat, ref addition, ref multiplication);
			foreach (ItemBase item in playerInventory.itemInventory.items.Values)
				if (item.statModifiers.ContainsKey(stat))
					foreach (StatModifier modifier in item.statModifiers[stat].GetModifiers())
						ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			foreach (StatusEffect effect in playerInventory.statusEffects.statusEffects.Values)
				foreach (StatModifier modifier in effect.modifiers)
					if (modifier.stat == stat) ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			if (playerInventory.statInventory.permanentChanges.ContainsKey(stat))
				foreach (StatModifier modifier in playerInventory.statInventory.permanentChanges[stat])
					ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			if (playerInventory.statInventory.temporaryChanges.ContainsKey(stat))
				foreach (TemporaryStat temporary in playerInventory.statInventory.temporaryChanges[stat])
					ApplyStatModifier(temporary.modifier, 1, ref flat, ref addition, ref multiplication);
			foreach (StatModifier modifier in playerInventory.statInventory.movingStats.Values)
				if (modifier.stat == stat) ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			if (playerInventory.passiveAbility != null && playerInventory.passiveAbility.statModifiers.ContainsKey(stat))
				foreach (StatModifier modifier in playerInventory.passiveAbility.statModifiers[stat].GetModifiers())
					ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			float baseValue = GetBaseValue(stat) + flat;
			float additiveValue = addition + 1f;
			float rawValue = baseValue * additiveValue * multiplication;
			rawStats[stat] = rawValue;
			stats[stat] = CheckFinalValue(stat, rawValue);
			statValuesMap[stat].SetValues(baseValue, additiveValue, multiplication);
			A_StatUpdate?.Invoke(stat);
		}

		private void ApplyStatModifier(StatModifier modifier, int amount, ref float flatValues, ref float additionValues, ref float multiplicationValues)
		{
			switch (modifier.modifyType)
			{
				case EStatModifyType.Addition: additionValues += modifier.GetModificationTotal(amount); break;
				case EStatModifyType.Multiplication: multiplicationValues *= modifier.GetModificationTotal(amount); break;
				case EStatModifyType.Flat: flatValues += modifier.GetModificationTotal(amount); break;
			}
		}

		private float CheckFinalValue(EStat stat, float value)
		{
			switch (stat)
			{
				case EStat.MaxHealth: return Mathf.Clamp(value, 1f, 2147483648f);
				case EStat.Armor: return value / (value + 0.75f) * 0.999f;
				case EStat.Evasion: return value / (value + 1f) * 0.999f;
				default: return value;
			}
		}

		public static float GetBaseValue(EStat stat)
		{
			int index = (int)stat;
			return index >= 0 && index < baseValues.Length ? baseValues[index] : 0f;
		}

		public float GetStat(EStat stat)
		{
			float value = stats[stat];
			if (stat == EStat.MaxHealth) return Mathf.Max(1f, value);
			if (stat == EStat.Projectiles || stat == EStat.ProjectileBounces) return Mathf.FloorToInt(value);
			return value;
		}

		public float GetRawStat(EStat stat)
		{
			return rawStats[stat];
		}

		public float GetUnclampedStat(EStat stat)
		{
			return stats[stat];
		}
	}
}
