using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;
using System.Linq;

public class TargetOfInterestUi : MonoBehaviour
{
	public List<TargetOfInterestPrefab> prefabs;

	private HashSet<Enemy> activeTargets = new HashSet<Enemy>();

	private List<Enemy> queuedTargets = new List<Enemy>();

	private Queue<Enemy> addTargetQueue = new Queue<Enemy>();

	private RectTransform rowContainer;

	private float defaultWidth;

	private void Awake()
	{
		rowContainer = transform as RectTransform;
		if (rowContainer != null) defaultWidth = rowContainer.sizeDelta.x;
		Enemy.A_TargetOfInterestSpawn += OnTargetOfInterestSpawned;
		Enemy.A_EnemyReleasedFromPool += OnEnemyReleasedFromPool;
	}

	private void OnDestroy()
	{
		Enemy.A_TargetOfInterestSpawn -= OnTargetOfInterestSpawned;
		Enemy.A_EnemyReleasedFromPool -= OnEnemyReleasedFromPool;
	}

	private void OnTargetOfInterestSpawned(Enemy enemy)
	{
		if (enemy != null) addTargetQueue.Enqueue(enemy);
	}

	private void Update()
	{
		if (UiManager.Instance != null && UiManager.Instance.cinematicBars != null && UiManager.Instance.cinematicBars.IsShowing()) return;
		while (addTargetQueue.Count > 0) DequeueEnemies(addTargetQueue.Dequeue());
	}

	private void DequeueEnemies(Enemy enemy)
	{
		if (enemy == null || prefabs == null || activeTargets.Contains(enemy) || queuedTargets.Contains(enemy)) return;
		if (activeTargets.Count >= prefabs.Count) AddRows();
		if (activeTargets.Count < prefabs.Count) { activeTargets.Add(enemy); RefreshPrefabs(); }
		else queuedTargets.Add(enemy);
	}

	private void AddRows()
	{
		if (prefabs.Count == 0) return;
		var template = prefabs[prefabs.Count - 1];
		if (template == null) return;
		var group = template.transform.parent;
		// The gameplay scenes use a vertical list of horizontal groups of three.
		// Duplicate the last group so extra bosses retain the same layout and styling.
		if (group != null && group.parent == transform && group.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null)
		{
			var copy = Instantiate(group.gameObject, transform, false);
			copy.name = group.name + " (extra)";
			foreach (var row in copy.GetComponentsInChildren<TargetOfInterestPrefab>(true))
			{
				row.gameObject.SetActive(false);
				prefabs.Add(row);
			}
		}
		else
		{
			var copy = Instantiate(template, template.transform.parent, false);
			copy.gameObject.SetActive(false);
			prefabs.Add(copy);
		}
	}

	private void OnEnemyReleasedFromPool(Enemy enemy)
	{
		int pending = addTargetQueue.Count;
		for (int i = 0; i < pending; i++)
		{
			Enemy queued = addTargetQueue.Dequeue();
			if (queued != enemy) addTargetQueue.Enqueue(queued);
		}
		bool removedQueued = queuedTargets.Remove(enemy);
		if (activeTargets.Remove(enemy) || removedQueued) RefreshPrefabs();
	}

	private void RefreshPrefabs()
	{
		if (prefabs == null) return;
		while (activeTargets.Count < prefabs.Count && queuedTargets.Count > 0) { activeTargets.Add(queuedTargets[0]); queuedTargets.RemoveAt(0); }
		// The objective panel occupies the left edge of the second and later
		// rows on the Retroid HUD. Leave space for it when multiple rows are used.
		if (rowContainer != null && defaultWidth > 0f)
		{
			Vector2 size = rowContainer.sizeDelta;
			size.x = activeTargets.Count > 3 ? defaultWidth * .78f : defaultWidth;
			rowContainer.sizeDelta = size;
		}
		for (int i = 0; i < prefabs.Count; i++) if (prefabs[i] != null) prefabs[i].SetEnemy(i < activeTargets.Count ? activeTargets.ElementAt(i) : null);
	}
}
