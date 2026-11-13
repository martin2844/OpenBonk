namespace Assets.Scripts.UI.HUD
{
	public class ScoreContainer
	{
		public string header;

		public string description;

		public bool isPositive;

		public bool useSfx;

		public float sizeMultiplier;

		public ScoreContainer(string header, string description, bool isPositive, bool useSfx, float sizeMultiplier)
		{
			this.header = header;
			this.description = description;
			this.isPositive = isPositive;
			this.useSfx = useSfx;
			this.sizeMultiplier = sizeMultiplier;
		}
	}
}
