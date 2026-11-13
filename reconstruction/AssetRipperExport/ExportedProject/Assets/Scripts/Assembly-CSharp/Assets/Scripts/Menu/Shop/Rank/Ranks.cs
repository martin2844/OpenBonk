using UnityEngine;

namespace Assets.Scripts.Menu.Shop.Rank
{
	public static class Ranks
	{
		public const int maxRank = 240;

		public const int numRankTiers = 6;

		public const int numRanksPerTier = 40;

		public static void GetRankTextures(int rank, out Texture frame, out Texture rankIcon, out Color rankColor, out Color frameColor)
		{
			int clamped = Mathf.Clamp(rank, 0, maxRank);
			var icons = Assets.Scripts.Managers.IconManager.Instance;
			frame = icons != null ? icons.rankFrameIcon : null;
			rankIcon = icons != null && icons.rankIcons != null && icons.rankIcons.Length > 0 ? icons.rankIcons[clamped % icons.rankIcons.Length] : null;
			frameColor = Assets.Scripts.Utility.MyColorUtility.GetRankColor(clamped);
			rankColor = Color.Lerp(frameColor, Color.white, 0.6f);
		}
	}
}
