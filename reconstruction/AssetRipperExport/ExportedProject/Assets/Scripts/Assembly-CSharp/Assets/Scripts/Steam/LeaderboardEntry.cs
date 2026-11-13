using Steamworks;

namespace Assets.Scripts.Steam
{
	public class LeaderboardEntry
	{
		public LeaderboardEntry_t leaderboardEntry;

		public int[] details;

		public LeaderboardEntry(LeaderboardEntry_t leaderboardEntry, int[] details)
		{
		}

		public ECharacter GetCharacter()
		{
			return default(ECharacter);
		}
	}
}
