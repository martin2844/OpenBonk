using System;
using UnityEngine;

namespace Assets.Scripts.Game.MapGeneration
{
	[Serializable]
	public class StageTilePrefabs
	{
		public GameObject[] flatTilePrefabs;

		public float populateTilesRatio;

		public GameObject[] mapSpecificTilesPrefabs;

		public int numSpecificTiles;
	}
}
