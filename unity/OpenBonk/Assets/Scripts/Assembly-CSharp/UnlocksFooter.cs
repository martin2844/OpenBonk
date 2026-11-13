using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using TMPro;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.Localization;
using System.Collections.Generic;

public class UnlocksFooter : MonoBehaviour
{
	public TextMeshProUGUI t_unlockName;

	public TextMeshProUGUI t_unlockDescription;

	public UnlockContainer unlockContainer;

	public MyButton buyButton;

	public RequirementPrefab[] reqContainers;

	public RequirementsContainer requirementsContainer;

	public GameObject buyContainer;

	public TextMeshProUGUI t_buyPrice;

	public TextMeshProUGUI t_suggestedBy;

	public UnlocksExtraInformation extraInformation;

	public ButtonNavigationSelectionOnly tabNavigation;

	public static Action<UnlockableBase> A_Purchased;

	private UnlockContainer lastSelected;

	private Vector2 reqContainerPosDefault = new Vector2(662f, -100f);

	private Vector2 reqContainerPosSmall = new Vector2(563f, -100f);

	private Vector2 reqContainerScaleDefault = new Vector2(1000f, 77f);

	private Vector2 reqContainerScaleSmall = new Vector2(800f, 77f);

	public Window parentWindow;

	private void Awake()
	{
		SetEmpty();
		UnlockContainer.A_Selected += OnUnlockSelected;
		MyButtonUnlock.A_Clicked += OnUnlockClicked;
		if (tabNavigation != null) tabNavigation.A_ButtonSelected += OnTabChanged;
	}

	private void OnDestroy()
	{
		UnlockContainer.A_Selected -= OnUnlockSelected;
		MyButtonUnlock.A_Clicked -= OnUnlockClicked;
		if (tabNavigation != null) tabNavigation.A_ButtonSelected -= OnTabChanged;
	}

	private void OnTabChanged(int index)
	{
		SetEmpty();
	}

	private void SetEmpty()
	{
		lastSelected = null;
		if (t_unlockName != null) t_unlockName.text = string.Empty;
		if (t_unlockDescription != null)
		{
			t_unlockDescription.text = string.Empty;
			t_unlockDescription.gameObject.SetActive(false);
		}
		if (unlockContainer != null) unlockContainer.SetEmpty();
		if (buyContainer != null) buyContainer.SetActive(false);
		if (requirementsContainer != null) requirementsContainer.gameObject.SetActive(false);
		if (extraInformation != null) extraInformation.gameObject.SetActive(false);
		if (t_suggestedBy != null) t_suggestedBy.gameObject.SetActive(false);
	}

	private void OnUnlockSelected(UnlockContainer container)
	{
		if (container != null) Refresh(container);
	}

	private void Refresh(UnlockContainer container)
	{
		if (container == null) return;
		lastSelected = container;
		var item = container.unlockable;
		if (unlockContainer != null) unlockContainer.Set(item);
		bool available = item != null && !container.fullGameOnly;
		if (t_unlockName != null) t_unlockName.text = available ? item.GetName() : "???";
		bool purchased = available && container.isUnlocked && container.isPurchased;
		if (t_unlockDescription != null)
		{
			t_unlockDescription.gameObject.SetActive(purchased);
			t_unlockDescription.text = purchased ? item.GetDescription() : string.Empty;
		}
		if (buyContainer != null) buyContainer.SetActive(available && container.isUnlocked && !container.isPurchased);
		if (t_buyPrice != null) t_buyPrice.text = available ? $"<sprite name=silver> {item.GetPrice()}" : string.Empty;
		if (requirementsContainer != null)
		{
			bool showRequirements = available && !purchased;
			requirementsContainer.gameObject.SetActive(showRequirements);
			if (showRequirements)
			{
				requirementsContainer.Set(item);
				if (container.isUnlocked) requirementsContainer.HideBar();
				var rect = requirementsContainer.GetComponent<RectTransform>();
				if (rect != null)
				{
					rect.anchoredPosition = container.isUnlocked ? reqContainerPosSmall : reqContainerPosDefault;
					rect.sizeDelta = container.isUnlocked ? reqContainerScaleSmall : reqContainerScaleDefault;
				}
			}
		}
		if (extraInformation != null)
		{
			extraInformation.gameObject.SetActive(purchased);
			if (purchased) extraInformation.TrySetInfo(item);
		}
		if (t_suggestedBy != null)
		{
			bool showAuthor = purchased && !string.IsNullOrEmpty(item.author);
			t_suggestedBy.gameObject.SetActive(showAuthor);
			t_suggestedBy.text = showAuthor
				? LocalizationUtility.GetLocalizedString("Unlockables", "SUGGESTED_BY", new Dictionary<string, string> { ["author"] = item.author })
				: string.Empty;
		}
	}

	private void OnUnlockClicked(UnlockContainer container)
	{
		if (container == null) return;
		if (buyContainer != null && buyContainer.activeInHierarchy) ButtonManager.ForceHoverButton(buyButton);
	}

	public void TryBuyUnlockable()
	{
		var item = lastSelected != null ? lastSelected.unlockable : null;
		if (item == null || !lastSelected.isUnlocked || !item.CanBuy() || SaveManager.Instance?.progression == null) return;
		if (!SaveManager.Instance.progression.PurchaseUnlockable(item)) return;
		if (parentWindow != null) parentWindow.FindAllButtonsInWindow();
		A_Purchased?.Invoke(item);
		if (lastSelected != null)
		{
			lastSelected.Set(item);
			ButtonManager.ForceHoverButton(lastSelected.GetComponent<MyButton>());
			Refresh(lastSelected);
		}
	}
}
