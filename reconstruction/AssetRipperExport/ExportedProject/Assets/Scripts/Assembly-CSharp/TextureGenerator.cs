using UnityEngine;

public static class TextureGenerator
{
	public static Texture2D textureFromColorMap(Color[] colorMap, int width, int height)
	{
		var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colorMap);
		texture.Apply();
		return texture;
	}

	public static Texture2D TextureFromHeightMap(float[,] heightMap)
	{
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);
		var colors = new Color[width * height];
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
			colors[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
		return textureFromColorMap(colors, width, height);
	}

	public static Texture2D ColorTextureFromHeightMap(float[,] heightMap, TextureData textureData)
	{
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);
		var colors = new Color[width * height];
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
			colors[y * width + x] = GetColor(heightMap[x, y], textureData);
		return textureFromColorMap(colors, width, height);
	}

	public static Color GetColor(float height, TextureData textureData)
	{
		for (int i = textureData.layers.Length - 1; i >= 0; i--)
			if (height - 0.1f > textureData.layers[i].startHeight)
				return textureData.layers[i].tint;
		return textureData.layers[0].tint;
	}
}
