using System;
using System.Collections.Generic;
using Steamworks;

namespace Assets.Scripts.Steam.LeaderboardsNew
{
	public class SteamLeaderboardNew
	{
		public string lbName;

		public string lbNameFriends;

		public SteamLeaderboard_t lbHandle;

		public SteamLeaderboard_t lbHandleFriends;

		public List<LeaderboardEntry> globalEntries;

		public List<LeaderboardEntry> friendsEntries;

		public LeaderboardEntry localEntry;

		public int localEntryRankFriends;

		public static Action<SteamLeaderboardNew> A_LeaderboardReady;

		private bool isSingleBoard;

		public bool scanForLegit;

		private int leaderboardEntriesPerRequest;

		private int currentIndex;

		private int numEntriesGlobal;

		private int numEntriesFriends;

		private int numDesiredGlobalEntries;

		private bool hasFriends;

		private bool hasGlobal;

		public SteamLeaderboardNew(string name, bool singleBoard = false, int entriesPerRequest = 200, int desiredNumEntries = 150, bool scanForLegit = true)
		{
		}

		public void SetHandle(SteamLeaderboard_t handle, string lb)
		{
		}

		private bool IsFriendsLb(string lb)
		{
			return false;
		}

		public bool IsReadyToDownloadEntries()
		{
			return false;
		}

		public bool IsReadyToDisplay()
		{
			return false;
		}

		public int GetTotalEntries(string lb)
		{
			return 0;
		}

		public void Refresh()
		{
		}

		private void RequestGlobalEntries()
		{
		}

		public void OnDownloadResults(string lbNameDownloaded, LeaderboardScoresDownloaded_t param)
		{
		}

		private void OnDownloadResultsGlobal(LeaderboardScoresDownloaded_t param)
		{
		}

		private void OnDownloadResultsFriends(LeaderboardScoresDownloaded_t param)
		{
		}

		private void CheckIfLeaderboardsAreReady()
		{
		}

		public void OnDownloadResultsLocal(string lbNameDownloaded, LeaderboardScoresDownloaded_t param)
		{
		}
	}
}
