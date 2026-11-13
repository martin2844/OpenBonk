using System;
using UnityEngine;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;

namespace Inventory__Items__Pickups.Xp_and_Levels
{
	public class PlayerXp
	{
		public int xp;

		public int level;

		public static float maxXpMultiplier = 10f;

		public static Action<int> A_LevelUp;

		public static Action<PlayerXp, int> A_XpAdded;

		private float leftOverXp;

		public void AddXp(int amount)
		{
			float adjusted = Mathf.Clamp(PlayerStats.GetStat(EStat.XpIncreaseMultiplier), 0f, maxXpMultiplier) * amount;
			int gained = Mathf.FloorToInt(adjusted);
			leftOverXp += adjusted - gained;
			int carried = Mathf.FloorToInt(leftOverXp);
			if (carried > 0)
			{
				gained += carried;
				leftOverXp -= carried;
			}
			xp += gained;
			A_XpAdded?.Invoke(this, gained);
			int newLevel = XpUtility.XpToLevel(xp);
			if (newLevel > level)
			{
				level = newLevel;
				A_LevelUp?.Invoke(level);
			}
		}

		public int GetXpInt()
		{
			return (int)(float)xp;
		}
	}
}
