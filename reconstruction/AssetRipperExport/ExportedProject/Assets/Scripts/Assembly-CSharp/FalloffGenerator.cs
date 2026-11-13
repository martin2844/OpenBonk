using UnityEngine;

public static class FalloffGenerator
{
	public static float[,] GenerateFalloffMap(int size)
	{
		var map = new float[size, size];
		for (int x = 0; x < size; x++)
		{
			float normalizedX = (float)x / size * 2f - 1f;
			for (int y = 0; y < size; y++)
			{
				float normalizedY = (float)y / size * 2f - 1f;
				map[x, y] = Evaluate(Mathf.Max(Mathf.Abs(normalizedX), Mathf.Abs(normalizedY)));
			}
		}
		return map;
	}

	private static float Evaluate(float value)
	{
		float numerator = Mathf.Pow(value, 3f);
		return numerator / (Mathf.Pow(value, 3f) + Mathf.Pow(2.2f - 2.2f * value, 3f));
	}
}
