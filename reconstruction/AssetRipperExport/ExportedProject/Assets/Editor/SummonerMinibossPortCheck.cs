using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class SummonerMinibossPortCheck
{
	public static void Run()
	{
		GameObject root = new GameObject("miniboss check"); root.SetActive(false);
		GameObject playerObject = new GameObject("miniboss player"); playerObject.SetActive(false);
		EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
		MapData map = ScriptableObject.CreateInstance<MapData>();
		GameManager oldGame = GameManager.Instance; EnemyManager oldManager = EnemyManager.Instance; MyPlayer oldPlayer = MyPlayer.Instance;
		RunConfig oldConfig = MapController.runConfig; int oldIndex = MapController.index;
		float oldDifficulty = MyTime.difficultyTimer, oldSwarm = MyTime.finalSwarmTimer;
		try
		{
			GameManager.Instance = root.AddComponent<GameManager>(); EnemyManager.Instance = root.AddComponent<EnemyManager>();
			MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
			MyPlayer.Instance.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
			var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
			stats.stats = new Dictionary<EStat, float> { [EStat.EnemyHpMultiplier] = 1f, [EStat.EnemyScalingMultiplier] = 1f, [EStat.Difficulty] = 0f };
			MyPlayer.Instance.inventory.playerStats = stats;
			MapController.runConfig = new RunConfig { mapData = map }; MapController.index = 0; MyTime.difficultyTimer = 0f; MyTime.finalSwarmTimer = 0f;
			var summoner = (SummonerController)FormatterServices.GetUninitializedObject(typeof(SummonerController));
			EnemyManager.Instance.summonerController = summoner;
			Enemy enemy = root.AddComponent<Enemy>();
			data.enemyName = EEnemy.GhostKing; data.hp = 100; data.speed = 5f;
			typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
			typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, EEnemyFlag.SummonerMiniboss);
			enemy.enemyMovement = root.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = root.AddComponent<Rigidbody>();
			int events = 0; Action<Enemy> listener = value => { if (value == enemy) events++; }; Enemy.A_HealthChange += listener;
			try
			{
				float[] multipliers = { 3f, 9f, 20f, 40f, 1f };
				for (int count = 0; count < multipliers.Length; count++)
				{
					typeof(SummonerController).GetProperty("minibossCount").GetSetMethod(true).Invoke(summoner, new object[] { count });
					enemy.SetSummonerMiniboss();
					Check(Mathf.Abs(enemy.hp - 80f * multipliers[count]) < 0.001f && enemy.hp == enemy.maxHp, "health scaling for miniboss " + count);
				}
				Check(events == 5, "health change notifications");
			}
			finally { Enemy.A_HealthChange -= listener; }
			Debug.Log("SummonerMinibossPortCheck passed");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(map);
			GameManager.Instance = oldGame; EnemyManager.Instance = oldManager; MyPlayer.Instance = oldPlayer; MapController.runConfig = oldConfig; MapController.index = oldIndex;
			MyTime.difficultyTimer = oldDifficulty; MyTime.finalSwarmTimer = oldSwarm;
		}
	}
	private static void Check(bool value, string name) { if (!value) throw new Exception("SummonerMinibossPortCheck failed: " + name); }
}
