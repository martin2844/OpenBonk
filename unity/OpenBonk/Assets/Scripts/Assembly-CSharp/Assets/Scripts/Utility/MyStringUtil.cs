using TMPro;
using UnityEngine;

namespace Assets.Scripts.Utility
{
	public class MyStringUtil
	{
		public static string ShowOnlyDecimals(float number)
		{
			number = Mathf.Round(number * 100f) / 100f;
			float whole = Mathf.Floor(number);
			if (Mathf.Approximately(number, whole)) return whole.ToString();
			float oneDecimal = Mathf.Round(number * 10f) / 10f;
			return Mathf.Approximately(number, oneDecimal) ? oneDecimal.ToString("0.0") : number.ToString("0.00");
		}

		public static void ApplyEllipsisIfTruncated(TextMeshProUGUI tmp)
		{
		}
	}
}
