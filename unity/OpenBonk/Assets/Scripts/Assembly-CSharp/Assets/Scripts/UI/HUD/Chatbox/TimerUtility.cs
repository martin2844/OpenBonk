namespace Assets.Scripts.UI.HUD.Chatbox
{
	public class TimerUtility
	{
		public static string TimerToString(float time)
		{
			var span = System.TimeSpan.FromSeconds(time);
			return string.Format("{0}:{1:D2}<size=75%>.{2:00}</size>", span.Minutes, span.Seconds, span.Milliseconds / 10);
		}
	}
}
