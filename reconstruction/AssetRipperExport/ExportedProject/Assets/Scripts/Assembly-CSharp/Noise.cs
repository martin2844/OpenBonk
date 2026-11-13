using UnityEngine;

public static class Noise
{
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, float blend, float blendStrength, Vector2 offset)
	{
		var map = new float[mapWidth, mapHeight];
		var random = new ConsistentRandom(seed);
		var octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; i++)
			octaveOffsets[i] = new Vector2(random.Next(-100000, 100000) + offset.x,
				random.Next(-100000, 100000) + offset.y);

		if (scale <= 0f) scale = 0.0001f;
		float halfWidth = mapWidth * 0.5f;
		float halfHeight = mapHeight * 0.5f;
		float min = float.MaxValue;
		float max = float.MinValue;
		for (int y = 0; y < mapHeight; y++)
		for (int x = 0; x < mapWidth; x++)
		{
			float amplitude = 1f;
			float frequency = 1f;
			float height = 0f;
			float baseX = (x - halfWidth) / scale;
			float baseY = (y - halfHeight) / scale;
			for (int i = 0; i < octaves; i++)
			{
				float sampleX = baseX * frequency + octaveOffsets[i].x;
				float sampleY = baseY * frequency + octaveOffsets[i].y;
				float blendX = baseX * (frequency * blend) + octaveOffsets[i].x;
				float blendY = baseY * (frequency * blend) + octaveOffsets[i].y;
				float perlin = Mathf.PerlinNoise(sampleX + blendX * blendStrength,
					sampleY + blendY * blendStrength) * 2f - 1f;
				height += perlin * amplitude;
				amplitude *= persistance;
				frequency *= lacunarity;
			}
			if (height > max) max = height;
			else if (height < min) min = height;
			map[x, y] = height;
		}
		for (int y = 0; y < mapHeight; y++)
		for (int x = 0; x < mapWidth; x++)
			map[x, y] = Mathf.InverseLerp(min, max, map[x, y]);
		return map;
	}
}
