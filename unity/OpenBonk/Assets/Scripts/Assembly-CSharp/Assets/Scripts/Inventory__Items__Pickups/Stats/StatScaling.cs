namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public static class StatScaling
	{
		public static float HyperbolicScaling(float input, float maxValue = 1f, float scaling = 0.5f)
		{
			return input / (scaling + input) * maxValue;
		}

		private static float PowerScaling(float inputValue, float maxInputValue, float maxValue, float diminishingEffect)
		{
            float normalized = UnityEngine.Mathf.Max(0f, UnityEngine.Mathf.Min(inputValue, maxInputValue)) / maxInputValue;
            return (1f - UnityEngine.Mathf.Pow(1f - normalized, diminishingEffect)) * maxValue;
		}
	}
}
