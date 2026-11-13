using System.Collections.Generic;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEngine;

public class ProceduralTileGeneration : MonoBehaviour
{
	public int debugSeed;

	public GameObject stairsMesh;

	public GameObject flatTile;

	public GameObject slopeTile;

	public GameObject ceilingTile;

	public GameObject wallFlat;

	public GameObject wallLeftUp;

	public GameObject wallLeftDown;

	public GameObject wallLeftCross;

	public List<GameObject> tiles;

	public List<GameObject> flatTiles;

	public List<GameObject> fillTiles;

	public GameObject collider;

	private StageData currentStage;

	private MapParameters currentMapParameters;

	public StageData testStage;

	public MapParameters testMapParameters;

	public GameObject newRoot;

	public GameObject tilesParent;

	public ProceduralTile[][] proceduralTiles;

	public Vector3 Generate(out Vector3 firstTileDirection, StageData stageData, MapParameters mapParameters, bool useDebugSeed = false)
	{
		currentStage = stageData;
		currentMapParameters = mapParameters;
		ClearTiles();
		proceduralTiles = new ProceduralTile[mapParameters.size][];
		tiles = new List<GameObject>();
		flatTiles = new List<GameObject>();
		for (int i = 0; i < mapParameters.size; i++)
			proceduralTiles[i] = new ProceduralTile[mapParameters.size];
		int seed = useDebugSeed ? debugSeed : Random.Range(0, int.MaxValue);
		NodeTree root = Maze.Generate(mapParameters.size, mapParameters.size, seed);
		debugSeed = seed;
		MazeHeightGenerator.GenerateHeight(this, root, seed, mapParameters);
		Vector2Int firstDirection = root.children[0].position - root.position;
		firstTileDirection = new Vector3(firstDirection.x, 0f, firstDirection.y);
		FillHoles();
		FillEdges();
		var allTiles = new List<GameObject>(tiles);
		allTiles.AddRange(fillTiles);
		if (collider != null) DestroyImmediate(collider);
		collider = MeshColliderCombiner.CombineMeshes(allTiles);
		if (tilesParent != null) DestroyImmediate(tilesParent);
		tilesParent = new GameObject("Tiles Parent");
		foreach (GameObject tile in allTiles) tile.transform.SetParent(tilesParent.transform);
		StaticBatchingUtility.Combine(tilesParent);
		return TilePositionToWorldPosition(root.position);
	}

	public Vector3 TilePositionToWorldPosition(Vector2Int pos)
	{
		int width = currentMapParameters.scaledTileWidth;
		int offset = width / 2 - (currentMapParameters.size * width) / 2;
		return new Vector3(pos.x * width + offset, 0f, pos.y * width + offset);
	}

	public void FillHoles()
	{
		if (fillTiles != null)
			foreach (GameObject tile in fillTiles) DestroyImmediate(tile);
		fillTiles = new List<GameObject>();
		for (int x = 0; x < currentMapParameters.size; x++)
			for (int y = 0; y < currentMapParameters.size; y++)
			{
				FillHole(new Vector2Int(x, y), new Vector2Int(x + 1, y));
				FillHole(new Vector2Int(x, y), new Vector2Int(x, y + 1));
			}
	}

	private void FillHole(Vector2Int pos1, Vector2Int pos2)
	{
		int max = currentMapParameters.size - 1;
		if (pos1.x > max || pos1.y > max || pos2.x > max || pos2.y > max) return;
		ProceduralTile first = proceduralTiles[pos1.x][pos1.y];
		ProceduralTile second = proceduralTiles[pos2.x][pos2.y];
		Vector2Int direction = pos2 - pos1;
		TileEdge edge1 = first.GetEdge(direction);
		TileEdge edge2 = second.GetEdge(-direction);
		int height = first.posY + edge1.offsetHeight;
		int difference = second.posY + edge2.offsetHeight - height;
		ETileEdgeType firstType = edge1.type;
		ETileEdgeType secondType = edge2.type;

		if (firstType == ETileEdgeType.Flat && secondType == ETileEdgeType.Flat)
		{
			if (difference == 0) return;
			bool negative = difference < 0;
			int count = Mathf.Abs(difference);
			int baseHeight = height + (negative ? -1 : 0);
			for (int i = 0; i < count; i++)
			{
				EFillType type = count == 1 ? EFillType.Both
					: i == 0 ? (negative ? EFillType.Top : EFillType.Bottom)
					: i == count - 1 ? (negative ? EFillType.Bottom : EFillType.Top)
					: EFillType.Nothing;
				InstantiateFillTile(pos1, baseHeight + i * (negative ? -1 : 1), wallFlat,
					negative ? -direction : direction, direction, type);
			}
			return;
		}

		if ((firstType == ETileEdgeType.SlopeRight || firstType == ETileEdgeType.SlopeLeft) && secondType == ETileEdgeType.Flat)
		{
			if (difference != 0)
				FillWallSlopeToFlat(difference, pos1, height, direction, true);
			EFillType type = difference < 0 ? EFillType.Top
				: difference < 2 ? EFillType.Both : EFillType.Bottom;
			InstantiateFillTile(pos1, height, difference <= 0 ? wallLeftDown : wallLeftUp,
				direction, direction, type, firstType == ETileEdgeType.SlopeRight);
			return;
		}

		if (firstType == ETileEdgeType.Flat && (secondType == ETileEdgeType.SlopeRight || secondType == ETileEdgeType.SlopeLeft))
		{
			if (difference != 0)
				FillWallSlopeToFlat(difference, pos1, height, direction, false);
			EFillType type = difference < -1 ? EFillType.Bottom
				: difference <= 0 ? EFillType.Both : EFillType.Top;
			InstantiateFillTile(pos1, second.posY, difference < 0 ? wallLeftUp : wallLeftDown,
				-direction, direction, type, secondType == ETileEdgeType.SlopeRight);
			return;
		}

		if ((firstType != ETileEdgeType.SlopeRight && firstType != ETileEdgeType.SlopeLeft) ||
			(secondType != ETileEdgeType.SlopeRight && secondType != ETileEdgeType.SlopeLeft)) return;
		bool sameSlope = firstType == secondType;
		if (difference == 0)
		{
			if (sameSlope)
				InstantiateFillTile(pos1, height, wallLeftCross, direction, direction,
					EFillType.Bottom, firstType == ETileEdgeType.SlopeRight);
			return;
		}
		bool ascending = difference > 0;
		EFillType firstFill = ascending ? EFillType.Bottom : EFillType.Top;
		EFillType secondFill = ascending ? EFillType.Top : EFillType.Bottom;
		GameObject firstPrefab = ascending ? wallLeftUp : wallLeftDown;
		GameObject secondPrefab = ascending ? wallLeftDown : wallLeftUp;
		InstantiateFillTile(pos1, height, firstPrefab, direction, direction,
			firstFill, firstType == ETileEdgeType.SlopeRight);
		if (!sameSlope || secondPrefab != null)
			InstantiateFillTile(pos1, height + difference, secondPrefab, -direction, direction,
				secondFill, secondType == ETileEdgeType.SlopeRight);
		if (difference < -1 || difference > 1)
			FillWallSlopToSlop(difference, pos1, height, direction);
	}

	private void FillWallSlopeToFlat(int heightDifference, Vector2Int position, int height, Vector2Int globalDir, bool isFirstPieceSloped, bool useEdgeTextures = false)
	{
		bool negative = heightDifference < 0;
		Vector2Int direction = negative ? -globalDir : globalDir;
		int count = Mathf.Abs(heightDifference);
		if (negative && !isFirstPieceSloped) count--;
		int start = heightDifference > 0 && isFirstPieceSloped ? 1 : 0;
		int baseHeight = height + (negative ? -1 : 0);
		int step = negative ? -1 : 1;
		for (int i = start; i < count; i++)
		{
			EFillType type = EFillType.Nothing;
			if (isFirstPieceSloped && i == count - 1)
				type = negative ? EFillType.Bottom : EFillType.Top;
			else if (!isFirstPieceSloped && i == start)
				type = negative ? EFillType.Top : EFillType.Bottom;
			InstantiateFillTile(position, baseHeight + i * step, wallFlat,
				direction, globalDir, type, false, useEdgeTextures);
		}
	}

	private void FillWallSlopToSlop(int heightDifference, Vector2Int position, int height, Vector2Int globalDir)
	{
		if (heightDifference > 0) heightDifference--;
		else if (heightDifference < 0) heightDifference++;
		bool negative = heightDifference < 0;
		height += negative ? -1 : 1;
		Vector2Int direction = negative ? -globalDir : globalDir;
		int step = negative ? -1 : 1;
		for (int i = 0; i < Mathf.Abs(heightDifference); i++)
			InstantiateFillTile(position, height + i * step, wallFlat,
				direction, globalDir, EFillType.Nothing);
	}

	public GameObject InstantiateTile(Vector2Int pos, int height, GameObject tilePrefab, Vector3 direction, Vector3 parentDir)
	{
		int width = currentMapParameters.scaledTileWidth;
		int offset = width / 2 - (currentMapParameters.size * width) / 2;
		Vector3 position = new Vector3(pos.x * width + offset,
			height * currentMapParameters.scaledTileHeight, pos.y * width + offset);
		bool isFlat = tilePrefab == flatTile;
		Quaternion rotation = Quaternion.LookRotation(isFlat || direction == Vector3.zero ? Vector3.forward : direction);
		GameObject tile = Instantiate(tilePrefab, position, rotation);
		proceduralTiles[pos.x][pos.y] = tile.GetComponent<ProceduralTile>();
		proceduralTiles[pos.x][pos.y].SetPosY(height, currentStage, isFlat, direction, parentDir);
		tiles.Add(tile);
		tile.transform.localScale = tilePrefab.transform.localScale * currentMapParameters.scale;
		if (tilePrefab == flatTile) flatTiles.Add(tile);
		if (!isFlat && currentStage.m_stairs != null && tilePrefab != ceilingTile)
		{
			GameObject stairs = Instantiate(stairsMesh, position, rotation);
			stairs.transform.SetParent(tile.transform);
			stairs.transform.localScale = Vector3.one;
			stairs.GetComponent<TileStairs>().Set(currentStage);
		}
		return tile;
	}

	public GameObject InstantiateTile(Vector2Int pos, int height, int yDir, Vector2Int direction)
	{
		return InstantiateTile(pos, height, yDir == 0 ? flatTile : slopeTile,
			new Vector3(direction.x, 0f, direction.y), Vector3.zero);
	}

	private void InstantiateFillTile(Vector2Int pos, int height, GameObject tilePrefab, Vector2Int direction, Vector2Int globalDirection, EFillType fillType, bool flip = false, bool useEdgeTexture = false)
	{
		int scale = currentMapParameters.scale;
		int width = currentMapParameters.scaledTileWidth;
		int offset = width / 2 - (currentMapParameters.size * width) / 2;
		Vector3 position = new Vector3(pos.x * width + globalDirection.x * scale + offset,
			height * scale, pos.y * width + globalDirection.y * scale + offset);
		GameObject tile = Instantiate(tilePrefab, position,
			Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y)));
		tile.GetComponent<FillTile>().SetFillType(fillType, currentStage, useEdgeTexture);
		fillTiles.Add(tile);
		Vector3 localScale = tilePrefab.transform.localScale * scale;
		if (flip) localScale.x = -localScale.x;
		tile.transform.localScale = localScale;
	}

	private void FillEdges()
	{
		switch (currentStage.mapEdgeFillType)
		{
			case MapEdgeFillType.Island: FillEdgesIsland(); break;
			case MapEdgeFillType.Walls: FillEdgesWalls(); break;
			case MapEdgeFillType.Trees: FillEdgesTrees(); break;
		}
	}

	private void FillEdgesIsland()
	{
		int lowest = int.MaxValue;
		foreach (var row in proceduralTiles)
			foreach (var tile in row)
				lowest = Mathf.Min(lowest, tile.posY);
		int target = lowest - 1;
		int last = currentMapParameters.size - 1;
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(i, 0), target - proceduralTiles[i][0].posY, true, Vector2Int.down);
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(last, i), target - proceduralTiles[last][i].posY, true, Vector2Int.right);
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(i, last), target - proceduralTiles[i][last].posY, true, Vector2Int.up);
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(0, i), target - proceduralTiles[0][i].posY, true, Vector2Int.left);
	}

	private void FillEdgesWalls()
	{
		int highest = int.MinValue;
		foreach (var row in proceduralTiles)
			foreach (var tile in row)
				highest = Mathf.Max(highest, tile.posY);
		int target = highest + 2;
		int last = currentMapParameters.size - 1;
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(i, 0), target - proceduralTiles[i][0].posY, false, Vector2Int.up, true);
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(last, i), target - proceduralTiles[last][i].posY, false, Vector2Int.left, true);
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(i, last), target - proceduralTiles[i][last].posY, false, Vector2Int.down, true);
		for (int i = 0; i <= last; i++)
			FillEdge(new Vector2Int(0, i), target - proceduralTiles[0][i].posY, false, Vector2Int.right, true);
	}

	private void FillEdgesTrees()
	{
		for (int i = 0; i < currentMapParameters.size; i++)
			FillEdge(new Vector2Int(i, 0), 2, false, Vector2Int.up, true);
		for (int i = 0; i < currentMapParameters.size; i++)
			FillEdge(new Vector2Int(currentMapParameters.size - 1, i), 2, false, Vector2Int.left, true);
		for (int i = 0; i < currentMapParameters.size; i++)
			FillEdge(new Vector2Int(i, currentMapParameters.size - 1), 2, false, Vector2Int.down, true);
		for (int i = 0; i < currentMapParameters.size; i++)
			FillEdge(new Vector2Int(0, i), 2, false, Vector2Int.right, true);
	}

	private void FillEdge(Vector2Int pos1, int desiredHeightOffset, bool isFacingOut, Vector2Int dir, bool useEdgeTextures = false)
	{
		if (pos1.x > currentMapParameters.size - 1 || pos1.y > currentMapParameters.size - 1) return;
		Vector2Int globalDirection = isFacingOut ? dir : -dir;
		ProceduralTile tile = proceduralTiles[pos1.x][pos1.y];
		TileEdge edge = tile.GetEdge(globalDirection);
		int height = tile.posY + edge.offsetHeight;
		if (edge.type == ETileEdgeType.Flat && desiredHeightOffset != 0)
		{
			bool negative = desiredHeightOffset < 0;
			Vector2Int direction = negative ? -globalDirection : globalDirection;
			int baseHeight = height + (negative ? -1 : 0);
			int count = Mathf.Abs(desiredHeightOffset);
			for (int i = 0; i < count; i++)
			{
				EFillType type = i == 0 && count == 1 ? EFillType.Both
					: i == 0 ? (negative ? EFillType.Top : EFillType.Bottom)
					: i == count - 1 ? (negative ? EFillType.Bottom : EFillType.Top)
					: EFillType.Nothing;
				InstantiateFillTile(pos1, baseHeight + i * (negative ? -1 : 1),
					wallFlat, direction, globalDirection, type, false, useEdgeTextures);
			}
		}
		else if (edge.type == ETileEdgeType.SlopeRight || edge.type == ETileEdgeType.SlopeLeft)
		{
			EFillType type = desiredHeightOffset < 0 ? EFillType.Top
				: desiredHeightOffset < 2 ? EFillType.Both : EFillType.Bottom;
			GameObject prefab = desiredHeightOffset <= 0 ? wallLeftDown : wallLeftUp;
			if (desiredHeightOffset != 0)
				FillWallSlopeToFlat(desiredHeightOffset, pos1, height, globalDirection, true, useEdgeTextures);
			InstantiateFillTile(pos1, height, prefab, globalDirection, globalDirection,
				type, edge.type == ETileEdgeType.SlopeRight, useEdgeTextures);
		}
	}

	private Vector3 GetMapToZeroPositionOffset()
	{
		int width = currentMapParameters.scaledTileWidth;
		int offset = width / 2 - (currentMapParameters.size * width) / 2;
		return new Vector3(offset, 0f, offset);
	}

	public Vector3 GetWorldSize()
	{
		int minHeight = int.MaxValue;
		int maxHeight = int.MinValue;
		foreach (GameObject tile in tiles)
		{
			float y = tile.transform.position.y;
			if (minHeight > y) minHeight = (int)y;
			if (maxHeight < y) maxHeight = (int)y;
		}
		int width = currentMapParameters.size * currentMapParameters.scaledTileWidth;
		return new Vector3(width, unchecked(maxHeight - minHeight), width);
	}

	public Vector3 GetWorldCenter()
	{
		int minHeight = int.MaxValue;
		int maxHeight = int.MinValue;
		foreach (GameObject tile in tiles)
		{
			float y = tile.transform.position.y;
			if (minHeight > y) minHeight = (int)y;
			if (maxHeight < y) maxHeight = (int)y;
		}
		return new Vector3(0f, unchecked(maxHeight - minHeight) * 0.5f + minHeight, 0f);
	}

	private void ClearTiles()
	{
		if (tiles != null)
			foreach (GameObject tile in tiles) DestroyImmediate(tile);
	}
}
