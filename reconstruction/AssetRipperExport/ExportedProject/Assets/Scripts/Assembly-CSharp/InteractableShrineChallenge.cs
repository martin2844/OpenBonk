using System;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New.Summoners;
using Assets.Scripts.UI.Localization;
using UnityEngine;
using Utility;

public class InteractableShrineChallenge : BaseInteractable
{
	public GameObject minimapIcon;

	public GameObject alertIcon;

	private bool done;

	public GameObject fx;

	private HashSet<Enemy> enemies;

	public static Action A_Completed;

	private bool hasGivenReward;

	public static string debugName = "Challenges";

	public InteractableShrineChallenge()
	{
		isItemSource = true;
	}

	private void Awake()
	{
		Enemy.A_EnemyReleasedFromPool += EnemyDied;
	}

	private new void OnDestroy()
	{
		Enemy.A_EnemyReleasedFromPool -= EnemyDied;
	}

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		var summoner = new ChallengeSummoner(0, new List<EEnemy>());
		bool onlyElites = UnityEngine.Random.Range(0f, 1f) < .5f;
		enemies = new HashSet<Enemy>();
		Vector3 shrinePosition = transform.position;
		Vector3 direction = shrinePosition - MyPlayer.Instance.transform.position;
		direction.y = 0f;
		Vector3 rayOrigin = shrinePosition + direction.normalized * 10f + Vector3.up * 100f;
		Vector3 position = RaycastUtility.RayToGround(rayOrigin);
		if ((position - rayOrigin).sqrMagnitude < 1e-10f) position = shrinePosition;
		try
		{
			foreach (Enemy enemy in summoner.SpawnEnemies(onlyElites, position))
				if (enemy != null) enemies.Add(enemy);
		}
		finally
		{
			summoner.Cleanup();
		}
		foreach (Enemy enemy in enemies) enemy.MakeChallenge();
		if (fx != null) fx.SetActive(true);
		Destroy(minimapIcon);
		Destroy(alertIcon);
		base.OnDestroy();
		return true;
	}

	public override bool CanInteract()
	{
		return !done;
	}

	private void EnemyDied(Enemy enemy)
	{
		if (!done || hasGivenReward || enemies == null) return;
		enemies.Remove(enemy);
		if (enemies.Count > 0) return;
		hasGivenReward = true;
		if (EffectManager.Instance != null && enemy != null && EffectManager.Instance.openChestNormal != null)
			EffectManager.Instance.SpawnChest(EffectManager.Instance.openChestNormal, enemy.GetCenterPosition());
		Destroy(this);
		A_Completed?.Invoke();
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "SHRINE_CHALLENGE");
	}

	public override bool ShowInDebug()
	{
		return true;
	}

	public override string GetDebugName()
	{
		return debugName;
	}
}
