using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;

namespace Assets.Scripts.Inventory__Items__Pickups.Progression
{
	public class UnlockUtility
	{
		public const float UNLOCKABLE_PRICE_MULTIPLIER = 1.75f;

		public static HashSet<TomeData> GetAvailableTomes()
		{
			var result = new HashSet<TomeData>();
			foreach (TomeData tome in DataManager.Instance.GetAllTomes()) if (MyAchievements.IsAvailable(tome)) result.Add(tome);
			foreach (var tome in MyPlayer.Instance.inventory.tomeInventory.tomeLevels.Keys) result.Add(DataManager.Instance.GetTome(tome));
			if (RunUnlockables.banishedUpgradables != null)
				foreach (var banished in RunUnlockables.banishedUpgradables) if (banished is TomeData tome) result.Remove(tome);
			return result;
		}

		public static List<WeaponData> GetAvailableWeapons()
		{
			if (ChallengesTracker.HasChallengeModifier("no_weapons")) return new List<WeaponData>();
			var result = new HashSet<WeaponData>();
			foreach (WeaponData weapon in DataManager.Instance.GetAllWeapons()) if (MyAchievements.IsAvailable(weapon)) result.Add(weapon);
			foreach (var weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) result.Add(weapon.weaponData);
			if (RunUnlockables.banishedUpgradables != null)
				foreach (var banished in RunUnlockables.banishedUpgradables) if (banished is WeaponData weapon) result.Remove(weapon);
			return new List<WeaponData>(result);
		}
	}
}
