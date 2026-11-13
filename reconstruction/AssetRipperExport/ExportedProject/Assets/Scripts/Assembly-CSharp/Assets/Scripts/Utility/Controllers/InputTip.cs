namespace Assets.Scripts.Utility.Controllers
{
	public class InputTip
	{
		public string tip;

		public string action;

		public float extraDelay;

		public InputTip(string tip, string action, float extraDelay)
		{
			this.tip = tip;
			this.action = action;
			this.extraDelay = extraDelay;
		}
	}
}
