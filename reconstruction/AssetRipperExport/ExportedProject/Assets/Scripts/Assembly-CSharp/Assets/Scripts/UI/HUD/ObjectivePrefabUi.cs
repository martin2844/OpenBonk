using Assets.Scripts.Inventory__Items__Pickups.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

namespace Assets.Scripts.UI.HUD
{
	public class ObjectivePrefabUi : MonoBehaviour
	{
		public GameObject checkBox;

		public GameObject checkMark;

		public TextMeshProUGUI t_objective;

		public LayoutGroup content;

		public TextSizer textSizer;

		private EObjective eObjective;

		private LocalizedString localizedObjective;

		public RawImage overlay;

		private float padding = 20f;

		private int paddingWidth;

		private float slideTime = 1f;

		private float timer;

		private bool completed;

		private Color completedColor = new Color(0f, 0.2f, 0f, 1f);

		private void Awake()
		{
			ItemInventory.A_ItemAdded += OnItemAdded;
		}

		private void OnDestroy()
		{
			ItemInventory.A_ItemAdded -= OnItemAdded;
		}

		private void OnItemAdded(EItem eItem)
		{
			if (eItem == EItem.CryptKey && eObjective == EObjective.CryptKeys) RefreshText();
		}

		private void RefreshText()
		{
			if (eObjective == EObjective.Generic) t_objective.text = localizedObjective.GetLocalizedString();
			else if (eObjective == EObjective.CryptKeys)
			{
				var values = new Dictionary<string, string>
				{
					["current"] = MyPlayer.Instance.inventory.itemInventory.GetAmount(EItem.CryptKey).ToString(),
					["target"] = "4"
				};
				t_objective.text = localizedObjective.GetLocalizedString(values);
			}
			Invoke(nameof(PadAndRebuild), 0.5f);
		}

		public void Set(LocalizedString localizedObjective, bool showCheckmark, EObjective eObjective = EObjective.Generic)
		{
			this.eObjective = eObjective;
			gameObject.SetActive(true);
			checkBox.SetActive(showCheckmark);
			this.localizedObjective = localizedObjective;
			RefreshText();
		}

		private void Update()
		{
			if (timer >= 1f)
			{
				if (content.padding.left != 0) content.padding.left = 0;
				return;
			}
			if (!completed)
			{
				timer = Mathf.Clamp01(timer + MyTime.deltaTime / slideTime);
				content.padding.left = (int)Mathf.Lerp(paddingWidth, 0f, UnityEngine.UIElements.Experimental.Easing.OutCubic(timer));
				float flash = 0.95f - Mathf.Abs(Mathf.Repeat(timer * 4f, 1.9f) - 0.95f);
				overlay.color = new Color(flash, flash, flash, 1f);
				content.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, UnityEngine.UIElements.Experimental.Easing.OutCirc(timer));
				content.CalculateLayoutInputHorizontal(); content.CalculateLayoutInputVertical();
				if (timer >= 1f) { overlay.color = Color.black; content.transform.localScale = Vector3.one; content.padding.left = 0; }
			}
			else
			{
				timer = Mathf.Clamp01(timer + MyTime.deltaTime * 2f);
				overlay.color = Color.Lerp(Color.black, completedColor, UnityEngine.UIElements.Experimental.Easing.InQuad(timer));
				content.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, UnityEngine.UIElements.Experimental.Easing.OutCirc(timer));
			}
		}

		private void PadAndRebuild()
		{
			Rebuild();
			Canvas.ForceUpdateCanvases();
			paddingWidth = (int)-(content.GetComponent<RectTransform>().rect.width + padding * 2f);
			content.padding.left = paddingWidth;
		}

		private void Rebuild()
		{
			textSizer.Refresh();
			textSizer.Recalculate();
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)textSizer.transform);
			LayoutGroup[] groups = GetComponentsInChildren<LayoutGroup>();
			for (int i = groups.Length - 1; i >= 0; i--)
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)groups[i].transform);
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
		}

		public void Complete()
		{
			checkMark.SetActive(true);
			overlay.color = Color.black;
			timer = 0f;
			content.padding.left = 0;
			completed = true;
			LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
		}
	}
}
