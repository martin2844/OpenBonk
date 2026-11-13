using UnityEngine;

namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	public class GaussianHeightBias : IHeightBiasStrategy
	{
		public float CalculateBias(int x, int y, int size, float outerBiasStrength, float strictness)
		{
			float center = (size - 1) * 0.5f;
			float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
			float maxDistance = Vector2.Distance(Vector2.zero, new Vector2(center, center));
			float normalized = distance / maxDistance;
			return Mathf.Exp(-(normalized * normalized * strictness)) * outerBiasStrength;
		}
	}
}
