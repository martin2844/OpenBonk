using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	public static class Maze
	{
		public static class Directions
		{
			public const int TOP = 1;

			public const int RIGHT = 2;

			public const int BOTTOM = 4;

			public const int LEFT = 8;
		}

		public static readonly Dictionary<int, int> Opposite = new Dictionary<int, int>
		{
			{ Directions.TOP, Directions.BOTTOM }, { Directions.RIGHT, Directions.LEFT },
			{ Directions.BOTTOM, Directions.TOP }, { Directions.LEFT, Directions.RIGHT }
		};

		public static readonly (Vector2Int direction, int dirValue)[] DirectionsWithVectors =
		{
			(new Vector2Int(-1, 0), Directions.TOP), (new Vector2Int(0, 1), Directions.RIGHT),
			(new Vector2Int(1, 0), Directions.BOTTOM), (new Vector2Int(0, -1), Directions.LEFT)
		};

		public static NodeTree Generate(int width, int height, int seed)
		{
			var cells = new byte[width * height];
			var random = new System.Random(seed);
			var positions = new Stack<Vector2Int>();
			var nodes = new Stack<NodeTree>();
			var start = new Vector2Int(random.Next(0, width), random.Next(0, height));
			var root = new NodeTree(start, null);
			positions.Push(start);
			nodes.Push(root);
			cells[ToIndex(start, width)] = 1;
			while (positions.Count > 0)
			{
				Vector2Int position = positions.Peek();
				NodeTree parent = nodes.Peek();
				var neighbors = new List<(Vector2Int direction, int dirValue)>();
				foreach (var direction in DirectionsWithVectors)
				{
					Vector2Int next = position + direction.direction;
					if (IsValidPosition(next, width, height) && cells[ToIndex(next, width)] == 0)
						neighbors.Add((next, direction.dirValue));
				}
				if (neighbors.Count == 0)
				{
					positions.Pop();
					nodes.Pop();
					continue;
				}
				var chosen = Shuffle(neighbors, random)[0];
				cells[ToIndex(position, width)] |= (byte)chosen.dirValue;
				int nextIndex = ToIndex(chosen.direction, width);
				cells[nextIndex] |= (byte)Opposite[chosen.dirValue];
				var child = new NodeTree(chosen.direction, parent);
				parent.children.Add(child);
				positions.Push(chosen.direction);
				nodes.Push(child);
				cells[nextIndex] = 1;
			}
			return root;
		}

		private static int ToIndex(Vector2Int position, int width)
		{
			return position.x * width + position.y;
		}

		private static Vector2Int ToPosition(int index, int width)
		{
			return new Vector2Int(index / width, index % width);
		}

		private static bool IsValidPosition(Vector2Int position, int width, int height)
		{
			return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
		}

		private static List<T> Shuffle<T>(List<T> list, System.Random rand)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = rand.Next(i + 1);
				T value = list[i];
				list[i] = list[j];
				list[j] = value;
			}
			return list;
		}
	}
}
