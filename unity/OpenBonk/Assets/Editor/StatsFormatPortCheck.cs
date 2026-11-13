using System;
using System.Globalization;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public static class StatsFormatPortCheck
{
	public static void Run()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
			void Check(EStat stat, float value, string expected)
			{
				string actual = StatsUi.FormatStat(stat, value);
				if (actual != expected) throw new Exception($"{stat}: expected {expected}, got {actual}");
			}
			Check(EStat.MaxHealth, 1234f, "1,234");
			Check(EStat.Armor, .25f, "25%");
			Check(EStat.DamageMultiplier, 1.5f, "1.5x");
			Check(EStat.CritDamage, 1.5f, "3.0x");
			Check(EStat.MoveSpeedMultiplier, 1.25f, "1.25x");
			Check(EStat.XpIncreaseMultiplier, 12f, "10.0x");
			Check(EStat.DurationMultiplier, 1.03f, "1.0x");
			if (StatsUi.FormatStat(EStat.DurationMultiplier, 1.03f, true) != "1.03x" || StatsUi.FormatStat(EStat.PowerupChance, 1.03f, true) != "1.03x" || StatsUi.FormatStat(EStat.DamageMultiplier, 1.03f, true) != "1.0x") throw new Exception("external passive precision mismatch");
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");
			if (StatsUi.FormatStat(EStat.DurationMultiplier, 1.03f, true) != "1,03x") throw new Exception("external passive precision locale mismatch");
			Debug.Log("StatsFormatPortCheck passed");
		}
		finally { CultureInfo.CurrentCulture = previous; }
	}
}
