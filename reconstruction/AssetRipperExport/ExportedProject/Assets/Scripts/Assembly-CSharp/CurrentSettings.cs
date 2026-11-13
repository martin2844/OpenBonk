using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;

public class CurrentSettings : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CDoUpdateResolution_003Ed__17 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public int index;

		public CurrentSettings _003C_003E4__this;

		public int oldValue;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CDoUpdateResolution_003Ed__17(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	[CompilerGenerated]
	private sealed class _003CDoUpdateTargetMonitor_003Ed__22 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public int index;

		public CurrentSettings _003C_003E4__this;

		public int oldValue;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CDoUpdateTargetMonitor_003Ed__22(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	[CompilerGenerated]
	private sealed class _003CMoveToPrimaryDisplay_003Ed__27 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CMoveToPrimaryDisplay_003Ed__27(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public AudioMixer audioMixer;

	public Material warningMaterial;

	public static CurrentSettings Instance;

	public static Action<EControllerType> A_ControllerTypeChanged;

	public static Action<string, object, object> A_SettingUpdated;

	public static Action<int> A_ResolutionChanged;

	private Resolution resolutionBeforeMonitorChange;

	private static bool firstResolutionChange;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		SaveManager.A_SavesLoaded += UpdateSave;
		if (SaveManager.loaded) UpdateSave();
	}

	private void Start()
	{
		Invoke(nameof(RefreshAudioMixer), 0f);
	}

	private void OnDestroy()
	{
		if (Instance == this) { SaveManager.A_SavesLoaded -= UpdateSave; Instance = null; }
	}

	public void UpdateSave()
	{
		var config = SaveManager.Instance.config;
		InitializeFirstLanguage(config);
		CFSettings[] categories = { config.cfGameSettings, config.cfVideoSettings, config.cfAudioSettings, config.cfControlSettings, config.cfVisualsSettings, config.cfOtherSettings };
		foreach (var category in categories)
			foreach (var field in category.GetType().GetFields())
			{
				if (field.Name == "language" && !config.hasSelectedLanguage) continue;
				BetterUpdateCfSettings(field.Name, field.GetValue(category), category);
			}
		SaveManager.Instance.SaveConfig();
		if (Application.platform == RuntimePlatform.OSXPlayer)
		{
			QualitySettings.vSyncCount = 1;
			Application.targetFrameRate = 60;
		}
	}

	private static void InitializeFirstLanguage(ConfigSaveFile config)
	{
		if (!config.hasSelectedLanguage)
		{
			var locales = LocalizationSettings.AvailableLocales?.Locales;
			var selected = LocalizationSettings.SelectedLocale;
			int index = locales == null || selected == null ? -1 : locales.IndexOf(selected);
			if (index >= 0)
			{
				config.cfGameSettings.language = index;
				config.hasSelectedLanguage = true;
			}
		}
	}

	public void BetterUpdateCfSettings(string settingName, object value, CFSettings cfSettings)
	{
		var field = cfSettings.GetType().GetField(settingName);
		object oldValue = field.GetValue(cfSettings);
		field.SetValue(cfSettings, value);
		OnSettingUpdated(settingName, value, oldValue);
	}

	private void OnSettingUpdated(string name, object value, object oldValue)
	{
		switch (name)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			case "resolution": UpdateResolution((int)value, (int)oldValue); break;
#endif
			case "language": UpdateLanguage((int)value); break;
			case "skip_chest_animation": UpdateSkipChestAnimation((int)value); break;
			case "fps_limit": UpdateMaxFps((int)(float)value); break;
			case "vsync": UpdateVSync((int)value); break;
			case "input_delay": UpdateInputDelay((int)(float)value); break;
			case "fullscreen_mode": UpdateFullscreenMode((int)value); break;
			case "shadow_quality": UpdateShadowQuality((int)value); break;
			case "shadow_resolution": UpdateShadowResolution((int)value); break;
			case "texture_quality": UpdateTextureQuality((int)value); break;
			case "anti_aliasing": UpdateAntiAliasing((int)value); break;
			case "soft_particles": UpdateSoftParticles((int)value); break;
			case "warning_color": UpdateWarningColor((int)value); break;
			case "master_volume": UpdateMixerVolume((float)value, "MasterVolume"); break;
			case "music": UpdateMixerVolume((float)value, "MusicVolume"); break;
			case "game_sfx": UpdateMixerVolume((float)value, "GameSfxVolume"); break;
			case "ambience": UpdateMixerVolume((float)value, "AmbienceVolume"); break;
			case "ui": UpdateMixerVolume((float)value, "UiVolume"); break;
			case "controller_type": A_ControllerTypeChanged?.Invoke((EControllerType)(int)value); break;
		}
		A_SettingUpdated?.Invoke(name, oldValue, value);
	}

	private void UpdateInputDelay(int i)
	{
		QualitySettings.maxQueuedFrames = Mathf.Clamp(i, 0, 6);
	}

	private void UpdateLanguage(int i)
	{
		var locales = LocalizationSettings.AvailableLocales?.Locales;
		if (locales != null && i >= 0 && i < locales.Count && locales[i] != null && !string.IsNullOrEmpty(locales[i].Identifier.Code))
			LocalizationSettings.SelectedLocale = locales[i];
	}

	private void UpdateSkipChestAnimation(int i)
	{
		if (i == 0) return;
		var preferences = SaveManager.Instance?.config?.preferences;
		var windows = AlwaysUi.Instance != null ? AlwaysUi.Instance.dynamicWindows : null;
		if (preferences == null || preferences.hasShownWarningForChestSkip || windows == null) return;
		preferences.hasShownWarningForChestSkip = true;
		windows.NewWindow(
			Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("MainMenuOther", "WARNING"),
			Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("DynamicWindows", "CHESTSKIPWARNING"));
	}

	private void UpdateWarningColor(int i)
	{
		if (warningMaterial == null) return;
		var color = Assets.Scripts.Utility.MyColorUtility.GetWarningColor((EHpBarColor)i);
		color.a = 0.5f;
		warningMaterial.SetColor("_MainColor", color);
	}

	private void UpdateResolution(int index, int oldValue)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		var modes = ConfigSettingsUtility.GetMyResolutions();
		if (index < 0 || index >= modes.Length) return;
		var mode = modes[index];
		if (mode.width <= 0 || mode.height <= 0) return;
		Screen.SetResolution(mode.width, mode.height, Screen.fullScreenMode);
		A_ResolutionChanged?.Invoke(index);
#endif
	}

	private void UpdateMonitor(int index, int oldValue)
	{
	}

	[IteratorStateMachine(typeof(_003CDoUpdateResolution_003Ed__17))]
	private IEnumerator DoUpdateResolution(int index, int oldValue)
	{
		return null;
	}

	private void AcceptResolution(int newValue)
	{
	}

	private void RevertResolution(int oldValue)
	{
	}

	[IteratorStateMachine(typeof(_003CDoUpdateTargetMonitor_003Ed__22))]
	private IEnumerator DoUpdateTargetMonitor(int index, int oldValue)
	{
		return null;
	}

	private void AcceptTargetMonitor(int newValue)
	{
	}

	private void RevertTargetMonitor(int oldValue)
	{
	}

	private void SetResolution(int index)
	{
	}

	[IteratorStateMachine(typeof(_003CMoveToPrimaryDisplay_003Ed__27))]
	private IEnumerator MoveToPrimaryDisplay()
	{
		return null;
	}

	private void UpdateFullscreenMode(int i)
	{
		Screen.fullScreenMode = (FullScreenMode)i;
	}

	private void UpdateVSync(int i)
	{
		QualitySettings.vSyncCount = i;
	}

	private void UpdateMaxFps(int i)
	{
		Application.targetFrameRate = i;
	}

	private void UpdateShadowQuality(int i)
	{
		QualitySettings.shadows = (UnityEngine.ShadowQuality)i;
		QualitySettings.shadowCascades = i * 2;
	}

	private void UpdateShadowResolution(int i)
	{
		QualitySettings.shadowResolution = (UnityEngine.ShadowResolution)i;
		QualitySettings.shadowDistance = i * 40f;
	}

	private void UpdateTextureQuality(int i)
	{
		QualitySettings.globalTextureMipmapLimit = 3 - i;
	}

	private void UpdateAntiAliasing(int i)
	{
		QualitySettings.antiAliasing = i == 0 ? 0 : (int)Mathf.Pow(2f, i);
	}

	private void UpdateSoftParticles(int b)
	{
		QualitySettings.softParticles = b == 1;
	}

	public void UpdateMixerVolume(float f, string groupVolumeName)
	{
		audioMixer.SetFloat(groupVolumeName, SliderToDb(f));
	}

	private float SliderToDb(float sliderValue)
	{
		return sliderValue <= 0.0001f ? -80f : 20f * Mathf.Log10(sliderValue);
	}

	private void RefreshAudioMixer()
	{
		var audio = SaveManager.Instance != null && SaveManager.Instance.config != null
			? SaveManager.Instance.config.cfAudioSettings : null;
		if (audio == null) return;
		foreach (var field in audio.GetType().GetFields())
			BetterUpdateCfSettings(field.Name, field.GetValue(audio), audio);
	}
}
