using UnityEngine;

namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	[CreateAssetMenu(menuName = "Me/Mapping/MapParameters")]
	public class MapParameters : ScriptableObject
	{
		public float volatility;

		public float centerHeightTarget;

		public float slopeStrength;

		public float yOffset;

		public float flatMapBias;

		public int size;

		public int scale;

		public int tileWidth;

		public int tileHeight;

		public EBiasStrategy biasStrategy;

		public EHeightGenerationStrategy heightGenerationStrategy;

		[Header("Calculated Values")]
		public int scaledTileWidth;

		public int scaledTileHeight;

		public StageData testStageData;

		private void OnValidate()
		{
			scaledTileHeight = tileHeight * scale;
			scaledTileWidth = tileWidth * scale;
		}
	}
}
