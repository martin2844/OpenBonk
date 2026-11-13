using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Pickups
{
	public class PowerupConstants
	{
		public static float GetTime(EStatusEffect statusEffect)
		{
			switch (statusEffect) { case EStatusEffect.Haste: return GetHasteTime(); case EStatusEffect.Rage: return GetRageTime(); case EStatusEffect.Shield: return GetShieldTime(); default: throw new System.ArgumentOutOfRangeException(nameof(statusEffect)); }
		}

		public static float GetExplosionRadius()
		{
			return 150f * GetMultiplier();
		}

		public static float GetHasteTime()
		{
			return 20f * GetMultiplier();
		}

		public static float GetHasteMultiplier()
		{
			return 1.75f * GetMultiplier();
		}

		public static float GetRageTime()
		{
			return 15f * GetMultiplier();
		}

		public static float GetRageDamageMultiplier()
		{
			return 1.5f * GetMultiplier();
		}

		public static float GetRageCooldownMultiplier()
		{
			return 2f * GetMultiplier();
		}

		public static float GetShieldTime()
		{
			return 15f * GetMultiplier();
		}

		public static float GetStonksTime()
		{
			return 15f * GetMultiplier();
		}

		public static float GetStonksMultiplier()
		{
			return 2f * Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.PowerupBoostMultiplier);
		}

		public static float GetFreezeTime()
		{
			return 12f * GetMultiplier();
		}

		private static float GetMultiplier()
		{
			return PlayerStats.GetStat(EStat.PowerupBoostMultiplier);
		}
	}
}
