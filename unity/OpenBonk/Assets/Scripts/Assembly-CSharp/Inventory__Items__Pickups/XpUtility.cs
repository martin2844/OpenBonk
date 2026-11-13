using UnityEngine;

namespace Inventory__Items__Pickups
{
	public class XpUtility
	{
		public static float eliteEnemyXpMultiplier = 10f;

		private static int baseXp = 5;

		private static float increaseRate = 1.065f;

		private static float exponent = 1.065f;

		public const int maxLevel = 9999;

		public static int[] xpForLevelsTable;

		public static void Init()
		{
			xpForLevelsTable = new int[maxLevel + 1];
			float cumulative = 0f;
			for (int level = 1; level <= maxLevel; level++)
			{
				float increment = level == 1 ? 7f : baseXp + Mathf.Pow(level, exponent) * increaseRate * baseXp;
				cumulative += increment;
				xpForLevelsTable[level] = Mathf.RoundToInt(cumulative);
			}
		}

		public static int XpToLevel(int xp)
		{
			int level = 0;
			while (xpForLevelsTable[level + 1] <= xp && level < maxLevel - 1) level++;
			if (level == maxLevel - 1 && xpForLevelsTable[maxLevel] <= xp) level = maxLevel;
			return level;
		}

		public static int XpToNextLevel(int currentXp)
		{
			return XpToNextLevelTotal(currentXp) - XpOnCurrentLevel(currentXp);
		}

		public static int XpOnCurrentLevel(int currentXp)
		{
			return currentXp - LevelToXp(XpToLevel(currentXp));
		}

		public static int XpToNextLevelTotal(int currentXp)
		{
			int level = XpToLevel(currentXp);
			return LevelToXp(level + 1) - LevelToXp(level);
		}

		public static float CurrentLevelProgress(int currentXp)
		{
			return (float)XpOnCurrentLevel(currentXp) / XpToNextLevelTotal(currentXp);
		}

		public static int LevelToXp(int lvl)
		{
			if (lvl < 0) return 0;
			if (lvl > maxLevel) return maxLevel;
			return xpForLevelsTable[lvl];
		}

		public static int XpTotalCurrentLevel(int xp)
		{
			return LevelToXp(XpToLevel(xp));
		}

		public static int XpTotalNextLevel(int xp)
		{
			return LevelToXp(XpToLevel(xp) + 1);
		}
	}
}
