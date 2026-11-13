using System;
using TMPro;

namespace Assets.Scripts.UI.DynamicWindows
{
	public class DWindowPrompt : DWindowBase
	{
		public TextMeshProUGUI t_header;

		public TextMeshProUGUI t_content;

		private Action A_Accept;

		public void Set(string header, string content, Action A_Accept)
		{
			t_header.text = header;
			t_content.text = content;
			this.A_Accept = A_Accept;
			QueueRebuild();
		}

		public void Accept()
		{
			A_Accept?.Invoke();
			UnityEngine.Object.Destroy(gameObject);
		}

		public void Close()
		{
			UnityEngine.Object.Destroy(gameObject);
		}
	}
}
