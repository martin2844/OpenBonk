namespace Assets.Scripts.Saves___Serialization.Progression
{
	public class CharacterProgression
	{
		public int xp;

		public int numRuns;

		private float xpModifier = 0.1f;

		public void OnRunFinished(int xp)
		{
			this.xp += UnityEngine.Mathf.RoundToInt(xp * xpModifier);
			numRuns++;
		}

		public int GetRank()
		{
			return global::Inventory__Items__Pickups.XpUtility.XpToLevel(xp) + 1;
		}

		public bool HasStar()
		{
			return GetRank() > 99;
		}
	}
}
