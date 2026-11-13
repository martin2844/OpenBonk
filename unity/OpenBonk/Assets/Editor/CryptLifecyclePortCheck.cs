using System;
using System.Reflection;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts.Game.Spawning;
using UnityEngine;

public static class CryptLifecyclePortCheck
{
	public static void Run()
	{
		GameObject gameObject = new GameObject("crypt lifecycle check");
		GameObject enemiesObject = new GameObject("crypt enemies check");
		GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
		GameManager oldGame = GameManager.Instance;
		EnemyManager oldEnemies = EnemyManager.Instance;
		RsgController oldRsg = RsgController.Instance;
		Action oldStart = GameManager.A_DungeonStarted;
		Action oldEnd = GameManager.A_DungeonEnded;
		float oldTimer = MyTime.cryptTimer;
		int enemyLayer = LayerMask.NameToLayer("Enemy");
		int groundLayer = LayerMask.NameToLayer("Ground");
		int objectLayer = LayerMask.NameToLayer("Object");
		bool oldGround = Physics.GetIgnoreLayerCollision(enemyLayer, groundLayer);
		bool oldObject = Physics.GetIgnoreLayerCollision(enemyLayer, objectLayer);
		try
		{
			GameManager.Instance = null;
			EnemyManager.Instance = null;
			RsgController.Instance = null;
			GameManager game = gameObject.AddComponent<GameManager>();
			EnemyManager enemies = enemiesObject.AddComponent<EnemyManager>();
			GameManager.Instance = game;
			EnemyManager.Instance = enemies;
			floor.layer = groundLayer;
			floor.transform.position = new Vector3(0f, -0.5f, 0f);
			floor.transform.localScale = new Vector3(100f, 1f, 100f);
			Physics.SyncTransforms();
			game.whatIsGround = 1 << groundLayer;
			game.whatIsGroundAndObjects = 1 << groundLayer;
			Check(SpawnPositions.GetEnemySpawnPositionAroundPoint(Vector3.zero, 5f, 10f, 0) == SpawnPositions.INVALID_POS, "spawn attempt exhaustion");
			Vector3 spawn = SpawnPositions.GetEnemySpawnPositionAroundPoint(Vector3.zero, 5f, 10f, 10);
			float distance = new Vector2(spawn.x, spawn.z).magnitude;
			Check(Mathf.Abs(spawn.y) < 0.01f && distance >= 4.99f && distance <= 10.01f, "ground spawn near point");
			floor.tag = "Ignore";
			Check(SpawnPositions.GetEnemySpawnPositionAroundPoint(Vector3.zero, 5f, 10f, 2) == SpawnPositions.INVALID_POS, "ignored spawn surface");
			int started = 0, ended = 0;
			GameManager.A_DungeonStarted = () => started++;
			GameManager.A_DungeonEnded = () => ended++;
			MyTime.cryptTimer = 42f;
			game.StartDungeon(37f);
			Check(game.isCrypt && !game.isDungeonOvertime && game.cryptIndex == 0, "dungeon start state");
			Check(game.dungeonTimeToComplete == 37f && MyTime.cryptTimer == 0f && started == 1, "start timer and event");
			Check(Physics.GetIgnoreLayerCollision(enemyLayer, groundLayer) && Physics.GetIgnoreLayerCollision(enemyLayer, objectLayer), "dungeon collision masks");
			game.StartDungeonTimer();
			Check(game.isDungeonTimerStarted, "timer started");
			game.StopDungeon();
			Check(!game.isCrypt && !game.isDungeonOvertime && ended == 1, "dungeon stop state and event");
			Check(!Physics.GetIgnoreLayerCollision(enemyLayer, groundLayer) && !Physics.GetIgnoreLayerCollision(enemyLayer, objectLayer), "restored collision masks");
			RsgController rsg = gameObject.AddComponent<RsgController>();
			RsgController.SetCustomSeed(123);
			Check((bool)typeof(RsgController).GetProperty("HasCustomSeed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rsg), "custom seed");
			RsgController.SetCustomSeed(0);
			Check(!(bool)typeof(RsgController).GetProperty("HasCustomSeed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rsg), "cleared custom seed");
			rsg.minPieces = 5; rsg.maxPieces = 20;
			typeof(RsgController).GetField("random", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new ConsistentRandom(42));
			int length = (int)typeof(RsgController).GetMethod("FindMapLength", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rsg, null);
			Check(length == new ConsistentRandom(42).Next(5, 20), "seeded map length");
			Debug.Log("CryptLifecyclePortCheck passed");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(gameObject);
			UnityEngine.Object.DestroyImmediate(enemiesObject);
			UnityEngine.Object.DestroyImmediate(floor);
			GameManager.Instance = oldGame; EnemyManager.Instance = oldEnemies; RsgController.Instance = oldRsg;
			GameManager.A_DungeonStarted = oldStart; GameManager.A_DungeonEnded = oldEnd;
			MyTime.cryptTimer = oldTimer;
			Physics.IgnoreLayerCollision(enemyLayer, groundLayer, oldGround);
			Physics.IgnoreLayerCollision(enemyLayer, objectLayer, oldObject);
			RsgController.SetCustomSeed(0);
		}
	}
	private static void Check(bool condition, string name) { if (!condition) throw new Exception("CryptLifecyclePortCheck failed: " + name); }
}
