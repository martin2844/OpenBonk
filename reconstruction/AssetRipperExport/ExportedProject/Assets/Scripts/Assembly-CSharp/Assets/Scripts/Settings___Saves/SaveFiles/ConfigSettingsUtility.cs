using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Assets.Scripts.Settings___Saves.SaveFiles
{
	public static class ConfigSettingsUtility
	{
		public static SettingType GetSettingType(FieldInfo field)
		{
			if (field.Name == "resolution") return SettingType.Resolution;
			if (field.Name == "language") return SettingType.Language;
			if (field.Name == "controller") return SettingType.ControllerDisplay;
			if (field.FieldType == typeof(string)) return SettingType.Control;
			if (field.FieldType == typeof(int) || field.FieldType == typeof(bool)) return SettingType.Enum;
			return field.FieldType == typeof(float) ? SettingType.Slider : SettingType.ControlNew;
		}

		public static string CheckSettingName(string settingName)
		{
			if (settingName == "controller_sensitivity" || settingName == "pause_on_controller_disconnect") return settingName;
			return settingName.Replace("controller_", "").Replace("keyboard_", "");
		}

		public static string GetSettingDescription(string settingName)
		{
			return "Configure " + SettingNameToReadable(settingName);
		}

		public static string[] GetSettingValues(string settingName)
		{
			switch (settingName)
			{
				case "anti_aliasing": return Enum.GetNames(typeof(AntiAliasing));
				case "bloom": return Enum.GetNames(typeof(Bloom));
				case "controller_type": return Enum.GetNames(typeof(EControllerType));
				case "enemy_targeting_mode": return Enum.GetNames(typeof(EEnemyTargetingMode));
				case "hp_bar_color": case "warning_color": return Enum.GetNames(typeof(EHpBarColor));
				case "fullscreen_mode": return Enum.GetNames(typeof(FullScreenMode));
				case "grass_quality": return Enum.GetNames(typeof(GrassQuality));
				case "shadow_quality": return Enum.GetNames(typeof(ShadowQuality));
				case "shadow_resolution": return Enum.GetNames(typeof(ShadowResolution));
				case "texture_quality": return Enum.GetNames(typeof(TextureQuality));
				case "vsync": return Enum.GetNames(typeof(VSync));
				case "resolution": return GetResolutionNames();
				case "language": return GetLanguageNames();
				case "controller": return GetControllers();
				case "target_monitor": return GetMonitorNames();
				default: return Enum.GetNames(typeof(BoolSetting));
			}
		}

		public static bool GetSliderWholeNumbers(string settingName)
		{
			return settingName == "fov" || settingName == "fps_limit" || settingName == "input_delay" ||
				settingName == "max_splits" || settingName == "num_tomes" || settingName == "auto_select_after_level";
		}

		public static void GetSliderRange(string settingName, out float min, out float max)
		{
			min = 0f;
			max = 10f;
			switch (settingName)
			{
				case "fps_limit": min = 30f; max = 300f; break;
				case "auto_select_after_level": min = 10f; max = 600f; break;
				case "fov": min = 60f; max = 140f; break;
				case "crosshair_size": min = .25f; max = 2f; break;
				case "minimap_size": min = .5f; max = 2f; break;
				case "camera_distance": max = 5f; break;
				case "input_delay": max = 6f; break;
				case "ui": case "crosshair_height": case "controller_vibration": case "crosshair_alpha":
				case "ambience": case "game_sfx": case "difficulty": case "look_smoothing": case "particle_opacity":
				case "xp_and_gold": case "master_volume": case "music": case "random_enemy_targeting": max = 1f; break;
			}
		}

		private static string[] GetResolutionNames()
		{
			Resolution[] resolutions = GetMyResolutions();
			string[] names = new string[resolutions.Length];
			for (int i = 0; i < resolutions.Length; i++) names[i] = string.Format("{0}x{1}", resolutions[i].width, resolutions[i].height);
			return names;
		}

		private static string[] GetLanguageNames()
		{
			var locales = LocalizationSettings.AvailableLocales.Locales;
			string[] names = new string[locales.Count];
			for (int i = 0; i < locales.Count; i++) names[i] = locales[i].Identifier.Code;
			return names;
		}

		public static Resolution[] GetMyResolutions()
		{
			var unique = new List<Resolution>();
			foreach (Resolution resolution in Screen.resolutions)
			{
				var mode = resolution;
#if UNITY_ANDROID && !UNITY_EDITOR
				// Android may report display modes in the panel's natural portrait orientation.
				if (mode.width < mode.height) { int width = mode.width; mode.width = mode.height; mode.height = width; }
#endif
				if (!unique.Exists(existing => AreResolutionSame(existing, mode))) unique.Add(mode);
			}
#if UNITY_ANDROID && !UNITY_EDITOR
			// Some Android devices do not expose a mode list; retain their current display size as a selectable entry.
			if (unique.Count == 0 && Screen.width > 0 && Screen.height > 0)
				unique.Add(new Resolution { width = Screen.width, height = Screen.height });
#endif
			return unique.ToArray();
		}

		public static int GetCurrentResolutionIndex(Resolution[] modes, int configuredIndex, int width, int height)
		{
			if (modes == null) return -1;
			if (configuredIndex >= 0 && configuredIndex < modes.Length) return configuredIndex;
			for (int i = 0; i < modes.Length; i++)
				if (modes[i].width == width && modes[i].height == height) return i;
			return -1;
		}

		public static string[] GetControllers()
		{
			var names = new List<string> { "None" };
#if UNITY_ANDROID && !UNITY_EDITOR
			foreach (string name in Input.GetJoystickNames()) if (!string.IsNullOrEmpty(name)) names.Add(name);
#else
			foreach (var joystick in Rewired.ReInput.controllers.Joysticks) names.Add(joystick.name);
#endif
			return names.ToArray();
		}

		public static bool AreResolutionSame(Resolution r1, Resolution r2)
		{
			return r1.width == r2.width && r1.height == r2.height;
		}

		private static string[] GetMonitorNames()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			return new[] { "Primary" };
#else
			var displays = new List<DisplayInfo>();
			Screen.GetDisplayLayout(displays);
			return displays.ConvertAll(display => display.name).ToArray();
#endif
		}

		public static string SettingNameToReadable(string s)
		{
			string readable = s.Replace('_', ' ');
			return char.ToUpper(readable[0]) + readable.Substring(1);
		}

		public static string SettingNameToReadable(string s, CFSettings cfSettings)
		{
			if (s == "swap_menu_ab") return AndroidControlText.SwapMenuButtons;
			if (s == "touch_controls") return AndroidControlText.TouchControls;
			string key = CheckSettingName(s);
			string table = cfSettings is CFGameSettings ? "SettingsGame" :
				cfSettings is CFVideoSettings ? "SettingsVideo" :
				cfSettings is CFControlSettings ? "SettingsControls" :
				cfSettings is ConfigSettingsTypes.CFAudioSettings ? "SettingsAudio" :
				cfSettings is Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes.CFVisualsSettings ? "SettingsVisuals" : null;
			return table == null ? SettingNameToReadable(key) : new UnityEngine.Localization.LocalizedString(table, key).GetLocalizedString();
		}

		public static string GetSettingEnumLocalized(string settingEnumValue)
		{
			var localization = Assets.Scripts.UI.Localization.LocalizationUtility.HasLocalizedString("SettingEnums", settingEnumValue);
			if (localization) return new UnityEngine.Localization.LocalizedString("SettingEnums", settingEnumValue).GetLocalizedString();
			string language = Assets.Scripts.UI.Localization.LocalizationUtility.GetLanguageName(settingEnumValue);
			return string.IsNullOrEmpty(language) ? settingEnumValue : language;
		}

		public static float GetSliderIncrement(string settingName)
		{
			return settingName == "sensitivity" || settingName == "controller_sensitivity" ? 0.01f : -1f;
		}
	}
}
