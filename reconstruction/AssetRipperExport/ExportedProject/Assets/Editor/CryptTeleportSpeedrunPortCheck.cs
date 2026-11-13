using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

public static class CryptTeleportSpeedrunPortCheck
{
	public static void Run()
	{
		GameObject root = new GameObject("crypt teleport check");
		root.SetActive(false);
		GameObject managerObject = new GameObject("speedrun manager check");
		managerObject.SetActive(false);
		GameObject saveObject = new GameObject("crypt record check");
		saveObject.SetActive(false);
		GameManager oldManager = GameManager.Instance;
		MyPlayer oldPlayer = MyPlayer.Instance;
		float oldRunTime = MyTime.runTimer;
		float oldCryptTime = MyTime.cryptTimer;
		SaveManager oldSave = SaveManager.Instance;
		bool oldLoaded = SaveManager.loaded;
		Action oldDied = PlayerHealth.A_Died;
		Action oldStage = GameManager.A_StageStarted;
		Action<bool> oldBoss = InteractableBossSpawner.A_BossDefeated;
		Action<bool> oldFinal = FinalFightController.A_BossDefeated;
		ChallengeModifierSpeedrun speedrun = ScriptableObject.CreateInstance<ChallengeModifierSpeedrun>();
		try
		{
			Rigidbody body = root.AddComponent<Rigidbody>();
			body.useGravity = false;
			PlayerMovement movement = root.AddComponent<PlayerMovement>();
			Set(movement, "rb", body);
			Set(movement, "playerHeight", 2f);
			body.velocity = new Vector3(1f, 2f, 3f);
			movement.TeleportPlayer(new Vector3(4f, 5f, 6f));
			Check(body.velocity == Vector3.zero, "teleport clears velocity");
			Set(movement, "leftGroundAtTime", MyTime.time);
			Set(movement, "usedJumps", 3);
			body.velocity = Vector3.one;
			movement.TeleportPlayerBackToBounds(Vector3.zero);
			Check(body.velocity == Vector3.zero && (int)Get(movement, "usedJumps") == 0, "boss exit resets velocity and jumps");
			InteractableGhostBossLeave ghostExit = root.AddComponent<InteractableGhostBossLeave>();
			Check(!ghostExit.CanInteract(), "boss exit starts locked");
			ghostExit.OpenDoor();
			Check(ghostExit.CanInteract(), "boss exit unlocks");
			InteractableCryptLeave exit = root.AddComponent<InteractableCryptLeave>();
			exit.SetType(RsgController.EDungeonType.BossDungeon);
			exit.SetTeleportTransform(Vector3.one, Vector3.forward, Vector3.right);
			Check(exit.CanInteract() && (RsgController.EDungeonType)Get(exit, "dungeonType") == RsgController.EDungeonType.BossDungeon, "exit availability and type");
			Check((Vector3)Get(exit, "teleportPosition") == Vector3.one && (Vector3)Get(exit, "teleportDirCamera") == Vector3.right, "exit teleport configuration");
			Set(exit, "hasInteracted", true);
			Check(!exit.CanInteract(), "exit prevents repeat interaction");
			SaveManager save = saveObject.AddComponent<SaveManager>();
			save.stats = new StatsSaveFile();
			save.stats.times[ESpeedrunTime.Crypt] = 0f;
			typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
			SaveManager.loaded = false;
			MethodInfo showTime = typeof(InteractableCryptLeave).GetMethod("ShowTime", BindingFlags.Instance | BindingFlags.NonPublic);
			MyTime.cryptTimer = 50f; showTime.Invoke(exit, null);
			MyTime.cryptTimer = 60f; showTime.Invoke(exit, null);
			Check(save.stats.times[ESpeedrunTime.Crypt] == 50f, "slower crypt does not replace record");
			MyTime.cryptTimer = 40f; showTime.Invoke(exit, null);
			Check(save.stats.times[ESpeedrunTime.Crypt] == 40f, "faster crypt replaces record");
			GameManager game = managerObject.AddComponent<GameManager>();
			GameManager.Instance = game;
			game.StartPlaying();
			MyPlayer player = root.AddComponent<MyPlayer>();
			MyPlayer.Instance = player;
			PlayerInventory inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
			PlayerHealth health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
			health.hp = 10; health.shield = 3; health.overheal = 2;
			inventory.playerHealth = health;
			player.inventory = inventory;
			int deaths = 0;
			PlayerHealth.A_Died = () => deaths++;
			GameManager.A_StageStarted = null; InteractableBossSpawner.A_BossDefeated = null; FinalFightController.A_BossDefeated = null;
			speedrun.timeLimitMinutes = 2f;
			speedrun.Init(null);
			MyTime.runTimer = 25.3f;
			GameManager.A_StageStarted();
			Check(speedrun.timeLeftOnStage == 95f, "stage start time remaining");
			MyTime.runTimer = 119f; speedrun.Tick();
			Check(health.hp == 10 && deaths == 0, "alive before time limit");
			MyTime.runTimer = 120f; speedrun.Tick(); speedrun.Tick();
			Check(health.hp == 0 && health.shield == 0f && health.overheal == 0f && deaths == 1, "time limit kills once");
			health.hp = 10;
			FinalFightController.A_BossDefeated(false);
			speedrun.Tick();
			Check(health.hp == 10 && deaths == 1, "boss victory stops timer death");
			Debug.Log("CryptTeleportSpeedrunPortCheck passed");
		}
		finally
		{
			speedrun.Cleanup();
			GameManager.Instance = oldManager; MyPlayer.Instance = oldPlayer; MyTime.runTimer = oldRunTime; MyTime.cryptTimer = oldCryptTime;
			typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); SaveManager.loaded = oldLoaded;
			PlayerHealth.A_Died = oldDied; GameManager.A_StageStarted = oldStage;
			InteractableBossSpawner.A_BossDefeated = oldBoss; FinalFightController.A_BossDefeated = oldFinal;
			UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(managerObject); UnityEngine.Object.DestroyImmediate(saveObject); UnityEngine.Object.DestroyImmediate(speedrun);
		}
	}
	private static object Get(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
	private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
	private static void Check(bool value, string description) { if (!value) throw new Exception("CryptTeleportSpeedrunPortCheck failed: " + description); }
}
