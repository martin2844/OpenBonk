using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using TMPro;
using UnityEngine;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.UI.Mouse;
using Assets.Scripts.Utility;
using UnityEngine.UI;

public class ChallengesUi : Window
{
	public GameObject challengeButtonPrefab;

	public SelectionGroupToggleSingle challengesSelectionGroup;

	private List<SelectionGroupToggleSingleButtonChallenge> challengeButtons;

	public MapSelectionUi mapSelectionUi;

	public MyButton btn_confirm;

	private SelectionGroupToggleSingleButton hoverBtn;

	private Color completedColor = new Color32(0, 196, 255, 255);

	private Color notCompletedColor = new Color32(236, 52, 52, 255);

	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_stats;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_silver;

	public TextMeshProUGUI t_completed;

	public TextMeshProUGUI t_author;

	public TextMeshProUGUI t_header;

	public TextMeshProUGUI t_winCondition;

	public TextMeshProUGUI t_reward;

	public TextMeshProUGUI t_leaderboards;

	private new void Awake()
	{
		base.Awake();
		if (challengesSelectionGroup != null) challengesSelectionGroup.A_ButtonSelected += OnChallengeSelected;
		SelectionGroupToggleSingleButtonChallenge.A_ChallengeHovered += OnChallengeHovered;
	}

	private new void Start()
	{
		base.Start();
	}

	private new void OnDestroy()
	{
		base.OnDestroy();
		if (challengesSelectionGroup != null) challengesSelectionGroup.A_ButtonSelected -= OnChallengeSelected;
		SelectionGroupToggleSingleButtonChallenge.A_ChallengeHovered -= OnChallengeHovered;
	}

	private void OnChallengeHovered(SelectionGroupToggleSingleButtonChallenge btn)
	{
		if (btn == null || !btn.transform.IsChildOf(transform)) return;
		hoverBtn = btn;
		ChallengeData challenge = btn.challengeData;
		if (challenge == null) { SetEmpty(); return; }
		if (!challenge.CanShow()) { SetHidden(challenge); return; }
		if (t_name != null) t_name.text = challenge.GetDisplayName();
		if (t_description != null) t_description.text = challenge.GetUnlockDescription();
		if (t_winCondition != null) { t_winCondition.gameObject.SetActive(true); t_winCondition.text = challenge.winCondition != null ? challenge.winCondition.GetDescription() : string.Empty; }
		if (t_silver != null) t_silver.text = "<sprite name=silver> " + challenge.GetSilverMultiplier() + "x " + LocalizationUtility.GetStatName(EStat.SilverIncreaseMultiplier);
		if (t_author != null)
		{
			bool hasAuthor = !string.IsNullOrEmpty(challenge.suggestionAuthor);
			t_author.gameObject.SetActive(hasAuthor);
			if (hasAuthor) t_author.text = LocalizationUtility.GetLocalizedString("ChallengesUi", "SUGGESTION", new Dictionary<string, string> { ["name"] = challenge.suggestionAuthor });
		}
		if (t_leaderboards != null) { t_leaderboards.gameObject.SetActive(true); t_leaderboards.text = LocalizationUtility.GetLocalizedString("ChallengesUi", "CHALLENGES_LEADERBOARD_NOTE"); }
		bool completed = SaveManager.Instance != null && SaveManager.Instance.progression != null && challenge.IsUnlocked();
		if (t_completed != null) { t_completed.gameObject.SetActive(true); t_completed.text = LocalizationUtility.GetLocalizedString("Other", completed ? "COMPLETED" : "NOT_COMPLETED"); t_completed.color = completed ? completedColor : notCompletedColor; }
		if (t_reward != null) { t_reward.gameObject.SetActive(true); t_reward.text = LocalizationUtility.GetLocalizedString("ChallengesUi", "REWARD", new Dictionary<string, string> { ["reward_icon"] = "<sprite name=silver>", ["value"] = "+1%" }); t_reward.color = completed ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f); }
		UpdateStatsText(challenge);
	}

	private void UpdateStatsText(ChallengeData challengeData)
	{
		if (t_stats == null) return;
		var lines = new List<string>();
		var negativeLines = new List<string>();
		if (challengeData != null && challengeData.challengeModifiers != null)
			foreach (ChallengeModifier modifier in challengeData.challengeModifiers)
				if (modifier != null && modifier.statModifiers != null)
					foreach (StatModifier stat in modifier.statModifiers)
						if (stat != null)
						{
							bool negative = IsNegativeModifier(stat);
							string color = MyColorUtility.GetStatTextColor(!negative);
							string line = StatUtility.EncapsulateNumber(StatUtility.GetModificationString(stat, false), color) + " " + Tooltip.GetTooltipString(stat.stat, color);
							if (negative) negativeLines.Add(line); else lines.Add(line);
						}
		lines.AddRange(negativeLines);
		t_stats.text = string.Join("\n", lines);
	}

	public static bool IsNegativeModifier(StatModifier statModifier)
	{
		if (statModifier == null) return false;
		bool negative = (statModifier.modifyType == EStatModifyType.Addition || statModifier.modifyType == EStatModifyType.Flat)
			? statModifier.modification < 0f
			: statModifier.modifyType == EStatModifyType.Multiplication && statModifier.modification < 1f;
		return IsOppositeStat(statModifier.stat) ? !negative : negative;
	}

	private static bool IsOppositeStat(EStat stat)
	{
		return stat == EStat.ChestPriceMultiplier || (stat >= EStat.EnemyAmountMultiplier && stat <= EStat.EnemyScalingMultiplier);
	}

	private void SetEmpty()
	{
		if (t_name != null) t_name.text = string.Empty;
		if (t_description != null) t_description.text = string.Empty;
		if (t_stats != null) t_stats.text = string.Empty;
		if (t_silver != null) t_silver.text = string.Empty;
		if (t_author != null) t_author.gameObject.SetActive(false);
		if (t_completed != null) t_completed.gameObject.SetActive(false);
		if (t_reward != null) t_reward.gameObject.SetActive(false);
		if (t_leaderboards != null) t_leaderboards.gameObject.SetActive(false);
		if (t_winCondition != null) t_winCondition.gameObject.SetActive(false);
	}

	private void SetHidden(ChallengeData challengeData)
	{
		SetEmpty();
		if (t_name != null) t_name.text = "??";
		if (t_description == null || challengeData == null) return;
		if (challengeData.prerequisiteChallenge != null && (SaveManager.Instance == null || SaveManager.Instance.progression == null || !challengeData.prerequisiteChallenge.IsUnlocked()))
			t_description.text = LocalizationUtility.GetLocalizedString("ChallengesUi", "REQUIREMENT_PRE", new Dictionary<string, string> { ["prerequisite"] = challengeData.prerequisiteChallenge.GetDisplayName() });
		else
		{
			float count = SaveManager.Instance != null && SaveManager.Instance.stats != null ? MyStats.GetStat(EMyStat.challengesCompleted.ToString()) : 0f;
			t_description.text = LocalizationUtility.GetLocalizedString("ChallengesUi", "REQUIREMENT", new Dictionary<string, string> { ["value"] = ((int)count).ToString(), ["target_value"] = challengeData.requiresNumChallengesCompleted.ToString() });
		}
	}

	private new void OnEnable()
	{
		base.OnEnable();
		Refresh();
	}

	private void Refresh()
	{
		if (challengeButtonPrefab == null || mapSelectionUi == null || mapSelectionUi.runConfig == null || mapSelectionUi.runConfig.stageData == null) { SetEmpty(); return; }
		if (challengeButtons == null)
		{
			challengeButtons = new List<SelectionGroupToggleSingleButtonChallenge>();
			var first = challengeButtonPrefab.GetComponent<SelectionGroupToggleSingleButtonChallenge>();
			if (first != null) challengeButtons.Add(first);
		}
		foreach (var button in challengeButtons) if (button != null) button.gameObject.SetActive(false);
		ChallengeData[] source = mapSelectionUi.runConfig.stageData.challenges;
		var challenges = source == null ? new List<ChallengeData>() : source.Where(c => c != null && c.isEnabled).OrderBy(c => c.sortingOrder).ToList();
		for (int i = 0; i < challenges.Count; i++)
		{
			if (i >= challengeButtons.Count)
			{
				var clone = Instantiate(challengeButtonPrefab, challengeButtonPrefab.transform.parent);
				challengeButtons.Add(clone.GetComponent<SelectionGroupToggleSingleButtonChallenge>());
			}
			var button = challengeButtons[i];
			if (button == null) continue;
			button.gameObject.SetActive(true);
			button.Set(challenges[i]);
			Button selectable = button.GetButton();
			if (selectable != null)
			{
				Navigation navigation = selectable.navigation;
				navigation.mode = Navigation.Mode.Explicit;
				navigation.selectOnUp = i > 0 ? challengeButtons[i - 1].GetButton() : null;
				navigation.selectOnDown = i + 1 < challenges.Count && i + 1 < challengeButtons.Count ? challengeButtons[i + 1].GetButton() : null;
				navigation.selectOnRight = btn_confirm != null ? btn_confirm.GetButton() : null;
				selectable.navigation = navigation;
				if (i > 0 && challengeButtons[i - 1].GetButton() != null) { var previous = challengeButtons[i - 1].GetButton(); var n = previous.navigation; n.selectOnDown = selectable; previous.navigation = n; }
			}
		}
		if (challengesSelectionGroup != null) challengesSelectionGroup.FindButtons();
		if (SaveManager.Instance != null && SaveManager.Instance.progression != null && SaveManager.Instance.progression.menuMeta != null && mapSelectionUi.runConfig.mapData != null)
			SaveManager.Instance.progression.menuMeta.GetMapProgress(mapSelectionUi.runConfig.mapData.eMap).tierChallengeNotifications.Remove(mapSelectionUi.runConfig.mapTierIndex);
		if (challenges.Count > 0 && challengeButtons[0] != null) { startBtn = challengeButtons[0]; OnChallengeHovered(challengeButtons[0]); }
		else SetEmpty();
		if (t_header != null) t_header.text = LocalizationUtility.GetLocalizedString("ChallengesUi", "CHALLENGES_HEADER", new Dictionary<string, string> { ["tier"] = (mapSelectionUi.runConfig.mapTierIndex + 1).ToString() });
	}

	public void SetNone()
	{
		if (challengesSelectionGroup != null) challengesSelectionGroup.SetNone();
		if (mapSelectionUi != null) mapSelectionUi.SetChallenge(null);
	}

	private void OnChallengeSelected(SelectionGroupToggleSingleButton btn)
	{
		var challengeButton = btn as SelectionGroupToggleSingleButtonChallenge;
		if (mapSelectionUi != null) mapSelectionUi.SetChallenge(challengeButton != null && challengeButton.isSelected && challengeButton.isShowing ? challengeButton.challengeData : null);
		if (challengeButton != null)
		{
			OnChallengeHovered(challengeButton);
			Button confirm = btn_confirm != null ? btn_confirm.GetButton() : null;
			if (confirm != null)
			{
				Navigation navigation = confirm.navigation;
				navigation.selectOnLeft = challengeButton.GetButton();
				confirm.navigation = navigation;
			}
			startBtn = challengeButton;
		}
	}
}
