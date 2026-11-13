using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	public class NodeTree
	{
		public int height;

		public int yDir;

		public Vector2Int position { get; private set; }

		public NodeTree parent { get; private set; }

		public List<NodeTree> children { get; private set; }

		public NodeTree(Vector2Int position, NodeTree parent)
		{
			this.position = position;
			this.parent = parent;
			children = new List<NodeTree>();
		}

		public override string ToString()
		{
			return null;
		}
	}
}
