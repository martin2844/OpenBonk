using System.Collections.Generic;
using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class QuestsWindowNew : Window
{
	public Button backBtn;

	public GameObject questPrefab;

	public GameObject tabPrefab;

	public ButtonNavigationSelectionOnly tabNavigation;

	public TabVerticalNavigation tabVertNavigation;

	public LocalizedString localizedGeneral;

	public LocalizedString localizedCharacters;

	public LocalizedString localizedWeapons;

	public LocalizedString localizedChallenges;

	public LocalizedString localizedItems;

	public LocalizedString localizedTomes;

	public LocalizedString localizedSkins;

	public LocalizedString localizedHats;

	private List<MyButtonQuest> questButtons;

	private EAchievementType achievementTab;

	public RawImage progressBar;

	public TextMeshProUGUI t_totalProgress;

	public MyButtonToggle toggleHideCompleted;

	private List<EAchievementType> tabsEnums;

	private new void Awake()
	{
		base.Awake();
		CreateTabs();
	}

	private new void Start()
	{
		base.Start();
		if (SaveManager.Instance?.progression?.menuMeta != null)
			SaveManager.Instance.progression.menuMeta.hasVisitedQuests = true;
		achievementTab = EAchievementType.Characters;
		toggleHideCompleted.Set(SaveManager.Instance.config.otherSettings.hideCompletedQuests);
		Refresh();
		tabNavigation.ButtonPressed(0, true);
		tabVertNavigation.Set(tabNavigation.GetSelectedButton());
		MyButtonQuest.A_Hover += OnQuestButtonHover;
		tabNavigation.A_ButtonSelected += OnTabSelected;
		toggleHideCompleted.A_Toggled += OnToggle;
	}

	private new void OnEnable()
	{
		base.OnEnable();
		Refresh();
		int completed = MyAchievements.NumCompletedAchievements();
		int total = MyAchievements.NumTotalAchievements();
		t_totalProgress.text = string.Format("{0:N0}/{1:N0}", completed, total);
		float progress = (float)completed / total;
		progressBar.transform.localScale = new Vector3(progress, 1f, 1f);
		progressBar.color = MyColorUtility.GetRedToGreenGradient(progress);
	}

	private new void OnDestroy()
	{
		base.OnDestroy();
		MyButtonQuest.A_Hover -= OnQuestButtonHover;
		tabNavigation.A_ButtonSelected -= OnTabSelected;
		toggleHideCompleted.A_Toggled -= OnToggle;
	}

	private void OnToggle(bool on)
	{
		Refresh();
		SaveManager.Instance.config.otherSettings.hideCompletedQuests = on;
	}

	private void Refresh()
	{
		var achievements = DataManager.Instance.unsortedAchievements.FindAll(a => a.achievementType == achievementTab && a.isEnabled);
		achievements.Sort((a, b) =>
		{
			bool unclaimedA = a.IsCompleted() && !a.IsClaimed();
			bool unclaimedB = b.IsCompleted() && !b.IsClaimed();
			if (unclaimedA != unclaimedB) return unclaimedA ? -1 : 1;
			if (toggleHideCompleted.IsOn())
			{
				int completion = a.IsClaimed().CompareTo(b.IsClaimed());
				if (completion != 0) return completion;
			}
			return a.CompareTo(b);
		});
		if (questButtons == null) questButtons = new List<MyButtonQuest> { questPrefab.GetComponent<MyButtonQuest>() };
		foreach (MyButtonQuest button in questButtons) button.gameObject.SetActive(false);
		int index = 0;
		foreach (MyAchievement achievement in achievements)
		{
			if (!achievement.IsVisible()) continue;
			if (index >= questButtons.Count)
				questButtons.Add(Instantiate(questPrefab, questPrefab.transform.parent).GetComponent<MyButtonQuest>());
			questButtons[index].gameObject.SetActive(true);
			questButtons[index++].Set(achievement);
		}
	}

	private void CreateTabs()
	{
		tabsEnums = new List<EAchievementType>();
		foreach (EAchievementType type in Enum.GetValues(typeof(EAchievementType)))
			if (DataManager.Instance.unsortedAchievements.Exists(a => a.achievementType == type && a.isEnabled && a.IsVisible())) tabsEnums.Add(type);
		tabsEnums.Sort((a, b) => GetAchievementTypeIndex((int)a).CompareTo(GetAchievementTypeIndex((int)b)));
		for (int i = 0; i < tabsEnums.Count; i++)
		{
			GameObject obj = i == 0 ? tabPrefab : Instantiate(tabPrefab, tabPrefab.transform.parent);
			MyButtonTabsQuest tab = obj.GetComponent<MyButtonTabsQuest>();
			tab.text.GetComponent<LocalizeStringEvent>().StringReference = GetTabLocalizedString(tabsEnums[i]);
			tab.Set(tabsEnums[i]);
		}
		for (int i = 0; i < tabsEnums.Count; i++)
		{
			Button button = tabNavigation.transform.GetChild(i).GetComponent<Button>();
			Navigation navigation = button.navigation;
			if (i > 0) navigation.selectOnLeft = tabNavigation.transform.GetChild(i - 1).GetComponent<Button>();
			navigation.selectOnRight = i + 1 < tabsEnums.Count ? tabNavigation.transform.GetChild(i + 1).GetComponent<Button>() : backBtn;
			button.navigation = navigation;
		}
	}

	private void OnTabSelected(int index)
	{
		achievementTab = tabsEnums[index];
		Refresh();
		tabVertNavigation.Set(tabNavigation.GetSelectedButton());
	}

	private void OnQuestButtonHover(MyButtonQuest btn)
	{
		savedBtn = btn;
	}

	private int GetAchievementTypeIndex(int index)
	{
		switch (index)
		{
			case 0: return 4;
			case 1: return 0;
			case 2: return 1;
			case 3: return 5;
			case 4: return 3;
			case 5: return 2;
			case 6: return 6;
			case 7: return 7;
			default: return -1;
		}
	}

	private LocalizedString GetTabLocalizedString(EAchievementType achType)
	{
		switch (achType)
		{
			case EAchievementType.General: return localizedGeneral;
			case EAchievementType.Characters: return localizedCharacters;
			case EAchievementType.Weapons: return localizedWeapons;
			case EAchievementType.Challenges: return localizedChallenges;
			case EAchievementType.Items: return localizedItems;
			case EAchievementType.Tomes: return localizedTomes;
			case EAchievementType.Skins: return localizedSkins;
			case EAchievementType.Hats: return localizedHats;
			default: return null;
		}
	}
}
