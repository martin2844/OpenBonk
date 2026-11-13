using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public static class GraveyardBossRoomPortCheck
{
	public static void Run()
	{
		GameObject roomObject = new GameObject("boss room check"); roomObject.SetActive(false);
		GameObject enemiesObject = new GameObject("enemy manager check"); enemiesObject.SetActive(false);
		GameObject enemyObject = new GameObject("boss enemy check"); enemyObject.SetActive(false);
		StageData stage = ScriptableObject.CreateInstance<StageData>();
		EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
		StageData oldStage = MapController.currentStage;
		EnemyManager oldManager = EnemyManager.Instance;
		Action oldBossDied = GraveyardBossRoom.A_BossDied;
		Action<bool> oldDefeated = InteractableBossSpawner.A_BossDefeated;
		float oldTime = MyTime.time, oldStageTimer = MyTime.stageTimer, oldDelta = MyTime.deltaTime;
		Color oldAmbient = RenderSettings.ambientLight, oldFog = RenderSettings.fogColor;
		float oldDarkness = GraveyardBossRoom.GetDarknessIntensityMultiplier();
		try
		{
			EnemyManager manager = enemiesObject.AddComponent<EnemyManager>();
			EnemyManager.Instance = manager;
			GraveyardBossRoom room = roomObject.AddComponent<GraveyardBossRoom>();
			room.interactableGhostBossLeave = roomObject.AddComponent<InteractableGhostBossLeave>();
			room.risingObjects = new GameObject("rising objects").transform; room.risingObjects.SetParent(roomObject.transform);
			room.lava = new GameObject("lava").transform; room.lava.SetParent(roomObject.transform);
			room.pillars = new GameObject("pillars").transform; room.pillars.SetParent(roomObject.transform);
			room.lamps = new BossLamp[1];
			room.directionalLight = new GameObject("boss light").AddComponent<Light>(); room.directionalLight.transform.SetParent(roomObject.transform);
			room.metalGate = new GameObject("boss gate").AddComponent<GraveyardBossMetalGate>(); room.metalGate.transform.SetParent(roomObject.transform);
			GameObject shieldParent = new GameObject("boss shields parent"); shieldParent.transform.SetParent(roomObject.transform);
			room.bossShields = new GameObject("boss shields").AddComponent<AegisRenderer>(); room.bossShields.transform.SetParent(shieldParent.transform);
			room.bossShields.prefab = new GameObject("shield prefab"); room.bossShields.prefab.transform.SetParent(room.bossShields.transform);
			room.bossShields.forwardOffset = 2f;
			stage.lightIntensity = 0.8f;
			stage.ambienceColor = Color.white; stage.fogColor = Color.white;
			typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { stage });
			MyTime.time = 10f; MyTime.stageTimer = 30f;
			room.Activate();
			Check(room.isFightingBoss && MyTime.stageTimer == 0f, "activation starts boss phase");
			Check((float)Get(room, "spawnBossAtTime") == 13f && (float)Get(room, "lavaStartTime") == 40f, "original spawn and lava timing");
			Check(!room.interactableGhostBossLeave.CanInteract(), "exit locked before victory");
			MyTime.time = 39f; MyTime.deltaTime = 0f; Method("UpdateLava").Invoke(room, null);
			Check(Get(room, "lavaState").ToString() == "Idle", "lava waits until scheduled start");
			MyTime.time = 40f; MyTime.deltaTime = 2f; Method("UpdateLava").Invoke(room, null);
			Check(Get(room, "lavaState").ToString() == "Rising" && Mathf.Abs(room.risingObjects.position.y - 6.5f) < 0.001f && room.lava.position.y == 0f && room.pillars.position.y == 0f, "staggered lava rise");
			MyTime.deltaTime = 6.5f; Method("UpdateLava").Invoke(room, null);
			Check(Get(room, "lavaState").ToString() == "UpIdle" && room.pillars.position.y == 13f && room.lava.position.y == 13f, "lava reaches upper position");
			MyTime.time = 65f; MyTime.deltaTime = 2f; Method("UpdateLava").Invoke(room, null);
			Check(Get(room, "lavaState").ToString() == "Lowering" && Mathf.Abs(room.lava.position.y - 6.5f) < 0.001f && room.pillars.position.y == 13f && room.risingObjects.position.y == 13f, "staggered lava lowering");
			MyTime.deltaTime = 5.5f; Method("UpdateLava").Invoke(room, null);
			Check(Get(room, "lavaState").ToString() == "Idle" && room.risingObjects.position.y == 0f && (float)Get(room, "lavaStartTime") == 95f, "lava returns and schedules next cycle");
			MyTime.time = 10f;
			Enemy enemy = enemyObject.AddComponent<Enemy>();
			enemy.renderer = enemyObject.AddComponent<MeshRenderer>();
			enemyObject.transform.position = new Vector3(2f, 3f, 4f);
			data.enemyName = EEnemy.GhostKing;
			typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
			enemy.hp = 0f;
			Set(room, "bossEnemy", enemy);
			Method("Awake").Invoke(room, null);
			Enemy.A_Damage?.Invoke(enemy, new DamageContainer(0f, "Unkown"));
			Check(!(bool)Get(room, "hasUsedDamageOtherThanLamps"), "lamp-only damage does not set achievement flag");
			Enemy.A_Damage?.Invoke(enemy, new DamageContainer(0f, "Fire Staff"));
			Check((bool)Get(room, "hasUsedDamageOtherThanLamps"), "other damage is tracked");
			int armorEvents = 0;
			Action<Enemy, int, int> armorListener = (changed, current, max) => { if (changed == enemy && current == 1 && max == 1) armorEvents++; };
			Enemy.A_ArmorChanged += armorListener;
			try
			{
				Method("RefreshBossArmor").Invoke(room, null);
				Check(Mathf.Abs((float)Get(enemy, "armor") - 0.99f) < 0.0001f && enemy.armorCurrent == 1 && enemy.armorMax == 1 && armorEvents == 1, "unlit boss armor and event");
				Check(room.bossShields.prefab.activeSelf && Vector3.Distance(room.bossShields.prefab.transform.localPosition, Vector3.forward * 2f) < 0.001f, "shield is placed at radius");
				room.bossShields.SetAmount(4);
				var shields = (System.Collections.Generic.List<GameObject>)Get(room.bossShields, "prefabs");
				Check(shields.Count == 4 && Vector3.Distance(shields[1].transform.localPosition, Vector3.right * 2f) < 0.001f, "shields spread around ring");
				room.bossShields.SetAmount(2);
				Check(shields.Count == 4 && shields[1].activeSelf && !shields[2].activeSelf, "shield objects are reused and hidden");
				Method("UpdateShields").Invoke(room, null);
				Check(shieldParent.activeSelf && shieldParent.transform.position == enemyObject.transform.position && Mathf.Abs(shieldParent.transform.eulerAngles.y - 2f) < 0.01f, "shield follows and rotates with boss");
				Method("LampActivate").Invoke(room, null);
				Check(Mathf.Abs((float)Get(enemy, "armor") - 0.95f) < 0.0001f && enemy.armorCurrent == 0, "lamp reduces boss armor");
				Method("LampDeactivate").Invoke(room, null);
				Method("OnLightIntensityChangeFromDarknessAttack").Invoke(room, new object[] { 0.5f });
				Check(GraveyardBossRoom.IsDarknessAttack() && Mathf.Abs(room.directionalLight.intensity - 0.24f) < 0.001f, "darkness changes lighting");
				Method("OnLightIntensityChangeFromDarknessAttack").Invoke(room, new object[] { 1f });
			}
			finally { Enemy.A_ArmorChanged -= armorListener; }
			typeof(GraveyardBossRoom).GetProperty("hasSpawnedBoss").GetSetMethod(true).Invoke(room, new object[] { true });
			int bossEvents = 0, defeatEvents = 0;
			GraveyardBossRoom.A_BossDied = () => bossEvents++;
			InteractableBossSpawner.A_BossDefeated = opened => { if (!opened) defeatEvents++; };
			Method("OnEnemyDied").Invoke(room, new object[] { enemy, null });
			Enemy.A_EnemyReleasedFromPool?.Invoke(enemiesObject.AddComponent<Enemy>());
			Check(!room.isBossDefeated, "other enemy release does not end fight");
			Enemy.A_EnemyReleasedFromPool?.Invoke(enemy);
			Method("CheckIfBossDead").Invoke(room, null);
			Method("CheckIfBossDead").Invoke(room, null);
			Check(bossEvents == 1 && defeatEvents == 1 && room.isBossDefeated && !room.isFightingBoss, "boss death and defeat events");
			Check(room.interactableGhostBossLeave.CanInteract(), "victory unlocks boss exit");
			var lavaStateField = typeof(GraveyardBossRoom).GetField("lavaState", BindingFlags.Instance | BindingFlags.NonPublic);
			lavaStateField.SetValue(room, Enum.Parse(lavaStateField.FieldType, "UpIdle")); MyTime.deltaTime = 0f;
			Method("UpdateLava").Invoke(room, null);
			Check(Get(room, "lavaState").ToString() == "Lowering", "victory lowers raised lava immediately");
			Method("UpdateShields").Invoke(room, null);
			Check(!shieldParent.activeSelf, "shields hide after boss defeat");
			MyTime.time = 12.25f;
			Method("PostBossDeadUpdate").Invoke(room, null);
			Check(Mathf.Abs(room.directionalLight.intensity - 0.64f) < 0.001f, "lighting fades after victory");
			Check(!(bool)Get(room.metalGate, "isOpen"), "gate remains closed before delay");
			MyTime.time = 13f;
			Method("PostBossDeadUpdate").Invoke(room, null);
			Check((bool)Get(room.metalGate, "isOpen"), "gate opens after delay");
			Check((Vector3)Get(room.metalGate, "endPos") == room.metalGate.transform.position + Vector3.up * 12f && (float)Get(room.metalGate, "moveOverTime") == 2f, "gate rise distance and duration");
			Method("OnDestroy").Invoke(room, null);
			Debug.Log("GraveyardBossRoomPortCheck passed");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(roomObject);
			RenderSettings.ambientLight = oldAmbient; RenderSettings.fogColor = oldFog;
			typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { oldStage });
			EnemyManager.Instance = oldManager; GraveyardBossRoom.A_BossDied = oldBossDied; InteractableBossSpawner.A_BossDefeated = oldDefeated;
			MyTime.time = oldTime; MyTime.stageTimer = oldStageTimer; MyTime.deltaTime = oldDelta;
			typeof(GraveyardBossRoom).GetField("darknessLightIntensityMultiplier", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, oldDarkness);
			UnityEngine.Object.DestroyImmediate(enemiesObject); UnityEngine.Object.DestroyImmediate(enemyObject);
			UnityEngine.Object.DestroyImmediate(stage); UnityEngine.Object.DestroyImmediate(data);
		}
	}
	private static MethodInfo Method(string name) => typeof(GraveyardBossRoom).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
	private static object Get(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
	private static void Set(object value, string name, object field) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, field);
	private static void Check(bool value, string name) { if (!value) throw new Exception("GraveyardBossRoomPortCheck failed: " + name); }
}
