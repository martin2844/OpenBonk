using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	public enum DrawMode
	{
		NoiseMap = 0,
		Mesh = 1,
		FalloffMap = 2,
		ColorMap = 3
	}

	public DrawMode drawMode;

	public TerrainData terrainData;

	public NoiseData noiseData;

	public TextureData textureData;

	[Range(0f, 6f)]
	public int levelOfDetail;

	private static int seed = 105;

	public bool autoUpdate;

	private float[,] falloffMap;

	public static int mapChunkSize = 241;

	public static int worldScale = 12;

	public GameObject mesh;

	public static MapGenerator Instance;

	public static float[,] staticNoiseMap;

	public float[,] heightMap;

	public MapDisplay display { get; private set; }

	private void Awake()
	{
		Instance = this;
		textureData.UpdateMeshHeights(terrainData.minHeight, terrainData.maxHeight);
	}

	private void OnValuesUpdated()
	{
	}

	public void GenerateMap(MapData mapData, StageData stageData, int seed = 105)
	{
		mesh.transform.localScale = new Vector3(4f * stageData.proceduralMapScale,
			8f * stageData.proceduralMapScale, 4f * stageData.proceduralMapScale);
		terrainData = stageData.proceduralTerrainData;
		noiseData = stageData.proceduralNoiseData;
		GenerateMap(seed);
	}

	public void GenerateMap(int seed = 105)
	{
		heightMap = GeneratePerlinNoiseMap(seed);
		textureData.UpdateMeshHeights(terrainData.minHeight, terrainData.maxHeight);
		display = FindObjectOfType<MapDisplay>();
		switch (drawMode)
		{
			case DrawMode.NoiseMap:
				display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
				break;
			case DrawMode.Mesh:
				display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap,
					terrainData.heightMultiplier, terrainData.heightCurve, levelOfDetail));
				break;
			case DrawMode.FalloffMap:
				display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
				break;
			case DrawMode.ColorMap:
				display.DrawTexture(TextureGenerator.ColorTextureFromHeightMap(heightMap, textureData));
				break;
		}
		textureData.UpdateMeshHeights(terrainData.minHeight, terrainData.maxHeight);
	}

	public float[,] GeneratePerlinNoiseMap(NoiseData noiseData, int seed, bool useFalloffMap)
	{
		float[,] map = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed,
			noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity,
			noiseData.blend, noiseData.blendStrength, noiseData.offset);
		if (useFalloffMap)
		{
			if (falloffMap == null) falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
			for (int y = 0; y < mapChunkSize; y++)
			for (int x = 0; x < mapChunkSize; x++)
				if (terrainData.useFalloff) map[x, y] = Mathf.Clamp01(map[x, y] - falloffMap[x, y]);
		}
		return map;
	}

	public float[,] GeneratePerlinNoiseMap(int seed)
	{
		return GeneratePerlinNoiseMap(noiseData, seed, terrainData.useFalloff);
	}

	public void DrawMapInEditor()
	{
	}

	private void OnValidate()
	{
	}
}
