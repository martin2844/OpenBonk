using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.UI.Mouse
{
	public static class Tooltip
	{
		private const string tooltipColor = "#ffe88a";

		private const string tooltipColorOther = "#42b9f5";

		public static string GetTooltipString(EStat stat, string color = "#ffe88a")
		{
			return "<link=\"" + stat + "\"><color=" + color + ">" + EnumUtility.EnumToReadable(stat) + "</color></link>";
		}

		public static string GetTooltipString(string s, string forceColor = "")
		{
			string color = forceColor == "" ? tooltipColorOther : forceColor;
			return "<link=\"" + s + "\"><color=" + color + ">" + s + "</color></link>";
		}

		public static string GetIdInfo(string keyword)
		{
			return System.Enum.TryParse(keyword, out EStat stat)
				? Assets.Scripts.UI.Localization.LocalizationUtility.GetStatDesc(stat)
				: "???: " + keyword;
		}
	}
}
