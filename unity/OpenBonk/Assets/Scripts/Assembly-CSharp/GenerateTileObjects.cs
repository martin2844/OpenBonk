using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;

public class GenerateTileObjects : MonoBehaviour
{
	public GameObject tileObjectsParent;

	public GameObject bossSpawner;

	public GameObject bossSpawnerFinal;

	public GameObject graveyardBossPortal;

	public GameObject arrowPrefabTest;

	public void Generate(List<GameObject> allFlatTiles, StageData stageData)
	{
		if (tileObjectsParent != null) DestroyImmediate(tileObjectsParent);
		tileObjectsParent = new GameObject("TileObjectsParent");
		var available = new List<GameObject>(allFlatTiles);
		available.RemoveAt(0);
		GameObject lastTile = available[available.Count - 1];
		available.RemoveAt(available.Count - 1);
		MapSpecificTiles(available, stageData);
		if (MapController.currentMap.eMap == EMap.Graveyard)
		{
			Vector3 direction = -lastTile.GetComponent<ProceduralTile>().parentDir;
			direction.y = 0f;
			Instantiate(graveyardBossPortal, lastTile.transform.position, Quaternion.LookRotation(direction, Vector3.up));
		}
		else
		{
			bool customBoss = stageData.stageTimeline?.boss is OpenBonkMapEnemyData;
			GameObject boss = MapController.IsLastStage() && !customBoss ? bossSpawnerFinal : bossSpawner;
			Instantiate(boss, lastTile.transform.position, boss.transform.rotation);
			if (MapController.currentMap is OpenBonkExternalMapData map && map.arenaLandmarks.TryGetValue(stageData, out var landmark))
			{
				Vector3 route = lastTile.GetComponent<ProceduralTile>().parentDir;
				if (route == Vector3.zero) route = Vector3.forward;
				Vector3 position = lastTile.transform.position + route * (stageData.mapParameters.scaledTileWidth * .29f);
				Instantiate(landmark, position, Quaternion.LookRotation(-route), tileObjectsParent.transform);
			}
		}
		GameObject[] prefabs = stageData.stageTilePrefabs.flatTilePrefabs;
		if (prefabs.Length == 0) return;
		int count = (int)(available.Count * stageData.stageTilePrefabs.populateTilesRatio);
		var positions = new List<Vector3>();
		for (int i = 0; i < count; i++)
		{
			int selected = Utility.MyRandom.random.Next(0, available.Count);
			positions.Add(available[selected].transform.position);
			available.RemoveAt(selected);
		}
		foreach (Vector3 position in positions)
		{
			GameObject prefab = prefabs[Utility.MyRandom.random.Next(0, prefabs.Length)];
			GameObject obj = Instantiate(prefab, position, prefab.transform.rotation);
			obj.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
			obj.SetActive(true);
			obj.transform.SetParent(tileObjectsParent.transform);
		}
		foreach (MeshFilter mesh in tileObjectsParent.GetComponentsInChildren<MeshFilter>())
			if (mesh.transform.parent != null && !mesh.gameObject.isStatic && !mesh.CompareTag("Ignore"))
				mesh.transform.SetParent(null);
		StaticBatchingUtility.Combine(tileObjectsParent);
	}

	private void MapSpecificTiles(List<GameObject> available, StageData stageData)
	{
		var prefabs = stageData.stageTilePrefabs.mapSpecificTilesPrefabs;
		if (prefabs.Length == 0) return;
		List<int> order = Enumerable.Range(0, available.Count).OrderBy(_ => Random.value).ToList();
		var selected = new List<GameObject>();
		for (int i = 1; i < order.Count; i++)
		{
			int index = order[i];
			if (index == 0) continue;
			ProceduralTile tile = available[index].GetComponent<ProceduralTile>();
			ProceduralTile previous = available[index - 1].GetComponent<ProceduralTile>();
			if (!tile.isFlat) continue;
			Vector3 direction = previous.dir;
			direction.y = 0f;
			Instantiate(prefabs[0], available[index].transform.position, Quaternion.LookRotation(direction, Vector3.up));
			selected.Add(available[index]);
			if (selected.Count >= stageData.stageTilePrefabs.numSpecificTiles) break;
		}
		foreach (GameObject tile in selected) available.Remove(tile);
	}
}
