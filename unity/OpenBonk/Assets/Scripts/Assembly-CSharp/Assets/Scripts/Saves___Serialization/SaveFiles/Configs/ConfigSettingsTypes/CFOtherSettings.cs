using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;

namespace Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes
{
	public class CFOtherSettings : CFSettings
	{
		public List<SettingHeader> GetHeaders()
		{
			return new List<SettingHeader> { new SettingHeader(0, "SAVES") };
		}
	}
}
