using System;
using UnityEngine;

[CreateAssetMenu]
public class TextureData : UpdatableData
{
	[Serializable]
	public class Layer
	{
		public Texture2D texture;

		public Color tint;

		[Range(0f, 1f)]
		public float tintStrength;

		[Range(0f, 1f)]
		public float startHeight;

		[Range(0f, 1f)]
		public float blendStrength;

		public float textureScale;

		public TerrainType type;
	}

	public enum TerrainType
	{
		Water = 0,
		Sand = 1,
		Grass = 2
	}

	private const int textureSize = 512;

	private const TextureFormat textureFormat = TextureFormat.RGB565;

	public Layer[] layers;

	private float savedMinHeight;

	private float savedMaxHeight;

	public void ApplyToMaterial(Material material)
	{
		int count = layers.Length;
		var colors = new Color[count];
		var starts = new float[count];
		var blends = new float[count];
		var strengths = new float[count];
		var scales = new float[count];
		var textures = new Texture2D[count];
		for (int i = 0; i < count; i++)
		{
			colors[i] = layers[i].tint;
			starts[i] = layers[i].startHeight;
			blends[i] = layers[i].blendStrength;
			strengths[i] = layers[i].tintStrength;
			scales[i] = layers[i].textureScale;
			textures[i] = layers[i].texture;
		}
		material.SetInt("layerCount", count);
		material.SetColorArray("baseColours", colors);
		material.SetFloatArray("baseStartHeights", starts);
		material.SetFloatArray("baseBlends", blends);
		material.SetFloatArray("baseColourStrength", strengths);
		material.SetFloatArray("baseTextureScales", scales);
		material.SetTexture("baseTextures", GenerateTextureArray(textures));
	}

	public void UpdateMeshHeights(float minHeight, float maxHeight)
	{
		savedMinHeight = minHeight;
		savedMaxHeight = maxHeight;
	}

	private Texture2DArray GenerateTextureArray(Texture2D[] textures)
	{
		var textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
		for (int i = 0; i < textures.Length; i++)
			if (textures[i]) textureArray.SetPixels(textures[i].GetPixels(), i);
		textureArray.Apply();
		return textureArray;
	}
}
