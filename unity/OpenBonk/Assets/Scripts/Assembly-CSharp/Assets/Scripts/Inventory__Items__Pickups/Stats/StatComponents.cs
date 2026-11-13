namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public class StatComponents
	{
		public bool hasModifications;

		public float baseValue { get; private set; }

		public float additiveValue { get; private set; }

		public float multiplicativeValue { get; private set; }

		public void Recycle()
		{
			baseValue = 0f;
			additiveValue = 0f;
			multiplicativeValue = 1f;
			hasModifications = false;
		}

		public void SetValues(float baseValues, float additiveValues, float multiplicativeValues)
		{
			baseValue = baseValues;
			additiveValue = additiveValues;
			multiplicativeValue = multiplicativeValues;
		}

		public float GetFinalValue(StatComponents other)
		{
			return (baseValue + other.baseValue) * (additiveValue + other.additiveValue) *
				(multiplicativeValue * other.multiplicativeValue);
		}

		public void AddMultiplier(float value)
		{
			hasModifications = true;
			multiplicativeValue *= value;
		}

		public void AddAdditive(float value)
		{
			hasModifications = true;
			additiveValue += value;
		}

		public void AddFlat(float value)
		{
			hasModifications = true;
			baseValue += value;
		}
	}
}
