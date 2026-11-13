namespace Assets.Scripts.Steam
{
	public class Leaderboards
	{
		public static int numMaxDeatils;

		public static void UploadScore(int score)
		{
		}

		private static int[] GetWeapons()
		{
			return null;
		}

		private static int[] GetTomes()
		{
			return null;
		}

		public static ECharacter GetCharacter(int[] details)
		{
			return default(ECharacter);
		}

		private static bool IsLegitCharacter(int[] details)
		{
			return false;
		}

		public static bool CanShowScore(ulong steamid, int score, int[] leaderboardDetails, out string s)
		{
			s = null;
			return false;
		}

		private static string GetSecretKey()
		{
			return null;
		}

		private static int[] GenerateScoreHash(int score)
		{
			return null;
		}

		private static int[] GenerateScoreHashNew(int score, ulong steamid)
		{
			return null;
		}
	}
}
