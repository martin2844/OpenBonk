namespace Assets.Scripts.Steam
{
	public static class SteamRichPresenceManager
	{
		public const string LEVEL_KEY = "lvl";

		public const string CHARACTER_KEY = "character";

		public const string MAP_KEY = "map";

		public const string TIME_KEY = "time";

		public const string MENU_STATUS_KEY = "menu_string";

		public const string DISPLAY_KEY = "steam_display";

		public const string TOKEN_MENU = "#Status_AtMainMenu";

		public const string TOKEN_INGAME = "#Status_InGame";

		private static float previousSecond;

		private static int lastSetLevel;

		private static int lastSetStage;

		public static void Init()
		{
		}

		private static void MainMenuOpened()
		{
		}

		public static void UpdateDisplayInGame()
		{
		}

		public static string GetPlayerLevel()
		{
			return null;
		}

		private static void Update()
		{
		}

		private static void UpdateTimer()
		{
		}

		private static void CheckUpdateLevel()
		{
		}

		private static void CheckUpdateStage()
		{
		}

		private static void SetKeyValue(string key, string value)
		{
		}

		public static string GetCharacter()
		{
			return null;
		}

		public static ECharacter GetEnumCharacter()
		{
			return default(ECharacter);
		}

		public static string GetMapString()
		{
			return null;
		}

		private static string GetTime()
		{
			return null;
		}

		public static string GetRandomMenuStatus()
		{
			return null;
		}

		private static void Refresh()
		{
		}

		public static void OnDestroy()
		{
		}
	}
}
