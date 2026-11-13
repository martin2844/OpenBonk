using System;
using UnityEngine.Localization;

namespace Assets.Scripts.Saves___Serialization.Progression
{
	[Serializable]
	public class LocalizationKey
	{
		public string key;

		public LocalizedString localizedValue;

		public LocalizationKey(string key, LocalizedString localizedValue)
		{
			this.key = key;
			this.localizedValue = localizedValue;
		}
	}
}
