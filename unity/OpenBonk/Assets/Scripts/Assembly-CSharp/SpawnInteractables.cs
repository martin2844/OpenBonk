using UnityEngine;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class SpawnInteractables : MonoBehaviour
{
	public GameObject chest;

	public GameObject chestFree;

	private float chestDensity;

	private float shrineDensity;

	private float chanceForFreeChest = 0.05f;

	public const int numChestsPerStage = 46;

	public const int numShrines = 14;

	private int numRails;

	public GameObject[] rails;

	private Vector3 worldArea;

	private Vector3 worldCenter;

	private float areaMagnitude;

	public void SetArea(Vector3 worldArea, Vector3 worldCenter, float mag)
	{
		this.worldArea = worldArea;
		this.worldCenter = worldCenter;
		areaMagnitude = mag;
	}

	public void SpawnShit()
	{
		SpawnRails();
		SpawnChests();
		SpawnShrines();
		SpawnOther();
	}

	public void SpawnChests()
	{
		if (ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_NO_ITEMS)) return;
		float count = Mathf.RoundToInt(numChestsPerStage * PlayerStats.GetStat(EStat.ChestIncreaseMultiplier));
		if (ChallengesTracker.HasChallengeModifier("turbo")) count = 92f;
		int total = (int)(count * MapController.currentMap.numChestsMultiplier);
		for (int i = 0; i < total; i++)
		{
			Vector3 position = SpawnPositions.GetObjectSpawnPosition(worldCenter, worldArea, 0.5f,
				GameManager.Instance.whatIsGroundAndObjects, out Vector3 normal, 50, true, false, false, 44f);
			if ((position - SpawnPositions.INVALID_POS).sqrMagnitude < 1e-10f) continue;
			GameObject prefab = global::Utility.MyRandom.random.NextDouble() < chanceForFreeChest ? chestFree : chest;
			GameObject placed = Instantiate(prefab, position, chest.transform.rotation);
			placed.transform.rotation = Quaternion.LookRotation(normal);
		}
	}

	public void SpawnShrines()
	{
		var available = new List<GameObject>();
		foreach (GameObject shrine in MapController.currentMap.shrines)
		{
			if (MyStats.GetStat(EMyStat.runs.ToString()) <= 2f && shrine.GetComponentInChildren<InteractableShrineCursed>() != null)
				continue;
			available.Add(shrine);
		}
		if (available.Count == 0) return;
		int total = (int)(numShrines * MapController.currentMap.numShrinesMultiplier);
		int spawned = 0;
		for (int i = 0; i < total; i++)
		{
			Vector3 position = SpawnPositions.GetObjectSpawnPosition(worldCenter, worldArea, 0.5f,
				GameManager.Instance.whatIsGroundAndObjects, out Vector3 normal, 50, true, true, false, 44f);
			if ((position - SpawnPositions.INVALID_POS).sqrMagnitude < 1e-10f) continue;
			GameObject prefab = available[global::Utility.MyRandom.random.Next(available.Count)];
			GameObject placed = Instantiate(prefab, position, chest.transform.rotation);
			spawned++;
			placed.transform.rotation = Quaternion.LookRotation(normal);
			placed.transform.Rotate(placed.transform.forward, UnityEngine.Random.Range(0f, 360f), Space.World);
		}
		Debug.Log($"Shrines spawned: map={MapController.currentMap.name}, count={spawned}, requested={total}, choices={available.Count}");
	}

	private void SpawnOther()
	{
		if (MyAchievements.IsAchievementDone("a_bananarang")) return;
		Vector3 position = SpawnPositions.GetObjectSpawnPosition(worldCenter, worldArea, 0.5f,
			GameManager.Instance.whatIsGroundAndObjects, out _, 100, false, false, false, 44f);
		GameObject prefab = EffectManager.Instance.bananaQuest;
		Instantiate(prefab, position + Vector3.up * 2f, prefab.transform.rotation);
	}

	private void SpawnRails()
	{
		var staticRails = new List<GameObject>();
		for (int i = 0; i < numRails; i++)
		{
			for (int attempt = 0; attempt < 5; attempt++)
			{
				Vector3 position = SpawnPositions.GetObjectSpawnPosition(worldCenter, worldArea, 1f,
					GameManager.Instance.whatIsGroundAndObjects, out Vector3 normal, 50, true, false, false, 44f);
				if ((position - SpawnPositions.INVALID_POS).sqrMagnitude < 1e-10f) continue;
				GameObject prefab = rails[UnityEngine.Random.Range(0, rails.Length)];
				GameObject placed = Instantiate(prefab, position, prefab.transform.rotation);
				placed.SetActive(true);
				Vector3 reference = Vector3.Dot(normal, Vector3.right) > 0.99f ? Vector3.forward : Vector3.right;
				Vector3 tangent = Vector3.Cross(normal, reference).normalized;
				Vector3 forward = Vector3.Cross(tangent, normal).normalized;
				placed.transform.rotation = Quaternion.LookRotation(forward, normal);
				placed.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f), Space.Self);
				if (!placed.GetComponentInChildren<Rail>().IsValidPosition())
				{
					Destroy(placed);
					continue;
				}
				if (placed.isStatic) staticRails.Add(placed);
			}
		}
		if (staticRails.Count > 0)
		{
			var root = new GameObject("Rails 0");
			foreach (GameObject rail in staticRails) rail.transform.SetParent(root.transform);
			StaticBatchingUtility.Combine(root);
		}
	}
}
