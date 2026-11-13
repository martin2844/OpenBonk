using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	public class MazeHeightGenerator
	{
		public static void GenerateHeight(ProceduralTileGeneration tileGeneration, NodeTree startNode, int seed, MapParameters mapParameters)
		{
			switch (mapParameters.heightGenerationStrategy)
			{
				case EHeightGenerationStrategy.Hein:
					GenerateHeightHein(tileGeneration, startNode, seed, mapParameters);
					break;
				case EHeightGenerationStrategy.Me:
					GenerateHeightMe(tileGeneration, startNode, seed, mapParameters);
					break;
			}
		}

		public static void GenerateHeightHein(ProceduralTileGeneration tileGeneration, NodeTree startNode, int seed, MapParameters mapParameters)
		{
		}

		public static void GenerateHeightMe(ProceduralTileGeneration proceduralTileGeneration, NodeTree startNode, int seed, MapParameters mapParameters)
		{
			var random = new System.Random(seed);
			var pending = new List<NodeTree>();
			NodeTree current = startNode;
			int height = 0;
			while (current != null)
			{
				NodeTree next = null;
				bool resumedBranch = false;
				for (int i = 0; i < current.children.Count; i++)
				{
					if (i == 0) next = current.children[i];
					else pending.Add(current.children[i]);
				}
				if (next == null && pending.Count > 0)
				{
					next = pending[0];
					pending.RemoveAt(0);
					resumedBranch = true;
				}

				Vector2Int direction = Vector2Int.zero;
				int slope = 0;
				int tileHeight = height;
				bool straight = current.children.Count == 1 && current.children.Contains(next) && current.parent != null;
				if (straight)
				{
					direction = next.position - current.position;
					straight = direction == current.position - current.parent.position;
				}
				if (straight && random.NextDouble() < mapParameters.volatility)
				{
					if (current.yDir > 0) slope = 1;
					else if (current.yDir < 0) slope = -1;
					else if (random.NextDouble() < Mathf.Exp((height < 0 ? height : unchecked(-height)) * mapParameters.flatMapBias))
						slope = height < 0 ? 1 : -1;
					else slope = random.NextDouble() < 0.5 ? 1 : -1;
					height += slope;
					if (slope < 0) tileHeight = height;
				}
				current.height = height;
				foreach (NodeTree child in current.children) child.yDir = slope;
				Vector2Int tileDirection = slope < 0 ? -direction : direction;
				Vector2Int parentDirection = current.parent != null
					? current.position - current.parent.position : tileDirection;
				proceduralTileGeneration.InstantiateTile(current.position, tileHeight,
					slope == 0 ? proceduralTileGeneration.flatTile : proceduralTileGeneration.slopeTile,
					new Vector3(tileDirection.x, 0f, tileDirection.y),
					new Vector3(parentDirection.x, 0f, parentDirection.y));
				current = next;
				if (resumedBranch) height = current.parent.height;
			}
		}
	}
}
