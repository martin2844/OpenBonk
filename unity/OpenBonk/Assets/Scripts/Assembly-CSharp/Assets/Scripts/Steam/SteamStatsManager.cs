using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Steamworks;

namespace Assets.Scripts.Steam
{
	public static class SteamStatsManager
	{
		public static bool areStatsReady;

		private static Callback<UserStatsReceived_t> m_UserStatsReceived;

		private static bool hasQueuedUpload;

		private static float uploadReadyAtTime;

		private static float uploadCooldown;

		private static Dictionary<string, int> cachedStatUpdates;

		private static float setCachedStatsInterval;

		private static float nextSetCachedStatsTime;

		private static bool hasChanges;

		public static void Init()
		{
		}

		public static void OnDestroy()
		{
		}

		public static void RequestStats()
		{
		}

		private static void QueueUpload()
		{
		}

		private static void Update()
		{
		}

		private static void TryUploadStats()
		{
		}

		private static void OnStatUpdated(string arg1, MyStat arg2)
		{
		}

		private static void SetCachedStats()
		{
		}

		private static void OnUserStatsReceived(UserStatsReceived_t param)
		{
		}
	}
}
