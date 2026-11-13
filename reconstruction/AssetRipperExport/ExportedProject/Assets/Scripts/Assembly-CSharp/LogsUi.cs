using System.Collections.Generic;
using System;
using Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Menu.Windows;
using TMPro;
using UnityEngine;

public class LogsUi : MonoBehaviour
{
	public RectTransform content;

	public RectTransform entryPrefab;

	private List<MyButtonLog> logEntries = new List<MyButtonLog>();

	private int page = -1;

	private int maxPages = 99;

	private int entriesPerPage;

	private List<EEnemy> enemies;

	public TextMeshProUGUI t_pages;

	public TextMeshProUGUI t_title;

	public TabsExplicitNavigation entryNavigation;

	private bool hasRefreshed;

	public LogsDisplayEnemy logDisplay;

	private void LateUpdate()
	{
		if (hasRefreshed) return;
		hasRefreshed = true;
		Refresh();
	}

	private void OnDisable()
	{
		hasRefreshed = false;
	}

	private void TryInit()
	{
		UiUtility.RebuildUi(transform);
		if (content == null || entryPrefab == null || entryPrefab.rect.height <= 0f) return;
		if (logEntries.Count == 0)
		{
			var first = entryPrefab.GetComponent<MyButtonLog>();
			if (first == null) return;
			logEntries.Add(first);
			first.StopHover();
		}
		entriesPerPage = Mathf.Max(1, Mathf.FloorToInt(content.rect.height / entryPrefab.rect.height));
		while (logEntries.Count < entriesPerPage)
		{
			var entry = Instantiate(entryPrefab, content).GetComponent<MyButtonLog>();
			if (entry == null) break;
			logEntries.Add(entry);
		}
		entriesPerPage = Mathf.Min(entriesPerPage, logEntries.Count);
		enemies = new List<EEnemy>((EEnemy[])Enum.GetValues(typeof(EEnemy)));
		maxPages = Mathf.CeilToInt((float)enemies.Count / entriesPerPage);
		if (t_title != null) t_title.text = $"Monster log [{LogUtility.GetNumUnlockedEntries()}/{LogUtility.GetNumMaxEntries()}]";
	}

	private void Refresh()
	{
		TryInit();
		if (enemies == null || logEntries.Count == 0) return;
		OpenPage(0, true);
		ButtonManager.SetFirstButton(logEntries[0]);
		if (logDisplay != null) logDisplay.SetEnemy(logEntries[0].eEnemy);
	}

	private void OpenPage(int page, bool force = false)
	{
		if ((!force && this.page == page) || enemies == null || maxPages <= 0) return;
		if (page < 0) page = maxPages - 1;
		if (page >= maxPages) page = 0;
		this.page = page;
		for (int i = 0; i < entriesPerPage; i++)
		{
			int index = page * entriesPerPage + i;
			var entry = logEntries[i];
			entry.gameObject.SetActive(index < enemies.Count);
			if (index < enemies.Count) entry.SetEnemy(enemies[index], index);
		}
		UpdatePageText();
		if (entryNavigation != null) entryNavigation.Refresh();
	}

	public void FlipPage(int direction)
	{
		OpenPage(page + direction);
	}

	private void UpdatePageText()
	{
		if (t_pages != null) t_pages.text = $"{page + 1}/{maxPages}";
	}
}
