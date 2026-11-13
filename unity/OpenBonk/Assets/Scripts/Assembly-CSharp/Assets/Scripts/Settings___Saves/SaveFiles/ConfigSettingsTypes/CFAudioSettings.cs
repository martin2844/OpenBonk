using System.Collections.Generic;

namespace Assets.Scripts.Settings___Saves.SaveFiles.ConfigSettingsTypes
{
	public class CFAudioSettings : CFSettings
	{
		public float master_volume = 0.69f;

		public float music = 0.48f;

		public float game_sfx = 1f;

		public float ambience = 1f;

		public float ui = 1f;

		public float xp_and_gold = 1f;

		public List<SettingHeader> GetHeaders()
		{
			return new List<SettingHeader> { new SettingHeader(0, "AUDIO") };
		}
	}
}
