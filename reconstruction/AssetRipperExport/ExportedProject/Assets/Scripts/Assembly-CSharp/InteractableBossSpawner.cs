using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.Localization;
using UnityEngine;

public class InteractableBossSpawner : BaseInteractable
{
	public GameObject minimapIcon;
	private List<Enemy> bossEnemies;
	public static Action A_BossSpawned;
	public static Action<bool> A_BossDefeated;
	public static Action<int> A_NumBossesDefeated;
	public GameObject preventObjectsSpawningHere;
	public GameObject portal;
	private int numBossesDefeated;
	public GameObject bossCurseFx;

	private void Awake()
	{
		Enemy.A_EnemyReleasedFromPool += OnEnemyReleasedFromPool;
		DetectInteractables.A_Interacted += OnInteractable;
	}

	private new void OnDestroy()
	{
		Enemy.A_EnemyReleasedFromPool -= OnEnemyReleasedFromPool;
		DetectInteractables.A_Interacted -= OnInteractable;
		base.OnDestroy();
	}

	private new void Start()
	{
		base.Start();
		preventObjectsSpawningHere.SetActive(false);
	}

	private void OnEnemyReleasedFromPool(Enemy enemy)
	{
		if (bossEnemies == null || !bossEnemies.Remove(enemy)) return;
		numBossesDefeated++;
		if (bossEnemies.Count != 0) return;
		PickupManager.Instance.PickupAllXp();
		bool opensPortal = CanSpawnPortal();
		if (opensPortal)
		{
			portal.SetActive(true);
			if (GameManager.Instance.uiManager != null && GameManager.Instance.uiManager.objectiveArrow != null)
				GameManager.Instance.uiManager.objectiveArrow.SetTarget(portal.transform, Vector3.up * 2f, 8f, 60f);
		}
		A_BossDefeated?.Invoke(opensPortal);
		A_NumBossesDefeated?.Invoke(numBossesDefeated);
	}

	private bool CanSpawnPortal()
	{
		return !MapController.IsLastStage() && MapController.index < MapController.runConfig.mapTierIndex;
	}

	public override bool Interact()
	{
		if (bossEnemies != null) return false;
		Vector3 direction = transform.position - GameManager.Instance.GetPlayer().transform.position;
		direction.y = 0f;
		bossEnemies = EnemyManager.Instance.summonerController.SpawnStageBoss(transform.position + direction.normalized * 7f);
		if (bossEnemies.Count == 0) { bossEnemies = null; return false; }
		minimapIcon.SetActive(false);
		gameObject.tag = "Untagged";
		A_BossSpawned?.Invoke();
		return true;
	}

	private void OnInteractable(BaseInteractable interactable, bool success)
	{
		if (success && interactable is InteractableShrineCursed && bossCurseFx != null)
			bossCurseFx.SetActive(true);
	}
	public override string GetInteractString() => LocalizationUtility.GetLocalizedString("Game_Interactables", "SPAWN_BOSS");
}
