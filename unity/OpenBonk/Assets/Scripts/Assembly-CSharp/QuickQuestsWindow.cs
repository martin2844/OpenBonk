using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.UI;
using UnityEngine;

public class QuickQuestsWindow : MonoBehaviour
{
	public GameObject prefab;

	public GameObject allQuestsCompletedText;

	private int numMaxQuests = 4;

	private List<QuickQuestContainer> containers;

	private List<MyAchievement> quests;

	private Dictionary<MyAchievement, int> randomTieBreaker = new Dictionary<MyAchievement, int>();

	private void Start()
	{
		Refresh();
		SaveManager.A_SavesLoaded += Refresh;
		Invoke(nameof(Refresh), 2f);
	}

	private void OnDestroy()
	{
		SaveManager.A_SavesLoaded -= Refresh;
	}

	private void OnEnable()
	{
		Refresh();
	}

	private void Refresh()
	{
		if (DataManager.Instance == null || SaveManager.Instance == null || SaveManager.Instance.progression == null || SaveManager.Instance.stats == null) return;
		TryInit();
		quests.RemoveAll(a => a == null || a.IsCompleted());
		if (quests.Count < numMaxQuests)
		{
			var available = GetAllAchievements();
			int count = Mathf.Min(numMaxQuests - quests.Count, available.Count);
			for (int i = 0; i < count; i++) quests.Add(available[i]);
		}
		foreach (var container in containers) container.gameObject.SetActive(false);
		for (int i = 0; i < quests.Count && i < containers.Count; i++)
		{
			containers[i].gameObject.SetActive(true);
			containers[i].SetQuest(quests[i]);
		}
		if (allQuestsCompletedText != null) allQuestsCompletedText.SetActive(quests.Count == 0 && MyAchievements.AreAllQuestsCompleted());
		UiUtility.RebuildUi(transform);
	}

	private int GetTie(MyAchievement a)
	{
		if (!randomTieBreaker.TryGetValue(a, out int tie)) randomTieBreaker[a] = tie = Random.Range(int.MinValue, int.MaxValue);
		return tie;
	}

	private List<MyAchievement> GetAllAchievements()
	{
		var available = new List<MyAchievement>();
		foreach (var a in DataManager.Instance.unsortedAchievements)
			if (a != null && a.IsVisible() && !a.IsCompleted() && !quests.Contains(a)) available.Add(a);
		available.Sort((a, b) => {
			int order = b.GetProgress().CompareTo(a.GetProgress());
			if (order == 0) order = a.difficulty.CompareTo(b.difficulty);
			return order != 0 ? order : GetTie(a).CompareTo(GetTie(b));
		});
		return available;
	}

	private void TryInit()
	{
		if (containers == null)
		{
			containers = new List<QuickQuestContainer>();
			containers.Add(prefab.GetComponent<QuickQuestContainer>());
			for (int i = 1; i < numMaxQuests; i++) containers.Add(Instantiate(prefab, prefab.transform.parent).GetComponent<QuickQuestContainer>());
		}
		if (quests == null)
		{
			quests = new List<MyAchievement>();
			var available = GetAllAchievements();
			for (int i = 0; i < Mathf.Min(numMaxQuests, available.Count); i++) quests.Add(available[i]);
		}
	}
}
