using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts._Data.Progression;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Assets.Scripts.UI.Localization
{
	public class LocalizationUtility
	{
		public static bool IsEnglish()
		{
			return LocalizationSettings.SelectedLocale != null && LocalizationSettings.SelectedLocale.Identifier.Code == "en";
		}

		public static string GetLocalizedString(string table, string key, string defaultEnglishString, bool useEnglishDefaultIfAvailable = true)
		{
			if (useEnglishDefaultIfAvailable && IsEnglish()) return defaultEnglishString;
			return LocalizationSettings.StringDatabase.GetTable(table)?.GetEntry(key)?.LocalizedValue ?? defaultEnglishString;
		}

		public static string GetLocalizedString(string table, string key)
		{
			return LocalizationSettings.StringDatabase.GetTable(table)?.GetEntry(key)?.LocalizedValue ?? ("Missing localization for " + table + "." + key);
		}

		public static string GetLocalizedString(string table, string key, Dictionary<string, string> smartStrings)
		{
			return LocalizationSettings.StringDatabase.GetTable(table)?.GetEntry(key)?.GetLocalizedString(smartStrings) ?? ("Missing localization for " + table + "." + key);
		}

		public static string GetLocalizedDamageSource(string source)
		{
			if (source == "Unkown") return "-";
			if (source != null && source.StartsWith("mod:", System.StringComparison.Ordinal))
			{
				if (DataManager.Instance != null && DataManager.Instance.TryGetExternalItemDamageSource(source, out var external)) return external.GetName();
				return source.Substring(4);
			}
			if (DataManager.Instance != null)
			{
				try
				{
					if (System.Enum.TryParse(source, out EWeapon weapon)) return DataManager.Instance.GetWeapon(weapon).GetName();
					if (System.Enum.TryParse(source, out EItem item)) return DataManager.Instance.GetItem(item).GetName();
				}
				catch (KeyNotFoundException) { return source; }
			}
			return GetLocalizedString("DamageSources", source);
		}

		public static bool HasLocalizedString(string table, string key)
		{
			var entry = LocalizationSettings.StringDatabase.GetTable(table)?.GetEntry(key);
			return entry != null && !string.IsNullOrEmpty(entry.LocalizedValue);
		}

		public static LocalizedString GetLocalizedStringReference(string table, string key)
		{
			return new LocalizedString(table, key);
		}

		public static string GetStatName(EStat stat)
		{
			return LocalizationSettings.StringDatabase.GetTable("Stats")?.GetEntry(stat + "_NAME")?.LocalizedValue ?? "LOCALIZATION ERROR in LocalizationUtility.GetStatName";
		}

		public static string GetStatDesc(EStat stat)
		{
			var entry = LocalizationSettings.StringDatabase.GetTable("Stats")?.GetEntry(stat + "_DESC");
			return entry == null ? GetStatName(stat) : entry.LocalizedValue ?? "LOCALIZATION ERROR in LocalizationUtility.GetStatDesc";
		}

		public static string GetCharacterName(ECharacter character)
		{
			return DataManager.Instance.GetCharacterData(character).GetName();
		}

		public static string GetEnemyName(EEnemy enemy)
		{
			return GetLocalizedString("Enemies", enemy.ToString());
		}

		public static string GetRarity(ERarity rarity)
		{
			return GetLocalizedString("Rarities", rarity.ToString());
		}

		public static string GetRarity(EItemRarity rarity)
		{
			return GetLocalizedString("Rarities", rarity.ToString());
		}

		public static string GetAchievementType(EAchievementType achievementType)
		{
			return GetLocalizedStringReference("QuestsUi", achievementType.ToString()).GetLocalizedString();
		}

		public static string GetLanguageName(Locale locale)
		{
			if (locale == null) return "";
			string name = GetLanguageName(locale.Identifier.Code);
			if (string.IsNullOrEmpty(name)) name = locale.LocaleName;
			return string.IsNullOrEmpty(name) ? locale.name : name;
		}

		public static string GetLanguageName(string code)
		{
			if (string.IsNullOrEmpty(code)) return "";
			// These labels are the game's own names, including distinct regional variants.
			switch (code)
			{
				case "zh-Hans": return "中文 (简体)";
				case "zh-Hant": return "中文 (繁體)";
				case "cs": return "Čeština";
				case "en": return "English";
				case "fr": return "Français";
				case "de": return "Deutsch";
				case "it": return "Italiano";
				case "ja": return "日本語";
				case "ko": return "한국어";
				case "pl": return "Polski";
				case "pt": return "Português";
				case "ru": return "Русский";
				case "es": return "Español";
				case "es-419": return "Español (Latinoamérica)";
				case "th": return "ไทย";
				case "tr": return "Türkçe";
				case "uk": return "Українська";
			}
			try { return System.Globalization.CultureInfo.GetCultureInfo(code).NativeName; }
			catch (System.Globalization.CultureNotFoundException) { return code; }
		}
	}
}
