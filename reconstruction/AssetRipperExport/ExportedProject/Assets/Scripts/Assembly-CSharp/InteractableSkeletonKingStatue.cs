using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableSkeletonKingStatue : BaseInteractable
{
	public EnemyData enemyData;

	public LocalizedString interactString;

	public GameObject chargeFx;

	public GameObject explodeFx;

	private Enemy myEnemy;

	private bool done;

	private void Awake()
	{
		Enemy.A_EnemyDied += OnEnemyDied;
	}

	private new void OnDestroy()
	{
		Enemy.A_EnemyDied -= OnEnemyDied;
		base.OnDestroy();
	}

	private void OnEnemyDied(Enemy enemy, DamageContainer dc)
	{
		if (myEnemy == null || enemy != myEnemy) return;
		MyAchievements.TryUnlock("a_graveyard");
		if (MyPlayer.Instance != null && MyPlayer.Instance.minimapCameraScript != null)
			MyPlayer.Instance.minimapCameraScript.RemoveArrow(transform);
	}

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		chargeFx.SetActive(true);
		chargeFx.transform.SetParent(null);
		Invoke(nameof(SpawnEnemy), 1f);
		return true;
	}

	private void SpawnEnemy()
	{
		explodeFx.SetActive(true);
		explodeFx.transform.SetParent(null);
		myEnemy = EnemyManager.Instance.SpawnBoss(enemyData.enemyName, 0, EEnemyFlag.Boss, transform.position, 1f);
		if (myEnemy == null) return;
		myEnemy.teleportTime = .01f;
		myEnemy.SetSwarmMultiplierHp(65f);
		gameObject.SetActive(false);
	}

	public override string GetInteractString()
	{
		return interactString.GetLocalizedString();
	}
}
