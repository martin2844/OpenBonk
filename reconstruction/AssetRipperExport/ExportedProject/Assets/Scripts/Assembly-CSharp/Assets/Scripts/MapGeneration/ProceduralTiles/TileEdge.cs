using System;
using UnityEngine;

namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	[Serializable]
	public class TileEdge
	{
		public Vector2Int direction;

		public int offsetHeight;

		public ETileEdgeType type;
	}
}
