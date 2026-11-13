using TMPro;

namespace Assets.Scripts.UI.DynamicWindows
{
	public class DWindow : DWindowBase
	{
		public TextMeshProUGUI t_header;

		public TextMeshProUGUI t_content;

		public TextMeshProUGUI t_button;

		public void Set(string header, string content, string buttonText = "Okey")
		{
			t_header.text = header;
			t_content.text = content;
			t_button.text = buttonText;
			QueueRebuild();
		}

		public void Close()
		{
			UnityEngine.Object.Destroy(gameObject);
		}
	}
}
