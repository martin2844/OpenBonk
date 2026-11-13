using System;

namespace Assets.Scripts.Saves___Serialization.Progression.Stats
{
	[Serializable]
	public class MyStat
	{
		public string name;

		public float value;

		public MyStat(string name, float value)
		{
			this.name = name;
			this.value = value;
		}
	}
}
