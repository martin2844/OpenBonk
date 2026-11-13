using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using MilkShake;
using UnityEngine;

public static class GhostKingDarknessPortCheck
{
	public static void Run()
	{
		GameObject root = new GameObject("darkness check"); root.SetActive(false);
		GameObject bossObject = new GameObject("darkness boss"); bossObject.SetActive(false);
		GameObject cameraObject = new GameObject("darkness camera"); cameraObject.SetActive(false);
		GameObject managerObject = new GameObject("darkness managers"); managerObject.SetActive(false);
		EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
		ShakePreset shake = ScriptableObject.CreateInstance<ShakePreset>();
		GameManager oldManager = GameManager.Instance;
		PoolManager oldPool = PoolManager.Instance;
		PlayerCamera oldCamera = PlayerCamera.Instance;
		MyPlayer oldPlayer = MyPlayer.Instance;
		PlayerInventory inventory = null;
		float oldDelta = MyTime.fixedDeltaTime, oldTime = MyTime.time;
		bool oldInside = GraveyardBossRoom.isPlayerInsideLight;
		try
		{
			Enemy boss = bossObject.AddComponent<Enemy>();
			boss.renderer = bossObject.AddComponent<MeshRenderer>();
			boss.enemyMovement = bossObject.AddComponent<EnemyMovementRb>();
			boss.enemyMovement.rb = bossObject.AddComponent<Rigidbody>(); boss.enemyMovement.rb.isKinematic = true;
			boss.hp = 50f; boss.maxHp = 100f;
			Check(boss.GetHpRatio() == 0.5f, "boss health ratio");
			Transform target = new GameObject("boss target").transform; target.SetParent(root.transform);
			PlayerCamera.Instance = cameraObject.AddComponent<PlayerCamera>(); PlayerCamera.Instance.shaker = cameraObject.AddComponent<Shaker>();
			GhostKingAttackDarkness attack = root.AddComponent<GhostKingAttackDarkness>();
			attack.shakePreset = shake;
			attack.blastSphere = Child(root, "blast"); attack.blastSphere.SetActive(false);
			attack.audioStart = Child(root, "start").AddComponent<AudioSource>();
			attack.audioCharge = Child(root, "charge").AddComponent<AudioSource>();
			attack.audioBlast = Child(root, "blast audio").AddComponent<AudioSource>();
			attack.audioLampSave = Child(root, "save").AddComponent<AudioSource>();
			attack.blastParticles = Child(root, "blast particles").AddComponent<ParticleSystem>();
			attack.chargeParticles = Array.Empty<ParticleSystem>();
			int targeting = 0, explosions = 0; float light = -1f;
			Action targetHandler = () => { targeting++; boss.FollowTarget(target); };
			Action explodeHandler = () => explosions++;
			Action<float> lightHandler = value => light = value;
			GhostKingAttackDarkness.A_DarknessAttackSetEnemyTarget += targetHandler;
			GhostKingAttackDarkness.A_Explode += explodeHandler;
			GhostKingAttackDarkness.A_LightIntensity += lightHandler;
			try
			{
				attack.Set(new EnemySpecialAttack { attackChargeTime = 2f }, boss);
				Check(targeting == 1 && boss.state == EEnemyState.FollowTarget && boss.followTarget == target && !boss.enemyMovement.rb.isKinematic, "attack retargets boss");
				MyTime.fixedDeltaTime = 1f; Invoke(attack, "FixedUpdate");
				Check(light > 0f && light < 1f && !(bool)Get(attack, "hasBlasted"), "charging dims room");
				Invoke(attack, "FixedUpdate");
				Check((bool)Get(attack, "hasBlasted") && attack.blastSphere.activeSelf && explosions == 0, "blast precedes damage");
				GraveyardBossRoom.isPlayerInsideLight = true;
				MyTime.fixedDeltaTime = 0.2f; Invoke(attack, "FixedUpdate");
				Check(explosions == 1 && (bool)Get(attack, "hasDamaged"), "sheltered blast dispatches once");
				MyTime.fixedDeltaTime = 1.4f; Invoke(attack, "FixedUpdate");
				Check(explosions == 1 && light == 1f && !attack.blastSphere.activeSelf, "attack restores light and ends");
				GameManager.Instance = managerObject.AddComponent<GameManager>();
				var player = managerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
				inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
				inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects();
				inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
				inventory.playerStats.stats = new Dictionary<EStat, float>();
				foreach (EStat stat in Enum.GetValues(typeof(EStat))) inventory.playerStats.stats[stat] = 1f;
				inventory.playerStats.stats[EStat.MaxHealth] = 100f; inventory.playerStats.stats[EStat.Shield] = 0f;
				inventory.playerStats.stats[EStat.Armor] = 0f; inventory.playerStats.stats[EStat.DamageReductionMultiplier] = 0f;
				inventory.playerStats.stats[EStat.Evasion] = 0f;
				inventory.playerHealth = new PlayerHealth(inventory.playerStats);
				GraveyardBossRoom.isPlayerInsideLight = false;
				boss.hp = 75f; MyTime.time = 10f; Invoke(attack, "BlastDamage");
				Check(Mathf.Abs(inventory.playerHealth.hp - 25f) < .01f, "unprotected player takes 75 percent at boss threshold");
				inventory.playerHealth.OnDestroy(); inventory.playerHealth = new PlayerHealth(inventory.playerStats);
				boss.hp = 74f; MyTime.time = 20f; Invoke(attack, "BlastDamage");
				Check(inventory.playerHealth.hp <= 0f, "unprotected player takes lethal damage below boss threshold");
				PoolManager.Instance = managerObject.AddComponent<PoolManager>();
				var config = new EnemySpecialAttack { isEnabled = true, attackName = "darkness fixture", attackPrefab = root, attackChargeTime = 2f, triggerDistance = 100f, attackCooldown = 10f };
				enemyData.specialAttacks = new[] { config };
				typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(boss, new object[] { enemyData });
				MyTime.time = 0f; boss.state = EEnemyState.Default;
				SpecialAttackController controller = new SpecialAttackController(boss);
				typeof(SpecialAttackController).GetField("nextCheckTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, 0f);
				controller.Tick();
				Check((bool)Get(controller, "isAttacking") && PoolManager.Instance.enemyAttacksPools[config.attackName].CountActive == 1 && boss.state == EEnemyState.Idle, "scheduler selects darkness prefab");
			}
			finally
			{
				GhostKingAttackDarkness.A_DarknessAttackSetEnemyTarget -= targetHandler;
				GhostKingAttackDarkness.A_Explode -= explodeHandler;
				GhostKingAttackDarkness.A_LightIntensity -= lightHandler;
			}
			Debug.Log("GhostKingDarknessPortCheck passed");
		}
		finally
		{
			foreach (GhostKingAttackDarkness clone in Resources.FindObjectsOfTypeAll<GhostKingAttackDarkness>())
				if (clone != null && clone.gameObject.name == "darkness check(Clone)") UnityEngine.Object.DestroyImmediate(clone.gameObject);
			UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(bossObject); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(managerObject); UnityEngine.Object.DestroyImmediate(enemyData);
			UnityEngine.Object.DestroyImmediate(shake);
			inventory?.playerHealth?.OnDestroy(); inventory?.statusEffects?.OnDestroy(); inventory?.itemInventory?.Cleanup(); MyPlayer.Instance = oldPlayer;
			GameManager.Instance = oldManager; PoolManager.Instance = oldPool;
			PlayerCamera.Instance = oldCamera; MyTime.fixedDeltaTime = oldDelta; MyTime.time = oldTime; GraveyardBossRoom.isPlayerInsideLight = oldInside;
		}
	}
	private static GameObject Child(GameObject root, string name) { GameObject child = new GameObject(name); child.transform.SetParent(root.transform); return child; }
	private static void Invoke(object value, string name) => value.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null);
	private static object Get(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
	private static void Check(bool value, string name) { if (!value) throw new Exception("GhostKingDarknessPortCheck failed: " + name); }
}
