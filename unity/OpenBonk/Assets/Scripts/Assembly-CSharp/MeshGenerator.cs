using UnityEngine;

public static class MeshGenerator
{
	public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
	{
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);
		float halfWidth = (width - 1) * 0.5f;
		float halfHeight = (height - 1) * 0.5f;
		int increment = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
		int verticesPerLine = (width - 1) / increment + 1;
		var meshData = new MeshData(verticesPerLine, verticesPerLine);
		int vertexIndex = 0;
		for (int y = 0; y < height; y += increment)
		for (int x = 0; x < width; x += increment)
		{
			meshData.vertices[vertexIndex] = new Vector3(x - halfWidth,
				heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, halfHeight - y);
			meshData.uvs[vertexIndex] = new Vector2((float)x / width, (float)y / height);
			if (x < width - 1 && y < height - 1)
			{
				meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
				meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
			}
			vertexIndex++;
		}
		return meshData;
	}
}
