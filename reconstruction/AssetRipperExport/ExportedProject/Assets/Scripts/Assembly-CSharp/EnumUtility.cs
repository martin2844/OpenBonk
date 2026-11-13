using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Menu.Shop;

public static class EnumUtility
{
	public static string EnumToReadable(string s)
	{
		return System.Text.RegularExpressions.Regex.Replace(s, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
	}

	public static string EnumToReadable(EStat eStat)
	{
		return Assets.Scripts.UI.Localization.LocalizationUtility.GetStatName(eStat);
	}

	public static bool HasFlagsAny(this EEnemyFlag value, EEnemyFlag flags)
	{
		return (value & flags) != 0;
	}

	public static bool HasFlagsAll(this EEnemyFlag value, EEnemyFlag flags)
	{
		return (value & flags) == flags;
	}
}
