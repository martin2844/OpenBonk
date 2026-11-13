#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewired.Localization
{
	[AddComponentMenu("Rewired/Localization/Localized String Provider")]
	public class LocalizedStringProvider : LocalizedStringProviderBase
	{
		[SerializeField]
		[Tooltip("A JSON file containing localizied string key value pairs.")]
		private TextAsset _localizedStringsFile;

		[NonSerialized]
		private Dictionary<string, string> _dictionary;

		[NonSerialized]
		private bool _initialized;

		protected virtual Dictionary<string, string> dictionary
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual TextAsset localizedStringsFile
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected override bool initialized => false;

		protected override bool Initialize()
		{
			return false;
		}

		protected virtual bool TryLoadLocalizedStringData()
		{
			return false;
		}

		protected override bool TryGetLocalizedString(string key, out string result)
		{
			result = null;
			return false;
		}
	}
}

#endif
