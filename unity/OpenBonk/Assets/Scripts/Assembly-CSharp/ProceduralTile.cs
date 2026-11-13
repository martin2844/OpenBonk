using System;
using System.Collections.Generic;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEngine;

[Serializable]
public class ProceduralTile : MonoBehaviour
{
	public List<TileEdge> edges;

	public Renderer renderer;

	private Vector2Int[] globalDirections;

	public int posY;

	public bool isFlat;

	public Vector3 dir { get; private set; }

	public Vector3 parentDir { get; private set; }

	public TileEdge GetEdge(Vector2Int globalDirection)
	{
		Vector2Int localDirection = GlobalToLocalDirection(globalDirection);
		foreach (TileEdge edge in edges)
			if (edge.direction == localDirection) return edge;
		return null;
	}

	public void SetGlobalRotation(Vector2Int direction)
	{
		transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
	}

	public Vector2Int GlobalToLocalDirection(Vector2Int dir)
	{
		Vector3 local = transform.InverseTransformDirection(new Vector3(dir.x, 0f, dir.y));
		return new Vector2Int((int)local.x, (int)local.z);
	}

	public void SetPosY(int y, StageData stageData, bool isFlat, Vector3 dir, Vector3 parentDir)
	{
		posY = y;
		this.isFlat = isFlat;
		this.dir = dir;
		this.parentDir = parentDir;
		renderer.sharedMaterial = stageData.GetTopMaterial();
	}

	public int GetY()
	{
		return posY;
	}
}
