using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class DungeonSnekSpawner : MonoBehaviour
{
	public GameObject snek;

	public GameObject chest;

	private void Start()
	{
		if (SaveManager.Instance == null || SaveManager.Instance.progression == null) return;
		bool spawnSnek = !MyAchievements.IsAchievementDone("a_snek") && RsgController.isCurrentMapRandomSeed;
		if (snek != null) snek.SetActive(spawnSnek);
		if (chest != null) chest.SetActive(!spawnSnek);
	}
}
