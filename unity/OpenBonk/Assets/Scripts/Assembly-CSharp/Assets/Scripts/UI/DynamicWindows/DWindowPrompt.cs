using System;
using TMPro;
using UnityEngine.Localization.Components;

namespace Assets.Scripts.UI.DynamicWindows
{
	public class DWindowPrompt : DWindowBase
	{
		public TextMeshProUGUI t_header;

		public TextMeshProUGUI t_content;

		private Action A_Accept;

		private Action A_Decline;

		public void Set(string header, string content, Action A_Accept)
		{
			t_header.text = header;
			t_content.text = content;
			this.A_Accept = A_Accept;
			QueueRebuild();
		}

		public bool SetChoices(string acceptText, string declineText, Action decline)
		{
			// These controls belong to the shared DynamicWindowPrompt prefab.
			var buttons = transform.Find("Shadow/Border/Background/WindowLayers/Content/Buttons");
			var accept = buttons?.Find("B_Yes");
			var declineButton = buttons?.Find("B_No");
			var acceptLabel = accept?.GetComponentInChildren<TextMeshProUGUI>();
			var declineLabel = declineButton?.GetComponentInChildren<TextMeshProUGUI>();
			var acceptFocus = accept?.GetComponent<MyButton>();
			if (acceptLabel == null || declineLabel == null || acceptFocus == null) return false;

			SetChoiceLabel(acceptLabel, acceptText);
			SetChoiceLabel(declineLabel, declineText);
			GetComponent<Window>().startBtn = acceptFocus;
			var back = GetComponentInChildren<BackEscape>();
			if (back != null) back.gameObject.SetActive(false);
			A_Decline = decline;
			return true;
		}

		private static void SetChoiceLabel(TextMeshProUGUI label, string text)
		{
			var localization = label.GetComponent<LocalizeStringEvent>();
			if (localization != null) localization.enabled = false;
			label.text = text;
		}

		public void Accept()
		{
			Finish(A_Accept);
		}

		public void Close()
		{
			Finish(A_Decline);
		}

		private void Finish(Action choice)
		{
			if (!gameObject.activeSelf) return;
			gameObject.SetActive(false);
			UnityEngine.Object.Destroy(gameObject);
			choice?.Invoke();
		}
	}
}
