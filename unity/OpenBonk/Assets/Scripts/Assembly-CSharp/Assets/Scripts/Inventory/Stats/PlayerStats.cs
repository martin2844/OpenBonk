using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Actors.Player;

namespace Assets.Scripts.Inventory.Stats
{
	public static class PlayerStats
	{
		public static float GetStat(EStat stat)
		{
			if (GameManager.Instance.GetPlayerInventory() == null) throw new System.Exception("Failed to get stat: " + stat);
			return MyPlayer.Instance.inventory.playerStats.GetStat(stat);
		}

		public static float GetStatRaw(EStat stat)
		{
			if (GameManager.Instance.GetPlayerInventory() == null) throw new System.Exception("Failed to get raw stat: " + stat);
			return MyPlayer.Instance.inventory.playerStats.GetRawStat(stat);
		}

		public static StatComponents GetStatComponents(EStat stat)
		{
			if (GameManager.Instance.GetPlayerInventory() == null) throw new System.Exception("Failed to get stat: " + stat);
			return MyPlayer.Instance.inventory.playerStats.statValuesMap[stat];
		}

		public static bool HasStats()
		{
			return GameManager.Instance != null && GameManager.Instance.GetPlayerInventory() != null;
		}
	}
}
