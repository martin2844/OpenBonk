using System;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSettingsTypes;

namespace Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves
{
	[Serializable]
	public class ConfigSaveFile
	{
		public int settingsVersion;

		public bool hasSelectedLanguage;

		public CFGameSettings cfGameSettings = new CFGameSettings();

		public CFVideoSettings cfVideoSettings = new CFVideoSettings();

		public CFAudioSettings cfAudioSettings = new CFAudioSettings();

		public CFControlSettings cfControlSettings = new CFControlSettings();

		public CFVisualsSettings cfVisualsSettings = new CFVisualsSettings();

		public CFOtherSettings cfOtherSettings = new CFOtherSettings();

		public ConfigSettingsExtra otherSettings = new ConfigSettingsExtra();

		public Preferences preferences = new Preferences();

		public void Init()
		{
			preferences.Init();
		}
	}
}
