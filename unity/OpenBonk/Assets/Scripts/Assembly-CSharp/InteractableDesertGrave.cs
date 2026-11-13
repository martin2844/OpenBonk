using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableDesertGrave : BaseInteractable
{
	public EnemyData enemyData;

	public LocalizedString interactString;

	public GameObject chargeFx;

	public GameObject explodeFx;

	private Enemy myEnemy;

	private bool done;

	public ShrineSpawnAnimation nextShrine;

	public float speedMultiplier;

	public float sizeMultiplier;

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
		myEnemy = null;
		if (nextShrine != null)
		{
			nextShrine.Activate();
			if (MyPlayer.Instance != null && MyPlayer.Instance.minimapCameraScript != null)
				MyPlayer.Instance.minimapCameraScript.AddArrow(nextShrine.transform, Color.white);
			if (UiManager.Instance != null && UiManager.Instance.objectiveArrow != null)
				UiManager.Instance.objectiveArrow.SetTarget(nextShrine.transform, Vector3.up * 2f, 15f, 60f, .5f);
		}
		if (MyPlayer.Instance != null && MyPlayer.Instance.minimapCameraScript != null)
			MyPlayer.Instance.minimapCameraScript.RemoveArrow(transform);
		Enemy.A_EnemyDied -= OnEnemyDied;
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
		myEnemy = EnemyManager.Instance.SpawnBoss(enemyData.enemyName, 0, EEnemyFlag.Boss, transform.position, sizeMultiplier);
		if (myEnemy == null) return;
		myEnemy.teleportTime = .25f;
		myEnemy.SetSwarmMultiplierHp(100f);
		myEnemy.SetSpeedMultiplier(speedMultiplier);
		myEnemy.extraKnockbackRes = 3f;
		gameObject.SetActive(false);
	}

	public override string GetInteractString()
	{
		return interactString.GetLocalizedString();
	}
}
