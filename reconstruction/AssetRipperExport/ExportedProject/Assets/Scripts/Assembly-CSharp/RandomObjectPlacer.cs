using Assets.Scripts.Game.MapGeneration;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using System.Collections.Generic;
using UnityEngine;

public class RandomObjectPlacer : MonoBehaviour
{
	public Vector3 center;

	public Vector3 size;

	private int numChargeShrines;

	public RandomMapObject[] randomObjects;

	public RandomMapObject chargeShrineSpawns;

	public RandomMapObject greedShrineSpawns;

	private int index;

	public void GenerateInteractables(MapData mapData)
	{
		if (MapController.HasPlayerInventory() && MapController.GetPlayerInventory(null).itemInventory.GetItem(EItem.Beacon) is ItemBeacon beacon)
			chargeShrineSpawns.amount += beacon.GetExtraShrines();
		RandomMapObject[] objects = mapData.randomObjectsOverride.Length == 0 ? randomObjects : mapData.randomObjectsOverride;
		RandomObjectSpawner(chargeShrineSpawns, mapData.numShrinesPotsAndOtherMultiplier);
		RandomObjectSpawner(greedShrineSpawns, mapData.numShrinesPotsAndOtherMultiplier);
		Generate(objects, mapData.numShrinesPotsAndOtherMultiplier);
	}

	public void Generate(RandomMapObject[] objects, float amountMultiplier)
	{
		foreach (RandomMapObject obj in objects) RandomObjectSpawner(obj, amountMultiplier);
	}

	private void RandomObjectSpawner(RandomMapObject randomObject, float amountMultiplier = 1f)
	{
		string prefabName = randomObject.prefabs[0].name;
		if (SaveManager.Instance.config.cfGameSettings.enable_silver_pots == 0 &&
			(prefabName == "PotSmallSilver" || prefabName == "PotBigSilver")) return;
		int extra = 0;
		if (MapController.HasPlayerInventory() && MapController.GetPlayerInventory(null).itemInventory.GetItem(EItem.Pumpkin) is ItemPumpkin pumpkin)
		{
			if (prefabName == "PotSmall") extra += pumpkin.GetExtraPotsSmall();
			if (prefabName == "PotBig") extra += pumpkin.GetExtraPotsBig();
		}
		int count = Mathf.RoundToInt(randomObject.GetAmount() * amountMultiplier) + extra;
		var staticObjects = new List<GameObject>();
		for (int i = 0; i < count; i++)
		{
			float scale = Random.Range(randomObject.scaleMin, randomObject.scaleMax);
			Vector3 position = SpawnPositions.GetObjectSpawnPosition(center + Vector3.up * 999f, size,
				randomObject.checkRadius * scale, GameManager.Instance.whatIsGroundAndObjects,
				out Vector3 normal, 50, true, false, false, randomObject.maxSlopeAngle);
			if (randomObject is OpenBonkMapObjects.DecorativeObject && position == SpawnPositions.INVALID_POS) continue;
			GameObject prefab = randomObject.prefabs[Random.Range(0, randomObject.prefabs.Length)];
			GameObject placed = Instantiate(prefab, position + Vector3.up * randomObject.upOffset * scale, prefab.transform.rotation);
			Vector3 randomRotation = randomObject.randomRotationVector;
			if (randomObject.alignWithNormal)
			{
				placed.transform.rotation = Quaternion.LookRotation(normal);
				placed.transform.Rotate(new Vector3(randomRotation.x * Random.Range(0f, 360f),
					Random.Range(-180f * randomRotation.y, 180f * randomRotation.y),
					randomRotation.z * Random.Range(0f, 360f)), Space.Self);
			}
			else
			{
				Vector3 angles = placed.transform.eulerAngles;
				angles += new Vector3(randomRotation.x * Random.Range(0f, 360f),
					randomRotation.y * Random.Range(0f, 360f), randomRotation.z * Random.Range(0f, 360f));
				placed.transform.rotation = Quaternion.Euler(angles);
			}
			placed.transform.localScale *= scale;
			if (placed.isStatic) staticObjects.Add(placed);
		}
		if (staticObjects.Count > 0)
		{
			var parent = new GameObject($"RandomObjectsParent{index++}");
			foreach (GameObject obj in staticObjects) obj.transform.SetParent(parent.transform);
			StaticBatchingUtility.Combine(parent);
		}
	}

	private void OnDrawGizmosSelected()
	{
	}
}
