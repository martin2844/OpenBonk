using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponPassives;
using Assets.Scripts.Menu.Shop;
using UnityEngine;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons
{
	public class WeaponBase
	{
		private float usedWeaponAtTime;

		public WeaponData weaponData;

		public int level;

		private Dictionary<EStat, float> weaponStats = new Dictionary<EStat, float>();

		private List<List<StatModifier>> upgrades;

		private WeaponPassive passive;
		private OpenBonkLuaWeaponSession externalSession;

		public static Action<EStat, EWeapon> A_WeaponStatUpdate;

		public bool enabled { get; private set; } = true;

		public WeaponBase(WeaponData data)
		{
			level = 1;
			weaponData = data;
			upgrades = new List<List<StatModifier>>();
			passive = WeaponPassiveFactory.GetWeaponPassive(this);
			passive?.Init();
			foreach (EStat stat in weaponData.baseStats.Keys) UpdateStat(stat);
			if (data is OpenBonkLuaWeaponData external) externalSession = new OpenBonkLuaWeaponSession(external);
		}

		public void Cleanup()
		{
			passive?.Cleanup();
			externalSession?.Dispose(); externalSession = null;
		}

		public void Disable()
		{
			enabled = false;
		}

		public void Enable()
		{
			enabled = true;
		}

		public void Use()
		{
			if (!enabled || IsCooldown()) return;
			if (weaponData is OpenBonkLuaWeaponData) externalSession?.Attack(this);
			else WeaponUtility.WeaponAttack(this);
			usedWeaponAtTime = MyTime.time;
		}

		public void Upgrade(List<StatModifier> upgradeOffer)
		{
			if (upgradeOffer == null)
			{
				level++;
				return;
			}
			if (upgradeOffer.Count > 0) upgrades.Add(upgradeOffer);
			level++;
			foreach (StatModifier modifier in upgradeOffer) UpdateStat(modifier.stat);
		}

		private void UpdateStat(EStat stat)
		{
			float flat = 0f, addition = 0f, multiplication = 1f;
			foreach (List<StatModifier> offer in upgrades)
				foreach (StatModifier modifier in offer)
					if (modifier.stat == stat) ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			if (passive != null)
				foreach (var entry in passive.statModifiers)
					if (entry.Key == stat)
						foreach (StatModifier modifier in entry.Value.GetModifiers())
							ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			weaponStats[stat] = (GetBaseValue(stat) + flat) * (addition + 1f) * multiplication;
			A_WeaponStatUpdate?.Invoke(stat, weaponData.eWeapon);
		}

		public float GetTestUpdateStat(EStat stat, StatModifier testUpgrade)
		{
			float flat = 0f, addition = 0f, multiplication = 1f;
			foreach (List<StatModifier> offer in upgrades)
				foreach (StatModifier modifier in offer)
					if (modifier.stat == stat) ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			if (passive != null)
				foreach (var entry in passive.statModifiers)
					if (entry.Key == stat)
						foreach (StatModifier modifier in entry.Value.GetModifiers())
							ApplyStatModifier(modifier, 1, ref flat, ref addition, ref multiplication);
			ApplyStatModifier(testUpgrade, 1, ref flat, ref addition, ref multiplication);
			return (GetBaseValue(stat) + flat) * (addition + 1f) * multiplication;
		}

		public float GetValue(EStat stat)
		{
			float value = weaponStats[stat];
			return stat == EStat.AttackSpeed ? Mathf.Max(0.01f, value) : value;
		}

		public void WeaponPassiveChanged(EStat stat)
		{
			UpdateStat(stat);
		}

		private float GetBaseValue(EStat stat)
		{
			return weaponData.GetBaseStat(stat);
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

		private bool IsCooldown()
		{
			return usedWeaponAtTime + GetCooldown() >= MyTime.time;
		}

		private float GetCooldown()
		{
			return WeaponUtility.GetWeaponCooldown(this);
		}
	}
}
