using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons
{
	public class WeaponInventory
	{
		private bool isMaxed;

		private bool hasAimableWeapon;

		public Dictionary<EWeapon, WeaponBase> weapons = new Dictionary<EWeapon, WeaponBase>();

		public static Action<WeaponBase> A_WeaponAdded;

		public static Action<WeaponBase> A_WeaponRemoved;

		public static Action<WeaponBase> A_WeaponToggled;

		public void AddWeapon(WeaponData weaponData, List<StatModifier> upgradeOffer)
		{
			if (ChallengesTracker.HasChallengeModifier("no_weapons") || weaponData == null) return;
			EWeapon weapon = weaponData.eWeapon;
			if (weapons.ContainsKey(weapon)) weapons[weapon].Upgrade(upgradeOffer);
			else weapons.Add(weapon, new WeaponBase(DataManager.Instance.GetWeapon(weapon)));
			CheckMaxed();
			if (weaponData.hasCrosshair) hasAimableWeapon = true;
			A_WeaponAdded?.Invoke(weapons[weapon]);
		}

		public void ToggleWeapon(EWeapon eWeapon, bool enable)
		{
			if (!weapons.TryGetValue(eWeapon, out WeaponBase weapon)) return;
			if (enable) weapon.Enable();
			else weapon.Disable();
			A_WeaponToggled?.Invoke(weapon);
		}

		private void RemoveWeapon()
		{
		}

		public void Tick()
		{
			foreach (WeaponBase weapon in weapons.Values) weapon.Use();
		}

		public int GetNumWeapons()
		{
			return weapons.Count;
		}

		public int GetWeaponLevel(EWeapon eWeapon)
		{
			return weapons.ContainsKey(eWeapon) ? weapons[eWeapon].level : 0;
		}

		private void CheckMaxed()
		{
			isMaxed = true;
			foreach (EWeapon weapon in weapons.Keys)
				if (!IsMaxLevel(weapon)) { isMaxed = false; break; }
		}

		private bool IsMaxLevel(EWeapon eWeapon)
		{
			return weapons.ContainsKey(eWeapon) && weapons[eWeapon].level >= weapons[eWeapon].weaponData.GetMaxLevel();
		}

		public bool IsMaxed()
		{
			if (weapons.Count < InventoryUtility.GetNumAvailableWeaponSlots()) return false;
			foreach (EWeapon weapon in weapons.Keys)
				if (!IsMaxLevel(weapon)) return false;
			return true;
		}

		public bool HasAimableWeapon()
		{
			return hasAimableWeapon;
		}

		public void Cleanup()
		{
			foreach (WeaponBase weapon in weapons.Values) weapon.Cleanup();
		}
	}
}
