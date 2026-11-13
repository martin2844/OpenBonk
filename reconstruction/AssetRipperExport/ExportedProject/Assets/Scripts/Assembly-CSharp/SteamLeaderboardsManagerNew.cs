using System;
using System.Collections.Generic;
using Assets.Scripts.Steam.LeaderboardsNew;
using Steamworks;

public static class SteamLeaderboardsManagerNew
{
	public const string lbVersion = "v2";

	public const string killsLb = "kills";

	public const string killsLbWeekly = "kills_weekly";

	public const string friendsLb = "friends";

	public static SteamLeaderboardNew leaderboardKillsAllTime;

	public static SteamLeaderboardNew leaderboardKillsWeekly;

	public static SteamLeaderboardNew leaderboardBannedPlayers;

	private static List<SteamLeaderboardNew> leaderboardNames;

	private static Dictionary<string, SteamLeaderboardNew> lbNameToLeaderboard;

	public static HashSet<ulong> cheaters;

	private static Dictionary<string, CallResult<LeaderboardFindResult_t>> leaderboardFindResults;

	private static Dictionary<string, CallResult<LeaderboardScoresDownloaded_t>> leaderboardScoresDownloadedResults;

	private static Dictionary<string, CallResult<LeaderboardScoresDownloaded_t>> leaderboardScoresDownloadedLocalResults;

	private static Dictionary<string, CallResult<LeaderboardScoreUploaded_t>> leaderboardScoreUploadResults;

	public static Action A_CheatersUpdated;

	private static bool initialized;

	private static Queue<LeaderboardUploadQueued> uploadQueue;

	private static bool isLeaderboardUploadInProgress;

	private static DateTime currentUploadStartTime;

	private static float uploadTimeoutSeconds;

	public static Action<string, int> A_LeaderboardScoreUploaded;

	public static void Init()
	{
	}

	public static void OnDestroy()
	{
	}

	private static void Update()
	{
	}

	public static void MenuOpened()
	{
	}

	public static void QueueLeaderboardUpload(string leaderboardName, int score, int[] details, bool isFriendsLb)
	{
	}

	private static void CheckUploadQueue()
	{
	}

	public static void FindLeaderboard(string leaderboardName)
	{
	}

	public static void DownloadLeaderboardEntries(string lbName, SteamLeaderboard_t handle, ELeaderboardDataRequest dataRequest, int rangeStart, int rangeEnd)
	{
	}

	public static void DownloadLeaderboardEntryLocal(string lbName, SteamLeaderboard_t handle)
	{
	}

	public static void UploadLeaderboardScore(string leaderboardName, int score, int[] details, bool isFriendsLb)
	{
	}

	private static void TryRefreshLeaderboard(string lbName)
	{
	}

	public static SteamLeaderboardNew GetLeaderboard(string lbName)
	{
		return null;
	}

	public static bool IsCheater(ulong steamid)
	{
		return false;
	}

	private static void OnLeaderboardReady(SteamLeaderboardNew leaderboard)
	{
	}

	private static void OnLeaderboardFindResult(LeaderboardFindResult_t param, bool bioFailure)
	{
	}

	private static void LeaderboardScoresDownloadedLocal(LeaderboardScoresDownloaded_t param, bool biofailure)
	{
	}

	private static void LeaderboardScoreUploaded(LeaderboardScoreUploaded_t param, bool biofailure)
	{
	}
}
