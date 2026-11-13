using TMPro;
using UnityEngine;

namespace Assets.Scripts.Settings___Saves
{
	public class AboutText : MonoBehaviour
	{
		public TextMeshProUGUI text;

		public void SetText(string header, string body)
		{
			text.text = string.Concat(header, "<size=75%>\n\n", body);
		}
	}
}
