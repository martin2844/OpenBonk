using System.Collections.Generic;
using Steamworks;

namespace Assets.Scripts.Steam
{
	public class LeaderboardContainer
	{
		public int numEntries;

		public SteamLeaderboard_t leaderboardHandle;

		public List<LeaderboardEntry> globalEntries;

		public LeaderboardEntry localEntry;

		public LeaderboardContainer(SteamLeaderboard_t handle)
		{
		}
	}
}
