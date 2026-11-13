using System.Collections.Generic;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Game.Other
{
	public class RunConfig
	{
		public MapData mapData;

		public StageData stageData;

		public int mapTierIndex;

		public ChallengeData challenge;

		public int musicTrackIndex = -1;

		private Dictionary<int, float> tierSilverMultipliers = new Dictionary<int, float> { { 0, 1f }, { 1, 1.1f }, { 2, 1.2f } };

		public float GetEnemyHp(float hp)
		{
			if (mapData.eMap == EMap.Graveyard || mapTierIndex == 2) return hp * 1.05f;
			if (mapTierIndex == 0) return hp * 0.8f;
			if (mapTierIndex == 1) return hp * 0.95f;
			return hp;
		}

		public float GetEnemySpeed(float speed)
		{
			if (mapData.eMap == EMap.Graveyard || mapTierIndex == 2) return speed * 1.065f;
			if (mapTierIndex == 0) return speed * 0.85f;
			if (mapTierIndex == 1) return speed * 0.98f;
			return speed;
		}

		public float GetEnemyDamage(float value)
		{
			if (mapData.eMap == EMap.Graveyard || mapTierIndex == 2) return value * 1.1f;
			if (mapTierIndex == 0) return value * 0.75f;
			if (mapTierIndex == 1) return value * 0.95f;
			return value;
		}

		private int GetIndexToMultiplier()
		{
			return mapData.eMap == EMap.Graveyard ? 2 : mapTierIndex;
		}

		public float GetSilverMultiplier()
		{
			float multiplier = tierSilverMultipliers[mapTierIndex];
			if (challenge != null) multiplier += challenge.silverMultiplier - 1f;
			return multiplier;
		}

		public string GetFormattedSilverMultiplier()
		{
			return MyStringUtil.ShowOnlyDecimals(GetSilverMultiplier());
		}
	}
}
