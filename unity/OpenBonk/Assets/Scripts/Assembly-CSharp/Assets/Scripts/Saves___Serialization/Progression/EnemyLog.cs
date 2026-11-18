using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assets.Scripts.Saves___Serialization.Progression
{
	[Serializable]
	public class EnemyLog
	{
		[JsonExtensionData, UnityEngine.Scripting.Preserve]
		private IDictionary<string, JToken> extraFields;

		public int kills;

		public int currentChallengeTier;
	}
}
