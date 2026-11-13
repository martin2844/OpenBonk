using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;

namespace Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes
{
	public class CFVisualsSettings : CFSettings
	{
		public int damage_numbers = 1;

		public int damage_flash = 1;

		public int low_hp_effects = 1;

		public float particle_opacity = 1f;

		public int particle_auto_opacity = 1;

		public int warning_color;

		public int xp_gold_hud = 1;

		public List<SettingHeader> GetHeaders()
		{
			return new List<SettingHeader> { new SettingHeader(0, "VISUALS"), new SettingHeader(6, "ADVANCED") };
		}
	}
}
