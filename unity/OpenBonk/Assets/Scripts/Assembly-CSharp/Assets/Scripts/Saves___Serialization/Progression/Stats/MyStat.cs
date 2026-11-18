using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assets.Scripts.Saves___Serialization.Progression.Stats
{
	[Serializable]
	public class MyStat
	{
		[JsonExtensionData, UnityEngine.Scripting.Preserve]
		private IDictionary<string, JToken> extraFields;

		public string name;

		public float value;

		public MyStat(string name, float value)
		{
			this.name = name;
			this.value = value;
		}
	}
}
