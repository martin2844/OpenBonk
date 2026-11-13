using Assets.Scripts.Game.Spawning;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts._Data.Tomes;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
public class AndroidInteractionProbe : BaseInteractable
{
	public override bool Interact()
	{
		UnityEngine.Debug.Log("Android interaction probe: received player input");
		Destroy(gameObject);
		return true;
	}
	public override string GetInteractString() { return "Interact"; }
}

// Instantiated only by the opt-in Android diagnostic, never by normal gameplay.
public class AndroidProjectileCollisionProbe : Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileBasic
{
	public void Configure(Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase weapon, Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack attack)
	{
		weaponBase = weapon;
		weaponAttack = attack;
		direction = Vector3.right;
		projectileSpeed = 1f;
		maxBounces = 10;
	}

	public bool TestCollision(Collider collider, Vector3 normal) => CheckCollision(collider, normal);
}
#endif

public class MapGenerationController : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CGenerateMap_003Ed__39 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public MapGenerationController _003C_003E4__this;

		private Mesh _003CworldMesh_003E5__2;

		private StageData _003CstageData_003E5__3;

		private MapData _003CmapData_003E5__4;

		private Vector3 _003CworldSize_003E5__5;

		private Vector3 _003CworldCenter_003E5__6;

		private Vector3 _003CspawnPosition_003E5__7;

		private Vector3 _003CspawnDirection_003E5__8;

		private Vector3 _003CworldAreaNew_003E5__9;

		private Vector3 _003CworldCenterNew_003E5__10;

		private float _003CworldAreaMagnitude_003E5__11;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CGenerateMap_003Ed__39(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public MapData testMapData;

	public StageData testStageData;

	public ProceduralTileGeneration proceduralTileGeneration;

	public RandomObjectPlacer randomObjectPlacer;

	public GenerateTileObjects RandomTileObjects;

	public MinimapMesh minimapMesh;

	public GameObject colliderBox;

	public GameObject spawnPortal;

	public GameObject bossPortal;

	public GameObject spawnPlatform;

	public GameObject bossPortalFinal;

	public GameObject graveyardBossPortal;

	public GameManager gameManager;

	public SpawnInteractables interactablesSpawner;

	public GrassChunkManager grassRenderer;

	public MapEdges mapEdges;

	public MeshRenderer proceduralMeshRenderer;

	public Water water;

	public GameObject proceduralMapWorldEdges;

	public MapGenerator proceduralMapMeshGenerator;

	public static Action A_GenerationComplete;

	public static Action A_PreGeneration;

	public Light sunLight;

	public static bool isGenerating;

	[Space(10f)]
	[Header("Graveyard / Crypt")]
	public GameObject cryptParent;

	public GameObject cryptExitOutside;

	public RsgController rsgController;

	public int testSeed;

	public GameObject androidTreePrefab;

	public GameObject androidEnemyPrefab;

	public GameObject androidGroundTilePrefab;

	public static int mapSeed;

	public static Vector3 tileSpawnPosition { get; private set; }

	public static Vector3 tileSpawnDir { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
	private bool androidEnemyProbe;
	private bool androidAutoEnemyProbe;
	private bool androidWeaponDamageProbe;
	private bool androidBananarangProbe;
	private bool androidBowProbe;
	private bool androidAxeProbe;
	private bool androidMineProbe;
	private bool androidDiceProbe;
	private bool androidFlamewalkerProbe;
	private bool androidBloodMagicProbe;
	private bool androidWirelessDaggerProbe;
	private bool androidPoisonFlaskProbe;
	private bool androidTornadoProbe;
	private bool androidDexecutionerProbe;
	private bool androidHeroSwordProbe;
	private bool androidCorruptSwordProbe;
	private bool androidScytheProbe;
	private int androidAuraProbe;
	private bool androidKatanaProbe;
	private bool androidLightningWeaponProbe;
	private bool androidShotgunProbe;
	private bool androidXpProbe;
	private bool androidShrineProbe;
	private bool androidGreedShrineProbe;
	private bool androidCactusHazardProbe;
	private bool androidSniperControllerProbe;
	private int androidXpProbeValue = 3;
	private bool androidUpgradePoolProbe;
	private bool androidDeathProbe;
	private bool androidGoldProbe;
	private bool androidCoinProbe;
	private bool androidInteractionProbe;
	private bool androidPotProbe;
	private bool androidPotAchievementProbe;
	private bool androidWeaponAchievementProbe;
	private bool androidItemAchievementProbe;
	private bool androidTomeAchievementProbe;
	private bool androidSilverPotProbe;
	private bool androidItemProbe;
	private bool androidChestSelectionProbe;
	private bool androidChestAwardProbe;
	private bool androidChestPurchaseProbe;
	private bool androidChestWindowProbe;
	private bool androidChestControllerProbe;
	private bool androidChestBanishProbe;
	private bool androidWorldChestProbe;
	private bool androidMicrowaveProbe;
	private bool androidMicrowaveControllerProbe;
	private bool androidEnemyGoldProbe;
	private bool androidMoldyProbe;
	private bool androidBorgorProbe;
	private bool androidBurnProbe;
	private bool androidFirefieldProbe;
	private bool androidIceProbe;
	private bool androidFlexProbe;
	private bool androidLightningProbe;
	private bool androidBoomerProbe;
	private bool androidKnockbackProbe;
	private bool androidSpecialAttackProbe;
	private bool androidHitscanProbe;
	private bool androidCactusProbe;
	private bool androidStatusProbe;
	private bool androidBossProbe;
	private bool androidTimelineProbe;
	private bool androidVictoryProbe;
	private bool androidProgressProbe;
	private static PlayerInventory androidBossInventory;
	private int androidLifetimeStatsProbe;
	private bool androidItemStatsProbe;
	private bool androidMoreItemStatsProbe;
	private bool androidLifestealProbe;
	private bool androidRegenProbe;
	private bool androidEvadeProbe;
	private bool androidPhantomShroudProbe;
	private bool androidCursedDollProbe;
	private bool androidBobsLanternProbe;
	private bool androidIdleJuiceProbe;
	private bool androidBeefyRingProbe;
	private bool androidSpicyMeatballProbe;
	private bool androidGrandmasTonicProbe;
	private bool androidQuinsMaskProbe;
	private bool androidStationaryProbe;
	private bool androidBonkerProbe;
	private bool androidIceCrystalProbe;
	private bool androidInputTipProbe;
	private bool androidGoldenSneakersProbe;
	private bool androidSoulHarvesterProbe;
	private bool androidBobDeadProbe;
	private bool androidJoesDaggerProbe;
	private bool androidFinalFightProbe;
	private bool androidPylonPhysicsProbe;
	private bool androidGameplayInputProbe;
	private bool androidChallengeMenuProbe;
	private bool androidCryptLimitProbe;
	private bool androidGraveyardProbe;
	private bool androidGraveyardExitProbe;
	private bool androidGraveyardBossProbe;
	private bool androidGraveyardDefeatProbe;
	private bool androidGraveyardCoffinsProbe;
	private bool androidGraveyardCoffinBossProbe;
	private bool androidGraveyardGhostCombatProbe;
	private bool androidGraveyardBossAttacksProbe;
	private bool androidGraveyardDarknessProbe;
	private bool androidGraveyardBossStrikeProbe;
	private bool androidGrassProbe;
	private bool androidGrassHighlightProbe;
	private bool androidGrassForcedProbe;
	private int androidPotProbeSeed;
	public static bool androidHudProbe;
#endif

	private void Awake()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
		using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
		{
			bool originalForestProbe = intent.Call<bool>("getBooleanExtra", "megabonk_original_forest", false);
			MapData selectedMap = MapController.currentMap;
			if (CanUseRecoveredMap(selectedMap) && MapController.currentStage != null)
			{
				androidEnemyProbe = intent.Call<bool>("getBooleanExtra", "megabonk_enemy_probe", false);
				androidAutoEnemyProbe = intent.Call<bool>("getBooleanExtra", "megabonk_auto_enemy_probe", false);
				androidWeaponDamageProbe = intent.Call<bool>("getBooleanExtra", "megabonk_weapon_damage_probe", false);
				androidBananarangProbe = intent.Call<bool>("getBooleanExtra", "megabonk_bananarang_probe", false);
				androidBowProbe = intent.Call<bool>("getBooleanExtra", "megabonk_bow_probe", false);
				androidAxeProbe = intent.Call<bool>("getBooleanExtra", "megabonk_axe_probe", false);
				androidMineProbe = intent.Call<bool>("getBooleanExtra", "megabonk_mine_probe", false);
				androidDiceProbe = intent.Call<bool>("getBooleanExtra", "megabonk_dice_probe", false);
				androidFlamewalkerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_flamewalker_probe", false);
				androidBloodMagicProbe = intent.Call<bool>("getBooleanExtra", "megabonk_bloodmagic_probe", false);
				androidWirelessDaggerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_wireless_dagger_probe", false);
				androidPoisonFlaskProbe = intent.Call<bool>("getBooleanExtra", "megabonk_poison_flask_probe", false);
				androidTornadoProbe = intent.Call<bool>("getBooleanExtra", "megabonk_tornado_probe", false);
				androidDexecutionerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_dexecutioner_probe", false);
				androidHeroSwordProbe = intent.Call<bool>("getBooleanExtra", "megabonk_hero_sword_probe", false);
				androidCorruptSwordProbe = intent.Call<bool>("getBooleanExtra", "megabonk_corrupt_sword_probe", false);
				androidScytheProbe = intent.Call<bool>("getBooleanExtra", "megabonk_scythe_probe", false);
				androidAuraProbe = intent.Call<int>("getIntExtra", "megabonk_aura_probe", 0);
				androidKatanaProbe = intent.Call<bool>("getBooleanExtra", "megabonk_katana_probe", false);
				androidLightningWeaponProbe = intent.Call<bool>("getBooleanExtra", "megabonk_lightning_weapon_probe", false);
				androidShotgunProbe = intent.Call<bool>("getBooleanExtra", "megabonk_shotgun_probe", false);
				androidXpProbe = intent.Call<bool>("getBooleanExtra", "megabonk_xp_probe", false);
				androidShrineProbe = intent.Call<bool>("getBooleanExtra", "megabonk_shrine_probe", false);
				androidGreedShrineProbe = intent.Call<bool>("getBooleanExtra", "megabonk_greed_shrine_probe", false);
				androidCactusHazardProbe = intent.Call<bool>("getBooleanExtra", "megabonk_cactus_hazard_probe", false);
				androidSniperControllerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_sniper_controller_probe", false);
				androidXpProbeValue = intent.Call<int>("getIntExtra", "megabonk_xp_probe_value", 3);
				androidUpgradePoolProbe = intent.Call<bool>("getBooleanExtra", "megabonk_upgrade_pool_probe", false);
				androidDeathProbe = intent.Call<bool>("getBooleanExtra", "megabonk_death_probe", false);
				androidGoldProbe = intent.Call<bool>("getBooleanExtra", "megabonk_gold_probe", false);
				androidCoinProbe = intent.Call<bool>("getBooleanExtra", "megabonk_coin_probe", false);
				androidInteractionProbe = intent.Call<bool>("getBooleanExtra", "megabonk_interaction_probe", false);
				androidPotProbe = intent.Call<bool>("getBooleanExtra", "megabonk_pot_probe", false);
				androidPotAchievementProbe = intent.Call<bool>("getBooleanExtra", "megabonk_pot_achievement_probe", false);
				androidWeaponAchievementProbe = intent.Call<bool>("getBooleanExtra", "megabonk_weapon_achievement_probe", false);
				androidItemAchievementProbe = intent.Call<bool>("getBooleanExtra", "megabonk_item_achievement_probe", false);
				androidTomeAchievementProbe = intent.Call<bool>("getBooleanExtra", "megabonk_tome_achievement_probe", false);
				androidSilverPotProbe = intent.Call<bool>("getBooleanExtra", "megabonk_silver_pot_probe", false);
				androidItemProbe = intent.Call<bool>("getBooleanExtra", "megabonk_item_probe", false);
				androidChestSelectionProbe = intent.Call<bool>("getBooleanExtra", "megabonk_chest_selection_probe", false);
				androidChestAwardProbe = intent.Call<bool>("getBooleanExtra", "megabonk_chest_award_probe", false);
				androidChestPurchaseProbe = intent.Call<bool>("getBooleanExtra", "megabonk_chest_purchase_probe", false);
				androidChestWindowProbe = intent.Call<bool>("getBooleanExtra", "megabonk_chest_window_probe", false);
				androidChestControllerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_chest_controller_probe", false);
				androidChestWindowProbe |= androidChestControllerProbe;
				androidChestBanishProbe = intent.Call<bool>("getBooleanExtra", "megabonk_chest_banish_probe", false);
				androidWorldChestProbe = intent.Call<bool>("getBooleanExtra", "megabonk_world_chest_probe", false);
				androidMicrowaveProbe = intent.Call<bool>("getBooleanExtra", "megabonk_microwave_probe", false);
				androidMicrowaveControllerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_microwave_controller_probe", false);
				androidMicrowaveProbe |= androidMicrowaveControllerProbe;
				androidEnemyGoldProbe = intent.Call<bool>("getBooleanExtra", "megabonk_enemy_gold_probe", false);
				androidMoldyProbe = intent.Call<bool>("getBooleanExtra", "megabonk_moldy_probe", false);
				androidBorgorProbe = intent.Call<bool>("getBooleanExtra", "megabonk_borgor_probe", false);
				androidBurnProbe = intent.Call<bool>("getBooleanExtra", "megabonk_burn_probe", false);
				androidFirefieldProbe = intent.Call<bool>("getBooleanExtra", "megabonk_firefield_probe", false);
				androidIceProbe = intent.Call<bool>("getBooleanExtra", "megabonk_ice_probe", false);
				androidFlexProbe = intent.Call<bool>("getBooleanExtra", "megabonk_flex_probe", false);
				androidLightningProbe = intent.Call<bool>("getBooleanExtra", "megabonk_lightning_probe", false);
				androidBoomerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_boomer_probe", false);
				androidKnockbackProbe = intent.Call<bool>("getBooleanExtra", "megabonk_knockback_probe", false);
				androidSpecialAttackProbe = intent.Call<bool>("getBooleanExtra", "megabonk_special_attack_probe", false);
				androidHitscanProbe = intent.Call<bool>("getBooleanExtra", "megabonk_hitscan_probe", false);
				androidCactusProbe = intent.Call<bool>("getBooleanExtra", "megabonk_cactus_probe", false);
				androidStatusProbe = intent.Call<bool>("getBooleanExtra", "megabonk_status_probe", false);
				androidBossProbe = intent.Call<bool>("getBooleanExtra", "megabonk_boss_probe", false);
				androidTimelineProbe = intent.Call<bool>("getBooleanExtra", "megabonk_timeline_probe", false);
				androidVictoryProbe = intent.Call<bool>("getBooleanExtra", "megabonk_victory_probe", false);
				androidProgressProbe = intent.Call<bool>("getBooleanExtra", "megabonk_progress_probe", false);
				androidLifetimeStatsProbe = intent.Call<int>("getIntExtra", "megabonk_lifetime_stats_probe", 0);
				androidItemStatsProbe = intent.Call<bool>("getBooleanExtra", "megabonk_item_stats_probe", false);
				androidMoreItemStatsProbe = intent.Call<bool>("getBooleanExtra", "megabonk_more_item_stats_probe", false);
				androidLifestealProbe = intent.Call<bool>("getBooleanExtra", "megabonk_lifesteal_probe", false);
				androidRegenProbe = intent.Call<bool>("getBooleanExtra", "megabonk_regen_probe", false);
				androidEvadeProbe = intent.Call<bool>("getBooleanExtra", "megabonk_evade_probe", false);
				androidPhantomShroudProbe = intent.Call<bool>("getBooleanExtra", "megabonk_phantom_shroud_probe", false);
				androidCursedDollProbe = intent.Call<bool>("getBooleanExtra", "megabonk_cursed_doll_probe", false);
				androidBobsLanternProbe = intent.Call<bool>("getBooleanExtra", "megabonk_bobs_lantern_probe", false);
				androidIdleJuiceProbe = intent.Call<bool>("getBooleanExtra", "megabonk_idle_juice_probe", false);
				androidBeefyRingProbe = intent.Call<bool>("getBooleanExtra", "megabonk_beefy_ring_probe", false);
				androidSpicyMeatballProbe = intent.Call<bool>("getBooleanExtra", "megabonk_spicy_meatball_probe", false);
				androidGrandmasTonicProbe = intent.Call<bool>("getBooleanExtra", "megabonk_grandmas_tonic_probe", false);
				androidQuinsMaskProbe = intent.Call<bool>("getBooleanExtra", "megabonk_quins_mask_probe", false);
				androidStationaryProbe = intent.Call<bool>("getBooleanExtra", "megabonk_stationary_probe", false);
				androidBonkerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_bonker_probe", false);
				androidIceCrystalProbe = intent.Call<bool>("getBooleanExtra", "megabonk_ice_crystal_probe", false);
				androidInputTipProbe = intent.Call<bool>("getBooleanExtra", "megabonk_input_tip_probe", false);
				androidGoldenSneakersProbe = intent.Call<bool>("getBooleanExtra", "megabonk_golden_sneakers_probe", false);
				androidSoulHarvesterProbe = intent.Call<bool>("getBooleanExtra", "megabonk_soul_harvester_probe", false);
				androidBobDeadProbe = intent.Call<bool>("getBooleanExtra", "megabonk_bob_dead_probe", false);
				androidJoesDaggerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_joes_dagger_probe", false);
				androidFinalFightProbe = intent.Call<bool>("getBooleanExtra", "megabonk_final_fight_probe", false);
				androidPylonPhysicsProbe = intent.Call<bool>("getBooleanExtra", "megabonk_pylon_physics_probe", false);
				androidGameplayInputProbe = intent.Call<bool>("getBooleanExtra", "megabonk_gameplay_input_probe", false) || intent.Call<bool>("getBooleanExtra", "megabonk_calcium_animation_probe", false);
				androidChallengeMenuProbe = intent.Call<bool>("getBooleanExtra", "megabonk_challenge_menu_probe", false);
				androidCryptLimitProbe = intent.Call<bool>("getBooleanExtra", "megabonk_crypt_limit_probe", false);
				androidGraveyardProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_probe", false);
				androidGraveyardExitProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_exit_probe", false);
				androidGraveyardBossProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_boss_probe", false);
				androidGraveyardDefeatProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_defeat_probe", false);
				androidGraveyardCoffinsProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_coffins_probe", false);
				androidGraveyardCoffinBossProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_coffin_boss_probe", false);
				androidGraveyardGhostCombatProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_ghost_combat_probe", false);
				androidGraveyardBossAttacksProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_boss_attacks_probe", false);
				androidGraveyardDarknessProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_darkness_probe", false);
				androidGraveyardBossStrikeProbe = intent.Call<bool>("getBooleanExtra", "megabonk_graveyard_boss_strike_probe", false);
				androidGrassProbe = intent.Call<bool>("getBooleanExtra", "megabonk_grass_probe", false);
				androidGrassHighlightProbe = intent.Call<bool>("getBooleanExtra", "megabonk_grass_highlight_probe", false);
				androidGrassForcedProbe = intent.Call<bool>("getBooleanExtra", "megabonk_grass_forced_probe", false);
				androidGrassHighlightProbe |= androidGrassForcedProbe;
				androidGrassProbe |= androidGrassHighlightProbe;
				androidPotProbeSeed = intent.Call<int>("getIntExtra", "megabonk_pot_probe_seed", 0);
				androidHudProbe = intent.Call<bool>("getBooleanExtra", "megabonk_hud_probe", false);
				if (androidWeaponDamageProbe)
				{
					Assets.Scripts.Actors.Enemies.Enemy.A_Damage += LogEnemyDamage;
					Assets.Scripts.Actors.Enemies.Enemy.A_EnemyDied += LogEnemyDeath;
					Pickup.A_PickupTriggered += LogPickup;
					Inventory__Items__Pickups.Xp_and_Levels.PlayerXp.A_XpAdded += LogXp;
					LevelupScreen.A_LevelupEnabled += LogLevelWindowOpened;
					LevelupScreen.A_LevelUpClose += LogLevelWindowClosed;
					Assets.Scripts.Inventory__Items__Pickups.TomeInventory.A_TomeUpgrade += LogTomeUpgrade;
					Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponInventory.A_WeaponAdded += LogWeaponUpgrade;
				}
				UnityEngine.Debug.Log($"Android gameplay route: recovered map={selectedMap?.name}");
				StartCoroutine(GenerateMap());
				if (androidCryptLimitProbe) StartCoroutine(ProbeCryptLimit());
				if (androidGraveyardExitProbe) StartCoroutine(ProbeGraveyardExit());
				if (androidGraveyardProbe)
				{
					Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += LogGraveyardDamage;
					Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_Died += LogGraveyardDeath;
					StartCoroutine(ProbeGraveyardStartup());
				}
				if (androidPotAchievementProbe) StartCoroutine(ProbePotAchievement());
				if (androidWeaponAchievementProbe) StartCoroutine(ProbeWeaponAchievement());
				if (androidItemAchievementProbe) StartCoroutine(ProbeItemAchievement());
				if (androidTomeAchievementProbe) StartCoroutine(ProbeTomeAchievement());
				if (androidGrandmasTonicProbe) StartCoroutine(ProbeGrandmasTonic());
				if (androidQuinsMaskProbe) StartCoroutine(ProbeQuinsMask());
				if (androidStationaryProbe) StartCoroutine(ProbeStationary());
				if (androidGreedShrineProbe) StartCoroutine(ProbeGreedShrine());
				if (androidCactusHazardProbe) StartCoroutine(ProbeCactusHazard());
				if (androidSniperControllerProbe) StartCoroutine(ProbeSniperController());
				if (androidFinalFightProbe || androidGameplayInputProbe || androidChallengeMenuProbe || originalForestProbe || androidEnemyProbe || androidAutoEnemyProbe || androidWeaponDamageProbe || androidBananarangProbe || androidBowProbe || androidAxeProbe || androidMineProbe || androidDiceProbe || androidFlamewalkerProbe || androidBloodMagicProbe || androidWirelessDaggerProbe || androidPoisonFlaskProbe || androidTornadoProbe || androidDexecutionerProbe || androidHeroSwordProbe || androidCorruptSwordProbe || androidScytheProbe || androidAuraProbe != 0 || androidKatanaProbe || androidLightningWeaponProbe || androidShotgunProbe || androidXpProbe || androidUpgradePoolProbe || androidDeathProbe || androidGoldProbe || androidCoinProbe || androidInteractionProbe || androidPotProbe || androidSilverPotProbe || androidItemProbe || androidChestSelectionProbe || androidChestAwardProbe || androidChestPurchaseProbe || androidChestWindowProbe || androidChestBanishProbe || androidWorldChestProbe || androidMicrowaveProbe || androidEnemyGoldProbe || androidMoldyProbe || androidBorgorProbe || androidBurnProbe || androidFirefieldProbe || androidIceProbe || androidFlexProbe || androidLightningProbe || androidBoomerProbe || androidKnockbackProbe || androidSpecialAttackProbe || androidHitscanProbe || androidCactusProbe || androidStatusProbe || androidBossProbe || androidTimelineProbe || androidVictoryProbe || androidProgressProbe || androidLifetimeStatsProbe != 0 || androidItemStatsProbe || androidMoreItemStatsProbe || androidLifestealProbe || androidRegenProbe || androidEvadeProbe || androidPhantomShroudProbe || androidCursedDollProbe || androidBobsLanternProbe || androidIdleJuiceProbe || androidBeefyRingProbe || androidSpicyMeatballProbe || androidBonkerProbe || androidIceCrystalProbe || androidInputTipProbe || androidGoldenSneakersProbe || androidSoulHarvesterProbe || androidBobDeadProbe || androidJoesDaggerProbe || androidGrassProbe)
					StartCoroutine(LogOriginalForestState());
				return;
			}
		}
		throw new InvalidOperationException("Cannot start the original map: selected map or stage is missing, or map type is unsupported.");
#endif
	}

	private static bool CanUseRecoveredMap(MapData map)
	{
		return map != null && (map.mapType == EMapType.Tiles || map.mapType == EMapType.ProceduralMesh);
	}

#if UNITY_ANDROID && !UNITY_EDITOR
	private IEnumerator ProbeGraveyardExit()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		while (Time.realtimeSinceStartup < deadline && (gameManager == null || !gameManager.isDungeonTimerStarted || RsgController.Instance == null || RsgController.Instance.rsgEnd == null)) yield return null;
		var crypt = RsgController.Instance;
		if (gameManager == null || !gameManager.isDungeonTimerStarted || crypt == null || crypt.rsgEnd == null) { UnityEngine.Debug.Log("Graveyard exit probe: timer or exit unavailable"); yield break; }
		var player = gameManager.GetPlayer();
		if (player == null || player.inventory == null || EnemyManager.Instance == null) { UnityEngine.Debug.Log("Graveyard exit probe: player or enemy manager unavailable"); yield break; }
		EnemyManager.Instance.enabledWaves = false;
		var exit = crypt.rsgEnd;
		var body = player.playerMovement.GetRb();
		body.position = exit.transform.position + Vector3.up * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
		bool completed = false;
		Action<float> onCompleted = elapsed => { completed = true; UnityEngine.Debug.Log("Graveyard exit probe: completion event time=" + elapsed); };
		InteractableCryptLeave.A_FirstDungeonCompleted += onCompleted;
		bool accepted = exit.Interact();
		UnityEngine.Debug.Log($"Graveyard exit probe: invoked accepted={accepted}, timer={Assets.Scripts.Utility.MyTime.cryptTimer}, exit={exit.transform.position}");
		yield return new WaitForSecondsRealtime(5f);
		UnityEngine.Debug.Log($"Graveyard exit probe: result completed={completed}, crypt={gameManager.isCrypt}, playerPos={player.transform.position}, alive={(player.inventory.playerHealth.hp > 0)}");
		InteractableCryptLeave.A_FirstDungeonCompleted -= onCompleted;
		if ((androidGraveyardBossProbe || androidGraveyardBossAttacksProbe || androidGraveyardDarknessProbe || androidGraveyardBossStrikeProbe) && completed && !gameManager.isCrypt) StartCoroutine(ProbeGraveyardBossEntry());
		if ((androidGraveyardCoffinsProbe || androidGraveyardCoffinBossProbe || androidGraveyardGhostCombatProbe) && completed && !gameManager.isCrypt) StartCoroutine(ProbeGraveyardCoffins());
	}

	private IEnumerator ProbeGraveyardCoffins()
	{
		var player = gameManager.GetPlayer();
		var coffins = FindObjectsOfType<InteractableCoffin>();
		UnityEngine.Debug.Log($"Graveyard coffins probe: generated={coffins.Length}");
		if (player == null || player.playerInput == null || player.playerInput.detectInteractables == null || coffins.Length != 4) yield break;
		typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("isInvincible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, true);
		typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("damageCooldownOverAtTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, float.MaxValue);
		var detector = player.playerInput.detectInteractables;
		var currentField = detector.GetType().GetField("currentInteractable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var ghostsField = typeof(InteractableCoffin).GetField("minibossEnemies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var body = player.playerMovement.GetRb();
		for (int i = 0; i < coffins.Length; i++)
		{
			var coffin = coffins[i];
			// This diagnostic teleports across the map; suppress incidental chest pickup windows.
			foreach (var drop in FindObjectsOfType<Assets.Scripts.Inventory__Items__Pickups.Interactables.OpenChest>()) drop.enabled = false;
			body.position = coffin.transform.position + Vector3.up * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
			bool canStart = coffin.CanInteract();
			bool accepted = coffin.Interact();
			var ghosts = (System.Collections.Generic.HashSet<Assets.Scripts.Actors.Enemies.Enemy>)ghostsField.GetValue(coffin);
			var snapshot = ghosts.ToArray();
			bool blocked = i == coffins.Length - 1 || !coffins[i + 1].CanInteract();
			UnityEngine.Debug.Log($"Graveyard coffins probe: index={i}, ready={canStart}, accepted={accepted}, ghosts={snapshot.Length}, ghost={(snapshot.Length > 0 ? snapshot[0].enemyData.enemyName.ToString() : "none")}, nextBlocked={blocked}");
			if (!accepted || snapshot.Length == 0 || coffin.keyPickup == null) yield break;
			float spawnDeadline = Time.realtimeSinceStartup + 30f;
			while (snapshot.Any(ghost => ghost != null && !ghost.CanTakeDamage()) && Time.realtimeSinceStartup < spawnDeadline) yield return null;
			UnityEngine.Debug.Log($"Graveyard coffins probe: index={i}, canDamage={snapshot[0].CanTakeDamage()}, state={snapshot[0].state}, hp={snapshot[0].hp}");
			if (androidGraveyardGhostCombatProbe)
			{
				yield return StartCoroutine(ProbeCoffinGhostCombat(player, snapshot[0]));
				yield break;
			}
			var effects = EffectManager.Instance;
			var savedChestPrefab = effects != null ? effects.openChestNormal : null;
			if (effects != null) effects.openChestNormal = null;
			try
			{
				foreach (var ghost in snapshot)
					ghost.DamageExternal(new Assets.Scripts.Actors.DamageContainer(0f, "coffin_probe") { damage = ghost.hp + 1f, flags = Assets.Scripts.Actors.DcFlags.BypassAll });
			}
			finally { if (effects != null) effects.openChestNormal = savedChestPrefab; }
			UnityEngine.Debug.Log($"Graveyard coffins probe: index={i}, afterHitHp={snapshot[0].hp}, dead={snapshot[0].IsDead()}");
			float deadline = Time.realtimeSinceStartup + 20f;
			while (!coffin.keyPickup.activeInHierarchy && Time.realtimeSinceStartup < deadline) yield return null;
			if (!coffin.keyPickup.activeInHierarchy) { UnityEngine.Debug.Log($"Graveyard coffins probe: index={i}, key did not appear, remaining={ghosts.Count}"); yield break; }
			var keyCollider = coffin.keyPickup.GetComponent<Collider>();
			Vector3 keyPosition = keyCollider != null ? keyCollider.bounds.center : coffin.keyPickup.transform.position;
			Vector3 detectorOffset = detector.transform.position - body.position;
			var previousConstraints = body.constraints;
			body.velocity = Vector3.zero;
			body.angularVelocity = Vector3.zero;
			body.constraints = RigidbodyConstraints.FreezeAll;
			body.position = keyPosition - detectorOffset;
			Physics.SyncTransforms();
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			var selected = currentField.GetValue(detector) as BaseInteractable;
			bool selectedKey = selected != null && selected.gameObject == coffin.keyPickup;
			UnityEngine.Debug.Log($"Graveyard coffins probe: index={i}, keyPosition={keyPosition}, detectorPosition={detector.transform.position}, selected={(selected != null ? selected.name : "none")}, keyLayer={coffin.keyPickup.layer}, keyTag={coffin.keyPickup.tag}, collider={(keyCollider != null && keyCollider.enabled)}");
			if (selectedKey) detector.TryInteract();
			body.constraints = previousConstraints;
			int keys = player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.CryptKey);
			bool nextReady = i == coffins.Length - 1 || coffins[i + 1].CanInteract();
			UnityEngine.Debug.Log($"Graveyard coffins probe: index={i}, keyVisible=true, selectedKey={selectedKey}, keys={keys}, nextReady={nextReady}, paused={Assets.Scripts.Utility.MyTime.paused}, cutscene={gameManager.cutscene}, teleporting={player.isTeleporting}");
			if (!selectedKey || keys != i + 1 || !nextReady) yield break;
			yield return new WaitForSecondsRealtime(3.2f);
		}
		UnityEngine.Debug.Log("Graveyard coffins probe: all four collected through interaction detector");
		if (androidGraveyardCoffinBossProbe) yield return StartCoroutine(ProbeGraveyardBossEntry());
	}

	private IEnumerator ProbeCoffinGhostCombat(Assets.Scripts.Actors.Player.MyPlayer player, Assets.Scripts.Actors.Enemies.Enemy ghost)
	{
		var body = player.playerMovement.GetRb();
		var constraints = body.constraints;
		var health = player.inventory.playerHealth;
		int previousHp = health.hp;
		float previousShield = health.shield;
		var invincible = typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("isInvincible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var cooldown = typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("damageCooldownOverAtTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		invincible.SetValue(player, false);
		cooldown.SetValue(player, 0f);
		health.hp = 1000; health.shield = 0f;
		body.constraints = RigidbodyConstraints.FreezeAll;
		body.position = ghost.rb.position + Vector3.right * 10f;
		body.velocity = Vector3.zero; Physics.SyncTransforms();
		int hits = 0;
		Action<Assets.Scripts.Inventory__Items__Pickups.PlayerHealth, Assets.Scripts.Actors.DamageContainer, bool> onDamage = (h, dc, shield) => { if (h == health && dc.enemy == ghost) hits++; };
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += onDamage;
		Vector3 start = ghost.transform.position;
		float startingDistance = Vector3.Distance(start, body.position);
		yield return new WaitForSecondsRealtime(12f);
		UnityEngine.Debug.Log($"Graveyard ghost combat probe: ghost={ghost.enemyData.enemyName}, start={start}, end={ghost.transform.position}, startDistance={startingDistance:F2}, endDistance={Vector3.Distance(ghost.transform.position, body.position):F2}, speed={ghost.GetSpeed():F2}, state={ghost.state}, hits={hits}, hpBefore=1000, hpAfter={health.hp}, alive={ghost.gameObject.activeInHierarchy}, paused={Assets.Scripts.Utility.MyTime.paused}");
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= onDamage;
		body.constraints = constraints;
		health.hp = previousHp; health.shield = previousShield;
	}

	private IEnumerator ProbeGraveyardBossEntry()
	{
		var player = gameManager.GetPlayer();
		var entrance = FindObjectOfType<InteractableCrypt>();
		if (player == null || entrance == null) { UnityEngine.Debug.Log("Graveyard boss probe: player or entrance unavailable"); yield break; }
		if (!androidGraveyardCoffinBossProbe)
			player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.CryptKey, 4);
		int keys = player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.CryptKey);
		var body = player.playerMovement.GetRb(); body.position = entrance.transform.position + Vector3.up * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
		bool accepted = entrance.Interact();
		UnityEngine.Debug.Log($"Graveyard boss probe: entrance keys={keys}, accepted={accepted}");
		yield return new WaitForSecondsRealtime(5f);
		var crypt = RsgController.Instance;
		UnityEngine.Debug.Log($"Graveyard boss probe: dungeon crypt={gameManager.isCrypt}, index={gameManager.cryptIndex}, start={(crypt != null && crypt.rsgStart != null)}, exit={(crypt != null && crypt.rsgEnd != null)}, pos={player.transform.position}");
		if (crypt == null || crypt.rsgEnd == null || !gameManager.isCrypt) yield break;
		var exit = crypt.rsgEnd; body.position = exit.transform.position + Vector3.up * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
		if (androidGraveyardDefeatProbe || androidGraveyardBossAttacksProbe || androidGraveyardDarknessProbe || androidGraveyardBossStrikeProbe)
		{
			typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("isInvincible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, true);
			typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("damageCooldownOverAtTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, float.MaxValue);
		}
		bool exitAccepted = exit.Interact();
		UnityEngine.Debug.Log("Graveyard boss probe: boss dungeon exit accepted=" + exitAccepted);
		yield return new WaitForSecondsRealtime(8f);
		var room = crypt.roomBoss;
		var bossField = typeof(GraveyardBossRoom).GetField("bossEnemy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var boss = room != null ? bossField.GetValue(room) as Assets.Scripts.Actors.Enemies.Enemy : null;
		UnityEngine.Debug.Log($"Graveyard boss probe: room={(room != null && room.gameObject.activeInHierarchy)}, boss={(boss != null)}, bossHp={(boss != null ? boss.hp : -1f)}, pos={player.transform.position}, alive={(player.inventory.playerHealth.hp > 0)}");
		if (androidGraveyardBossStrikeProbe && room != null && boss != null)
		{
			yield return StartCoroutine(ProbeGhostKingStrikes(player, boss));
			yield break;
		}
		if (androidGraveyardDarknessProbe && room != null && boss != null)
		{
			yield return StartCoroutine(ProbeGhostKingDarkness(player, boss, room));
			yield break;
		}
		if (androidGraveyardBossAttacksProbe && room != null && boss != null)
		{
			yield return StartCoroutine(ProbeGhostKingAttacks(player, boss));
			yield break;
		}
		if (!androidGraveyardDefeatProbe || room == null || boss == null) yield break;
		bool diedEvent = false;
		bool scytheBefore = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone("a_scythe");
		UnityEngine.Debug.Log($"Graveyard achievement probe: before scythe={scytheBefore}");
		Action onBossDied = () => { diedEvent = true; UnityEngine.Debug.Log("Graveyard defeat probe: boss death event"); };
		GraveyardBossRoom.A_BossDied += onBossDied;
		boss.DamageExternal(new Assets.Scripts.Actors.DamageContainer(0f, "graveyard_defeat_probe") { damage = boss.hp + 1f, flags = Assets.Scripts.Actors.DcFlags.BypassAll });
		UnityEngine.Debug.Log($"Graveyard achievement probe: after scythe={Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone("a_scythe")}");
		UnityEngine.Debug.Log($"Graveyard defeat probe: forced lethal dead={boss.IsDead()}, event={diedEvent}");
		yield return new WaitForSecondsRealtime(6f);
		var door = room.interactableGhostBossLeave;
		UnityEngine.Debug.Log($"Graveyard defeat probe: defeated={room.isBossDefeated}, doorAvailable={(door != null && door.CanInteract())}, event={diedEvent}, playerAlive={(player.inventory.playerHealth.hp > 0)}");
		if (door != null && door.CanInteract())
		{
			body.position = door.transform.position + Vector3.up * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
			bool doorAccepted = door.Interact();
			UnityEngine.Debug.Log("Graveyard defeat probe: exit accepted=" + doorAccepted);
			yield return new WaitForSecondsRealtime(4f);
			UnityEngine.Debug.Log($"Graveyard defeat probe: returned pos={player.transform.position}, roomActive={room.gameObject.activeInHierarchy}, crypt={gameManager.isCrypt}, alive={(player.inventory.playerHealth.hp > 0)}");
		}
		GraveyardBossRoom.A_BossDied -= onBossDied;
	}

	private IEnumerator ProbeGhostKingStrikes(Assets.Scripts.Actors.Player.MyPlayer player, Assets.Scripts.Actors.Enemies.Enemy boss)
	{
		var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
		var controller = typeof(Assets.Scripts.Actors.Enemies.Enemy).GetField("specialAttackController", flags).GetValue(boss);
		if (controller == null) { UnityEngine.Debug.Log("Ghost King strike probe: controller missing"); yield break; }
		var type = controller.GetType();
		var attacking = type.GetField("isAttacking", flags);
		var useAttack = type.GetMethod("UseSpecialAttack", flags);
		var attacks = (System.Collections.Generic.HashSet<Assets.Scripts.Game.Combat.EnemySpecialAttacks.EnemySpecialAttack>)type.GetField("attacks", flags).GetValue(controller);
		var savedAttacks = attacks.ToArray();
		attacks.Clear();
		float deadline = Time.realtimeSinceStartup + 15f;
		while ((bool)attacking.GetValue(controller) && Time.realtimeSinceStartup < deadline) yield return null;
		if ((bool)attacking.GetValue(controller)) { attacks.UnionWith(savedAttacks); UnityEngine.Debug.Log("Ghost King strike probe: preceding attack did not finish"); yield break; }
		var body = player.playerMovement.GetRb();
		var oldConstraints = body.constraints;
		var playerCollider = player.GetComponent<Collider>();
		if (playerCollider != null && boss.collider != null) Physics.IgnoreCollision(playerCollider, boss.collider, true);
		body.constraints = RigidbodyConstraints.FreezeAll; body.velocity = Vector3.zero;
		var health = player.inventory.playerHealth;
		health.hp = 5000; health.shield = 0f; health.overheal = 0f;
		typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("isInvincible", flags).SetValue(player, false);
		typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("damageCooldownOverAtTime", flags).SetValue(player, 0f);
		health.GetType().GetField("damageCooldown", flags).SetValue(health, 0f);
		int hits = 0;
		string phase = "none";
		Action<Assets.Scripts.Inventory__Items__Pickups.PlayerHealth, Assets.Scripts.Actors.DamageContainer, bool> onDamage = (h, dc, shield) =>
		{
			if (h != health) return;
			hits++;
			UnityEngine.Debug.Log($"Ghost King strike probe: phase={phase}, damage={dc.damage:F1}, flags={dc.flags}, hp={health.hp}");
		};
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += onDamage;
		try
		{
			foreach (string name in new[] { "GhostRootAttackContinous", "GhostSpikesMultiple" })
			{
				var attack = savedAttacks.FirstOrDefault(a => a.attackName == name);
				if (attack == null) { UnityEngine.Debug.Log("Ghost King strike probe: missing " + name); yield break; }
				phase = name;
				int beforeHp = health.hp, beforeHits = hits, peakWarnings = 0, peakAttackInstances = 0;
				useAttack.Invoke(controller, new object[] { attack });
				deadline = Time.realtimeSinceStartup + (name == "GhostRootAttackContinous" ? 12f : 7f);
				while (Time.realtimeSinceStartup < deadline && health.hp > 0)
				{
					var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance;
					if (pools != null && pools.warningSpherePool != null) peakWarnings = Mathf.Max(peakWarnings, pools.warningSpherePool.CountActive);
					if (pools != null && pools.enemyAttacksPools != null && pools.enemyAttacksPools.TryGetValue(name, out var pool)) peakAttackInstances = Mathf.Max(peakAttackInstances, pool.CountActive);
					yield return null;
				}
				var finalPools = Assets.Scripts.Objects.Pooling.PoolManager.Instance;
				int remainingWarnings = finalPools != null && finalPools.warningSpherePool != null ? finalPools.warningSpherePool.CountActive : -1;
				int remainingAttacks = finalPools != null && finalPools.enemyAttacksPools != null && finalPools.enemyAttacksPools.TryGetValue(name, out var finalPool) ? finalPool.CountActive : -1;
				UnityEngine.Debug.Log($"Ghost King strike probe: result={name}, hits={hits - beforeHits}, hpBefore={beforeHp}, hpAfter={health.hp}, peakWarnings={peakWarnings}, remainingWarnings={remainingWarnings}, peakAttackInstances={peakAttackInstances}, remainingAttacks={remainingAttacks}, playerPos={player.transform.position}");
				if (health.hp <= 0) yield break;
			}
		}
		finally
		{
			Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= onDamage;
			attacks.UnionWith(savedAttacks);
			if (playerCollider != null && boss.collider != null) Physics.IgnoreCollision(playerCollider, boss.collider, false);
			body.constraints = oldConstraints;
		}
	}

	private IEnumerator ProbeGhostKingDarkness(Assets.Scripts.Actors.Player.MyPlayer player, Assets.Scripts.Actors.Enemies.Enemy boss, GraveyardBossRoom room)
	{
		var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
		var controller = typeof(Assets.Scripts.Actors.Enemies.Enemy).GetField("specialAttackController", flags).GetValue(boss);
		var darkness = boss.enemyData.specialAttacks.FirstOrDefault(a => a.attackName == "DarknessAttack");
		if (controller == null || darkness == null || room.lamps == null || room.lamps.Length == 0)
		{ UnityEngine.Debug.Log("Ghost King darkness probe: missing controller, attack or lamps"); yield break; }
		var type = controller.GetType();
		var attacking = type.GetField("isAttacking", flags);
		var nextCheck = type.GetField("nextCheckTime", flags);
		var useAttack = type.GetMethod("UseSpecialAttack", flags);
		float deadline = Time.realtimeSinceStartup + 20f;
		nextCheck.SetValue(controller, float.MaxValue);
		while ((bool)attacking.GetValue(controller) && Time.realtimeSinceStartup < deadline) yield return null;
		if ((bool)attacking.GetValue(controller)) { UnityEngine.Debug.Log("Ghost King darkness probe: preceding attack did not finish"); yield break; }
		nextCheck.SetValue(controller, float.MaxValue);
		var body = player.playerMovement.GetRb();
		var oldConstraints = body.constraints;
		var playerCollider = player.GetComponent<Collider>();
		if (playerCollider != null && boss.collider != null) Physics.IgnoreCollision(playerCollider, boss.collider, true);
		body.constraints = RigidbodyConstraints.FreezeAll;
		body.velocity = Vector3.zero;
		Vector3 exposedPosition = body.position;
		var health = player.inventory.playerHealth;
		var invincible = typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("isInvincible", flags);
		var playerCooldown = typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("damageCooldownOverAtTime", flags);
		var healthCooldown = health.GetType().GetField("damageCooldown", flags);
		int blasts = 0, hits = 0;
		bool shelteredAtBlast = false;
		Action onBlast = () => { blasts++; shelteredAtBlast = GraveyardBossRoom.isPlayerInsideLight; };
		Action<Assets.Scripts.Inventory__Items__Pickups.PlayerHealth, Assets.Scripts.Actors.DamageContainer, bool> onDamage = (h, dc, shield) => { if (h == health) hits++; };
		GhostKingAttackDarkness.A_Explode += onBlast;
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += onDamage;
		try
		{
			foreach (var lamp in room.lamps) lamp.Deactivate();
			for (int phase = 0; phase < 3; phase++)
			{
				if (phase > 0)
				{
					deadline = Time.realtimeSinceStartup + 15f;
					while ((bool)attacking.GetValue(controller) && Time.realtimeSinceStartup < deadline) yield return null;
					nextCheck.SetValue(controller, float.MaxValue);
				}
				if (phase == 1)
				{
					var lamp = room.lamps[0];
					var trigger = lamp.GetComponent<Collider>();
					if (trigger == null) { UnityEngine.Debug.Log("Ghost King darkness probe: lamp trigger missing"); yield break; }
					body.position = trigger.bounds.center;
					body.velocity = Vector3.zero; Physics.SyncTransforms();
					yield return new WaitForSecondsRealtime(4.5f);
					UnityEngine.Debug.Log($"Ghost King darkness probe: lampActive={lamp.HasPlayer()}, shelter={GraveyardBossRoom.isPlayerInsideLight}, player={player.transform.position}, lamp={lamp.transform.position}");
					if (!lamp.HasPlayer() || !GraveyardBossRoom.isPlayerInsideLight) yield break;
				}
				else if (phase == 2)
				{
					body.position = exposedPosition; body.velocity = Vector3.zero; Physics.SyncTransforms();
					foreach (var lamp in room.lamps) lamp.Deactivate();
					yield return new WaitForFixedUpdate();
					yield return new WaitForFixedUpdate();
					float testDamage = Mathf.Max(0f, boss.hp - boss.maxHp * 0.7f);
					boss.DamageExternal(new Assets.Scripts.Actors.DamageContainer(0f, "darkness_probe") { damage = testDamage, flags = Assets.Scripts.Actors.DcFlags.BypassAll });
					UnityEngine.Debug.Log($"Ghost King darkness probe: lowHealthBossRatio={boss.GetHpRatio():F3}, shelteredBeforeBlast={GraveyardBossRoom.isPlayerInsideLight}");
					if (boss.GetHpRatio() >= 0.75f || GraveyardBossRoom.isPlayerInsideLight) yield break;
				}
				health.hp = health.maxHp; health.shield = 0f; health.overheal = 0f;
				invincible.SetValue(player, false); playerCooldown.SetValue(player, 0f); healthCooldown.SetValue(health, 0f);
				int beforeHp = health.hp, beforeHits = hits, beforeBlasts = blasts;
				useAttack.Invoke(controller, new object[] { darkness });
				deadline = Time.realtimeSinceStartup + 20f;
				while (blasts == beforeBlasts && Time.realtimeSinceStartup < deadline) yield return null;
				yield return new WaitForFixedUpdate();
				string label = phase == 0 ? "exposed" : phase == 1 ? "sheltered" : "exposed_low_boss_health";
				UnityEngine.Debug.Log($"Ghost King darkness probe: phase={label}, blasts={blasts - beforeBlasts}, shelteredAtBlast={shelteredAtBlast}, hits={hits - beforeHits}, hpBefore={beforeHp}, hpAfter={health.hp}, alive={health.hp > 0}");
				if (blasts == beforeBlasts || health.hp <= 0) yield break;
			}
		}
		finally
		{
			GhostKingAttackDarkness.A_Explode -= onBlast;
			Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= onDamage;
			if (playerCollider != null && boss.collider != null) Physics.IgnoreCollision(playerCollider, boss.collider, false);
			body.constraints = oldConstraints;
		}
	}

	private IEnumerator ProbeGhostKingAttacks(Assets.Scripts.Actors.Player.MyPlayer player, Assets.Scripts.Actors.Enemies.Enemy boss)
	{
		var controllerField = typeof(Assets.Scripts.Actors.Enemies.Enemy).GetField("specialAttackController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var controller = controllerField.GetValue(boss);
		if (controller == null) { UnityEngine.Debug.Log("Ghost King attack probe: controller missing"); yield break; }
		var currentField = controller.GetType().GetField("currentAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var attackingField = controller.GetType().GetField("isAttacking", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var body = player.playerMovement.GetRb();
		var oldConstraints = body.constraints;
		body.constraints = RigidbodyConstraints.FreezeAll;
		var observed = new System.Collections.Generic.HashSet<string>();
		object lastAttack = null;
		bool wasAttacking = false;
		float deadline = Time.realtimeSinceStartup + 50f;
		while (Time.realtimeSinceStartup < deadline && boss != null && boss.gameObject.activeInHierarchy)
		{
			bool attacking = (bool)attackingField.GetValue(controller);
			var attack = currentField.GetValue(controller) as Assets.Scripts.Game.Combat.EnemySpecialAttacks.EnemySpecialAttack;
			if (attacking && (!wasAttacking || attack != lastAttack) && attack != null)
			{
				observed.Add(attack.attackName);
				UnityEngine.Debug.Log($"Ghost King attack probe: started={attack.attackName}, gameTime={Assets.Scripts.Utility.MyTime.time:F1}, distance={boss.enemyMovement.distanceToTarget:F1}, bossState={boss.state}");
			}
			lastAttack = attack;
			wasAttacking = attacking;
			yield return null;
		}
		UnityEngine.Debug.Log($"Ghost King attack probe: observed={string.Join(",", observed.OrderBy(x => x))}, bossAlive={(boss != null && boss.gameObject.activeInHierarchy)}, playerAlive={(player.inventory.playerHealth.hp > 0)}, paused={Assets.Scripts.Utility.MyTime.paused}");
		body.constraints = oldConstraints;
	}

	private IEnumerator ProbeGraveyardStartup()
	{
		float deadline = Time.realtimeSinceStartup + 90f;
		while (Time.realtimeSinceStartup < deadline && (gameManager == null || gameManager.GetPlayer() == null || !gameManager.GetPlayer().hasStarted)) yield return null;
		var player = gameManager != null ? gameManager.GetPlayer() : null;
		var crypt = RsgController.Instance;
		UnityEngine.Debug.Log($"Graveyard probe: startup player={(player != null && player.hasStarted)}, pos={(player != null ? player.transform.position : Vector3.zero)}, generating={isGenerating}, crypt={(gameManager != null && gameManager.isCrypt)}, cryptIndex={(gameManager != null ? gameManager.cryptIndex : -2)}, timerStarted={(gameManager != null && gameManager.isDungeonTimerStarted)}, rooms={FindObjectsOfType<RsgPiece>().Length}, start={(crypt != null && crypt.rsgStart != null)}, exit={(crypt != null && crypt.rsgEnd != null)}");
		foreach (var trigger in FindObjectsOfType<CryptDetectStart>())
		{
			var collider = trigger.GetComponent<Collider>();
			UnityEngine.Debug.Log($"Graveyard probe: timer trigger={trigger.name}, enabled={trigger.isActiveAndEnabled}, collider={(collider != null)}, bounds={(collider != null ? collider.bounds.ToString() : "<none>")}");
		}
		yield return new WaitForSecondsRealtime(12f);
		UnityEngine.Debug.Log($"Graveyard probe: settled player={(player != null && player.hasStarted)}, pos={(player != null ? player.transform.position : Vector3.zero)}, generating={isGenerating}, crypt={(gameManager != null && gameManager.isCrypt)}, timerStarted={(gameManager != null && gameManager.isDungeonTimerStarted)}, cryptTimer={Assets.Scripts.Utility.MyTime.cryptTimer}");
		deadline = Time.realtimeSinceStartup + 90f;
		while (player != null && player.inventory != null && player.inventory.playerHealth != null && player.inventory.playerHealth.hp > 0 && Time.realtimeSinceStartup < deadline)
		{
			var movement = player.playerMovement;
			var body = movement != null ? movement.GetRb() : null;
			UnityEngine.Debug.Log($"Graveyard probe: movement pos={player.transform.position}, vel={(body != null ? body.velocity : Vector3.zero)}, hp={player.inventory.playerHealth.hp}, timerStarted={gameManager.isDungeonTimerStarted}, timer={Assets.Scripts.Utility.MyTime.cryptTimer}, inputY={MyInputManager.GetAxis(MyInputManager.MoveVertical)}");
			yield return new WaitForSecondsRealtime(1f);
		}
	}

	private void LogGraveyardDamage(Assets.Scripts.Inventory__Items__Pickups.PlayerHealth health, Assets.Scripts.Actors.DamageContainer damage, bool shield)
	{
		UnityEngine.Debug.Log($"Graveyard probe: damage source={damage?.damageSource}, amount={damage?.damage}, hp={health.hp}, shield={shield}");
	}

	private void LogGraveyardDeath()
	{
		var player = gameManager != null ? gameManager.GetPlayer() : null;
		UnityEngine.Debug.Log($"Graveyard probe: death pos={(player != null ? player.transform.position : Vector3.zero)}, timer={Assets.Scripts.Utility.MyTime.cryptTimer}, timerStarted={(gameManager != null && gameManager.isDungeonTimerStarted)}");
	}

	private IEnumerator ProbeCryptLimit()
	{
		yield return new WaitForSecondsRealtime(12f);
		var challenge = MapController.runConfig?.challenge;
		var player = gameManager != null ? gameManager.GetPlayer() : null;
		if (challenge == null || player == null || player.inventory == null || player.inventory.playerHealth == null || EnemyManager.Instance == null)
		{
			UnityEngine.Debug.Log("Crypt limit probe: missing challenge, player, or manager");
			yield break;
		}
		EnemyManager.Instance.enabledWaves = false;
		gameManager.StartDungeon(challenge.targetValue);
		Assets.Scripts.Utility.MyTime.cryptTimer = challenge.targetValue - 1.5f;
		gameManager.StartDungeonTimer();
		var health = player.inventory.playerHealth;
		UnityEngine.Debug.Log($"Crypt limit probe: started challenge={challenge.internalName}, modifier={Assets.Scripts.Saves___Serialization.Progression.Challenges.ChallengesTracker.HasChallengeModifier("crypt")}, index={gameManager.cryptIndex}, limit={challenge.targetValue}, timer={Assets.Scripts.Utility.MyTime.cryptTimer}, hp={health.hp}");
		float deadline = Time.realtimeSinceStartup + 15f;
		while (health.hp > 0 && Time.realtimeSinceStartup < deadline) yield return null;
		UnityEngine.Debug.Log($"Crypt limit probe: result timer={Assets.Scripts.Utility.MyTime.cryptTimer}, hp={health.hp}, dead={health.hp <= 0}");
	}

	private IEnumerator ProbeWeapon(Assets.Scripts.Actors.Player.MyPlayer player, EWeapon kind)
	{
		var inventory = player.inventory.weaponInventory;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in inventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var manager = EnemyManager.Instance;
		bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
		var data = DataManager.Instance.GetWeapon(kind);
		float targetDistance = kind == EWeapon.Flamewalker ? -2f : kind == EWeapon.Mine ? 1.5f : 3f;
		var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin), player.transform.position + player.playerRenderer.transform.forward * targetDistance, -1, true, canBeElite: false);
		if (target != null) target.enemyMovement.StopMovement();
		var bounceStat = Assets.Scripts.Menu.Shop.EStat.ProjectileBounces;
		var stats = player.inventory.playerStats.stats;
		bool hadBounceStat = stats.TryGetValue(bounceStat, out float previousBounces);
		Assets.Scripts.Actors.Enemies.Enemy secondTarget = null;
		if (kind == EWeapon.LightningStaff || kind == EWeapon.BluetoothDagger)
		{
			if (kind == EWeapon.LightningStaff) stats[bounceStat] = 1f;
			secondTarget = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin), player.transform.position + player.playerRenderer.transform.forward * 4f, -1, true, canBeElite: false);
			if (secondTarget != null) secondTarget.enemyMovement.StopMovement();
		}
		int spawned = 0, hits = 0, targetHits = 0, secondHits = 0, reducedHits = 0; float damage = 0f;
		System.Action<Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileBase> onSpawn = p => { if ((kind == EWeapon.Bow && p is ProjectileArrow) || (kind == EWeapon.Axe && p is ProjectileAxe) || (kind == EWeapon.Mine && p is ProjectileMines) || (kind == EWeapon.Dice && p is ProjectileDice) || (kind == EWeapon.Flamewalker && p is ProjectileFirefield) || (kind == EWeapon.BloodMagic && p is ProjectileBloodMagic) || (kind == EWeapon.BluetoothDagger && p is ProjectileBluetooth) || (kind == EWeapon.PoisonFlask && p is ProjectilePoisonFlask) || (kind == EWeapon.Tornado && p is ProjectileWhirlwind) || (kind == EWeapon.Dexecutioner && p is Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileDexecutioner) || (kind == EWeapon.HeroSword && p is ProjectileHeroSword) || (kind == EWeapon.CorruptSword && p is ProjectileCringeSword) || (kind == EWeapon.Scythe && p is ProjectileScythe) || (kind == EWeapon.Katana && p is ProjectileKatana) || (kind == EWeapon.LightningStaff && p is ProjectileLightningBolt)) spawned++; };
		System.Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> onDamage = (e, dc) => { if (dc.damageSource == data.damageSourceName) { hits++; if (e == target) targetHits++; if (e == secondTarget) secondHits++; if (dc.damage < data.damage * .75f) reducedHits++; damage += dc.damage; } };
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully += onSpawn;
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += onDamage;
		inventory.AddWeapon(data, null);
		yield return new WaitForSecondsRealtime(6f);
		var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance.projectilePools;
		bool hasPool = pools.TryGetValue(kind, out var pool);
		UnityEngine.Debug.Log($"{kind} probe: target={(target != null)}, secondTarget={(secondTarget != null)}, spawned={spawned}, hits={hits}, targetHits={targetHits}, secondHits={secondHits}, reducedHits={reducedHits}, damage={damage}, poolActive={(hasPool ? pool.CountActive : -1)}, poolInactive={(hasPool ? pool.CountInactive : -1)}");
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully -= onSpawn;
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= onDamage;
		body.constraints = constraints; manager.enabledWaves = waves;
		if (kind == EWeapon.LightningStaff) { if (hadBounceStat) stats[bounceStat] = previousBounces; else stats.Remove(bounceStat); }
		foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeBananarang(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var inventory = player.inventory.weaponInventory;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in inventory.weapons.Values)
			if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		yield return new WaitForSecondsRealtime(1f);
		var manager = EnemyManager.Instance;
		var enemyData = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin);
		var target = manager.SpawnEnemy(enemyData, player.transform.position + Vector3.forward * 3f, -1, true, canBeElite: false);
		if (target == null)
		{
			UnityEngine.Debug.Log("Bananarang probe: target spawn failed");
			foreach (var weapon in disabled) weapon.Enable();
			yield break;
		}
		target.enemyMovement.StopMovement();
		var data = DataManager.Instance.GetWeapon(EWeapon.Bananarang);
		var banana = new Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase(data);
		int spawned = 0, damageEvents = 0;
		float totalDamage = 0f;
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileBase projectile = null;
		System.Action<Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileBase> onSpawn = p => { if (p is ProjectileBanana) { projectile = p; spawned++; } };
		System.Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> onDamage = (e, dc) => { if (dc.damageSource == data.damageSourceName) { damageEvents++; totalDamage += dc.damage; } };
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully += onSpawn;
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += onDamage;
		Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponUtility.WeaponAttack(banana);
		yield return new WaitForSecondsRealtime(4f);
		var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance.projectilePools;
		bool hasPool = pools.TryGetValue(EWeapon.Bananarang, out var pool);
		UnityEngine.Debug.Log($"Bananarang probe: spawned={spawned}, damageEvents={damageEvents}, damage={totalDamage}, returned={(projectile != null && !projectile.gameObject.activeSelf)}, poolActive={(hasPool ? pool.CountActive : -1)}, poolInactive={(hasPool ? pool.CountInactive : -1)}");
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully -= onSpawn;
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= onDamage;
		banana.Cleanup();
		foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeShotgun(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var inventory = player.inventory.weaponInventory;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in inventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
		var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
		SaveManager.Instance.config.cfControlSettings.hold_aim = 0;
		var data = DataManager.Instance.GetWeapon(EWeapon.Shotgun);
		inventory.AddWeapon(data, null);
		var crosshair = FindObjectOfType<CrosshairUi>(true);
		UnityEngine.Debug.Log($"Shotgun probe ready: aimable={inventory.HasAimableWeapon()}, crosshair={crosshair != null}");
		bool lastAim = false;
		for (int i = 0; i < 30; i++)
		{
			bool aim = player.playerInput.IsAiming();
			if (aim != lastAim) { UnityEngine.Debug.Log($"Shotgun probe aim: aiming={aim}, crosshairOuter={(crosshair != null && crosshair.crosshairOuter.gameObject.activeSelf)}"); lastAim = aim; }
			yield return new WaitForSecondsRealtime(.5f);
		}
		var enemy = EnemyManager.Instance.SpawnEnemy(DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin), player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		if (enemy != null) enemy.enemyMovement.StopMovement();
		int hits = 0; float damage = 0;
		System.Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> onDamage = (e, dc) => { if (e == enemy && dc.damageSource == data.damageSourceName) { hits++; damage += dc.damage; } };
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += onDamage;
		yield return new WaitForSecondsRealtime(4f);
		UnityEngine.Debug.Log($"Shotgun probe result: target={enemy != null}, hits={hits}, damage={damage}, aiming={player.playerInput.IsAiming()}, crosshairOuter={(crosshair != null && crosshair.crosshairOuter.gameObject.activeSelf)}");
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= onDamage;
		body.constraints = constraints; EnemyManager.Instance.enabledWaves = waves;
		foreach (var weapon in disabled) weapon.Enable();
	}

	private sealed class MaskProbeRandom : System.Random { public override double NextDouble() => .2d; }

	private IEnumerator ProbeQuinsMask()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && gameManager != null && gameManager.IsPlaying() && player?.inventory?.itemInventory != null && EnemyManager.Instance != null && DataManager.Instance != null) break;
			yield return null;
		}
		if (player?.inventory?.itemInventory == null || EnemyManager.Instance == null || DataManager.Instance == null)
		{ UnityEngine.Debug.Log("Quins mask probe: player or data unavailable"); yield break; }
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var enemyData = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin);
		var target = manager.SpawnEnemy(enemyData, player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		var nearby = manager.SpawnEnemy(enemyData, player.transform.position + Vector3.forward * 6f, -1, true, canBeElite: false);
		if (target == null || nearby == null)
		{ UnityEngine.Debug.Log("Quins mask probe: enemy spawn failed"); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break; }
		target.enemyMovement.StopMovement(); nearby.enemyMovement.StopMovement(); yield return new WaitForSecondsRealtime(1f);
		target.SetDiagnosticHealth(200f); nearby.SetDiagnosticHealth(200f);
		var item = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.QuinsMask;
		player.inventory.itemInventory.AddItem(item); player.inventory.playerStats.ForceUpdateStats();
		float thorns = player.inventory.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.Thorns);
		int retaliations = 0, maskTargetHits = 0, maskNearbyHits = 0;
		Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> observer = (e, dc) => {
			if (e == target && dc.damageSource == "Thorns") retaliations++;
			if (dc.damageSource == "QuinsMask") { if (e == target) maskTargetHits++; if (e == nearby) maskNearbyHits++; }
		};
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += observer;
		var previousRandom = global::Utility.MyRandom.random; global::Utility.MyRandom.random = new MaskProbeRandom();
		Physics.SyncTransforms();
		player.inventory.playerHealth.DamagePlayerExternal(1f, 0f, Vector3.forward, false, "mask-probe", Assets.Scripts.Actors.DcFlags.BypassEvade, enemy: target);
		global::Utility.MyRandom.random = previousRandom;
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.quinMaskPool;
		UnityEngine.Debug.Log($"Quins mask probe: thorns={thorns}, retaliation={retaliations}, targetHits={maskTargetHits}, nearbyHits={maskNearbyHits}, targetHp={target.hp}, nearbyHp={nearby.hp}, effects={(pool != null ? pool.CountActive : -1)}");
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= observer;
		player.inventory.itemInventory.RemoveItem(item); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeSniperController()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && player != null && player.hasStarted && !player.isTeleporting && !gameManager.cutscene && player.playerInput != null && player.playerInput.isActiveAndEnabled && player.inventory?.weaponInventory != null && EnemyManager.Instance != null && Camera.main != null) break;
			yield return null;
		}
		if (player == null || EnemyManager.Instance == null || Camera.main == null) { UnityEngine.Debug.Log("Sniper controller probe: player, enemy manager or camera missing"); yield break; }
		var manager = EnemyManager.Instance; bool oldWaves = manager.enabledWaves; manager.enabledWaves = false;
		var inventory = player.inventory.weaponInventory;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in inventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var body = player.playerMovement.GetRb(); var oldConstraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
		int oldHp = player.inventory.playerHealth.hp; player.inventory.playerHealth.hp = 10000;
		int oldAim = SaveManager.Instance.config.cfControlSettings.hold_aim; SaveManager.Instance.config.cfControlSettings.hold_aim = 1;
		var data = DataManager.Instance.GetWeapon(EWeapon.Sniper); inventory.AddWeapon(data, null);
		var sniper = inventory.weapons[EWeapon.Sniper]; sniper.Disable();
		Ray ray = Camera.main.ScreenPointToRay(CrosshairUi.GetCrosshairRaycastPosition());
		Vector3 desiredCenter = ray.GetPoint(15f);
		var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin), desiredCenter, -1, true, canBeElite: false);
		if (target == null) {
			UnityEngine.Debug.Log("Sniper controller probe: target missing"); sniper.Disable(); foreach (var weapon in disabled) weapon.Enable();
			SaveManager.Instance.config.cfControlSettings.hold_aim = oldAim; body.constraints = oldConstraints; player.inventory.playerHealth.hp = oldHp; manager.enabledWaves = oldWaves; yield break;
		}
		while (target.IsTeleporting()) yield return null;
		target.enemyMovement.StopMovement();
		ray = Camera.main.ScreenPointToRay(CrosshairUi.GetCrosshairRaycastPosition()); desiredCenter = ray.GetPoint(15f);
		Physics.SyncTransforms(); target.transform.position += desiredCenter - target.collider.bounds.center; Physics.SyncTransforms();
		target.hp = target.maxHp = 10000f;
		int shots = 0, hits = 0; float damage = 0f;
		Action<Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileBase> onShot = p => { if (p is ProjectileSniper) shots++; };
		Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> onDamage = (e, dc) => { if (e == target && dc.damageSource == data.damageSourceName) { hits++; damage += dc.damage; } };
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully += onShot;
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += onDamage;
		UnityEngine.Debug.Log($"Sniper controller probe: ready target={target.name}, aimable={inventory.HasAimableWeapon()}, rayHit={Physics.SphereCast(ray, .5f, out var hit, 30f, gameManager.whatIsEnemy) && hit.collider == target.collider}");
		deadline = Time.realtimeSinceStartup + 75f;
		while (!player.playerInput.IsAiming() && Time.realtimeSinceStartup < deadline) yield return null;
		bool aimed = player.playerInput.IsAiming(); if (aimed) sniper.Enable();
		yield return new WaitForSecondsRealtime(6f);
		UnityEngine.Debug.Log($"Sniper controller probe: aimed={aimed}, shots={shots}, targetHits={hits}, damage={damage}, remainingHp={target.hp}");
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully -= onShot;
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= onDamage;
		sniper.Disable(); foreach (var weapon in disabled) weapon.Enable();
		SaveManager.Instance.config.cfControlSettings.hold_aim = oldAim;
		body.constraints = oldConstraints; player.inventory.playerHealth.hp = oldHp; manager.enabledWaves = oldWaves;
	}

	private IEnumerator ProbeCactusHazard()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && player != null && player.hasStarted && !player.isTeleporting && !gameManager.cutscene && player.playerMovement != null && player.playerInput != null && player.playerInput.isActiveAndEnabled && player.inventory?.playerHealth != null && EnemyManager.Instance != null) break;
			yield return null;
		}
		var cactus = FindObjectOfType<DamagePlayerCollider>();
		if (player == null || cactus == null || EnemyManager.Instance == null)
		{ UnityEngine.Debug.Log($"Cactus hazard probe: missing player={player == null}, cactus={cactus == null}"); yield break; }
		var collider = cactus.GetComponent<Collider>();
		var rb = player.playerMovement.GetRb();
		if (collider == null || rb == null) { UnityEngine.Debug.Log("Cactus hazard probe: missing collider or rigidbody"); yield break; }
		bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		int oldHp = player.inventory.playerHealth.hp; player.inventory.playerHealth.hp = 10000;
		float before = player.inventory.playerHealth.hp; int hits = 0; float knockback = 0; Vector3 direction = Vector3.zero;
		Action<Assets.Scripts.Inventory__Items__Pickups.PlayerHealth, Assets.Scripts.Actors.DamageContainer, bool> onDamage = (health, dc, shield) => {
			if (dc.damageSource == "Cactus") { hits++; knockback = dc.knockback; direction = dc.direction; }
		};
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += onDamage;
		player.playerInput.enabled = false;
		rb.velocity = Vector3.zero;
		rb.position = collider.bounds.center + Vector3.right * collider.bounds.extents.x;
		Physics.SyncTransforms();
		yield return new WaitForSecondsRealtime(2f);
		UnityEngine.Debug.Log($"Cactus hazard probe: stage={MapController.currentStage.name}, cactus={cactus.name}, layer={cactus.gameObject.layer}, collider={collider.GetType().Name}, hits={hits}, hpBefore={before}, hpAfter={player.inventory.playerHealth.hp}, knockback={knockback}, direction={direction}, configuredDamage={cactus.damage}");
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= onDamage;
		player.playerInput.enabled = true; player.inventory.playerHealth.hp = oldHp;
		EnemyManager.Instance.enabledWaves = waves;
		foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeGreedShrine()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		Assets.Scripts.Inventory__Items__Pickups.Interactables.DetectInteractables detector = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			detector = player != null && player.playerInput != null ? player.playerInput.detectInteractables : null;
			if (!isGenerating && player != null && player.hasStarted && !player.isTeleporting && !gameManager.cutscene && player.inventory?.statInventory != null && detector != null && detector.isActiveAndEnabled && EnemyManager.Instance != null) break;
			yield return null;
		}
		var placer = FindObjectOfType<RandomObjectPlacer>();
		var shrine = FindObjectOfType<InteractableShrineGreed>();
		bool generated = shrine != null;
		if (shrine == null && placer != null && placer.greedShrineSpawns?.prefabs?.Length > 0)
			shrine = Instantiate(placer.greedShrineSpawns.prefabs[0]).GetComponent<InteractableShrineGreed>();
		if (player == null || detector == null || shrine == null || EnemyManager.Instance == null)
		{ UnityEngine.Debug.Log($"Greed shrine probe: missing player={player == null}, detector={detector == null}, shrine={shrine == null}, placer={placer == null}"); yield break; }
		bool waves = EnemyManager.Instance.enabledWaves;
		EnemyManager.Instance.enabledWaves = false;
		var weapons = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); weapons.Add(weapon); }
		player.inventory.playerHealth.hp = 10000;
		shrine.transform.position = detector.transform.position;
		Physics.SyncTransforms();
		yield return new WaitForFixedUpdate();
		var field = typeof(Assets.Scripts.Inventory__Items__Pickups.Interactables.DetectInteractables).GetField("currentInteractable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		bool detected = (BaseInteractable)field.GetValue(detector) == shrine;
		var stat = shrine.statEffect.statModifier.stat;
		var changes = player.inventory.statInventory.permanentChanges;
		int before = changes.TryGetValue(stat, out var list) ? list.Count : 0;
		float gold = player.inventory.gold;
		int amount = Mathf.Max(25, (int)(Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice() * .1f));
		var shrineLogs = UiManager.Instance != null ? UiManager.Instance.shrineLogs : null;
		var logParent = shrineLogs != null && shrineLogs.prefab != null ? shrineLogs.prefab.transform.parent : null;
		int logCountBefore = logParent != null ? logParent.GetComponentsInChildren<ShrineLogEntry>(true).Length : -1;
		if (detected) detector.TryInteract();
		var logEntries = logParent != null ? logParent.GetComponentsInChildren<ShrineLogEntry>(true) : System.Array.Empty<ShrineLogEntry>();
		ShrineLogEntry latestLog = logEntries.Length > logCountBefore && logCountBefore >= 0 ? logEntries[logEntries.Length - 1] : null;
		UnityEngine.Debug.Log($"Greed shrine log probe: entries={logCountBefore}->{logEntries.Length}, text={(latestLog != null && latestLog.text != null ? latestLog.text.text : "<none>")}, active={(latestLog != null && latestLog.gameObject.activeSelf)}");
		int after = changes.TryGetValue(stat, out list) ? list.Count : 0;
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.goldPool;
		UnityEngine.Debug.Log($"Greed shrine probe interaction: generated={generated}, detected={detected}, used={!shrine.CanInteract()}, stat={stat}, changes={before}->{after}, expectedGold={amount}, coinsActive={pool.CountActive}, effects={shrine.fx.activeSelf}/{shrine.fxLoop.activeSelf}");
		yield return new WaitForSecondsRealtime(12f);
		UnityEngine.Debug.Log($"Greed shrine probe pickup: goldBefore={gold}, goldAfter={player.inventory.gold}, delta={player.inventory.gold - gold}, expectedGold={amount}, coinsActive={pool.CountActive}");
		EnemyManager.Instance.enabledWaves = waves;
		foreach (var weapon in weapons) weapon.Enable();
	}

	private IEnumerator ProbeStationary()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && player != null && player.hasStarted && player.playerMovement != null && EnemyManager.Instance != null) break;
			yield return null;
		}
		if (player == null || player.playerMovement == null || EnemyManager.Instance == null)
		{ UnityEngine.Debug.Log("Stationary probe: missing player or enemy manager"); yield break; }
		AndroidStationaryProbe.Begin(player);
	}

	private IEnumerator ProbeGrandmasTonic()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && player != null && player.hasStarted && player.inventory?.itemInventory != null && EnemyManager.Instance != null && DataManager.Instance != null) break;
			yield return null;
		}
		if (player?.inventory?.itemInventory == null || EnemyManager.Instance == null || DataManager.Instance == null)
		{ UnityEngine.Debug.Log("Grandmas tonic probe: missing player, inventory or data"); yield break; }
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var enemyData = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin);
		var target = manager.SpawnEnemy(enemyData, player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		var nearby = manager.SpawnEnemy(enemyData, player.transform.position + Vector3.forward * 6f, -1, true, canBeElite: false);
		if (target == null || nearby == null)
		{ UnityEngine.Debug.Log("Grandmas tonic probe: enemy spawn failed"); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break; }
		target.enemyMovement.StopMovement(); nearby.enemyMovement.StopMovement();
		yield return new WaitForSecondsRealtime(1f);
		var item = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.GrandmasSecretTonic;
		player.inventory.itemInventory.AddItem(item);
		int targetHits = 0, nearbyHits = 0; float total = 0f;
		Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> observer = (e, dc) => {
			if (dc.damageSource != "GrandmasSecretTonic") return;
			if (e == target) targetHits++; if (e == nearby) nearbyHits++; total += dc.damage;
		};
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += observer; Physics.SyncTransforms();
		var hit = new Assets.Scripts.Actors.DamageContainer(4f, "probe") { enemy = target, damage = 4f, crit = true };
		player.inventory.itemInventory.PostDamage(hit);
		int immediate = targetHits + nearbyHits;
		yield return new WaitForSecondsRealtime(.3f);
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.grandmaTonicPool;
		UnityEngine.Debug.Log($"Grandmas tonic probe: immediate={immediate}, targetHits={targetHits}, nearbyHits={nearbyHits}, total={total}, effectsCreated={(pool != null ? pool.CountAll : -1)}");
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= observer;
		player.inventory.itemInventory.RemoveItem(item); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeSpicyMeatball(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin);
		var target = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		var nearby = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 6f, -1, true, canBeElite: false);
		if (target == null || nearby == null) { UnityEngine.Debug.Log("Spicy meatball probe: target spawn failed"); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break; }
		target.enemyMovement.StopMovement(); nearby.enemyMovement.StopMovement();
		yield return new WaitForSecondsRealtime(1f);
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SpicyMeatball);
		int targetHits = 0, nearbyHits = 0, allHits = 0; float total = 0f;
		Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> observer = (e, dc) => { if (dc.damageSource == "SpicyMeatball") { allHits++; total += dc.damage; if (e == target) targetHits++; if (e == nearby) nearbyHits++; } };
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += observer;
		Physics.SyncTransforms();
		int scan = Assets.Scripts.Inventory__Items__Pickups.Weapons.EnemyTargeting.GetEnemiesInRadiusSafe(player, target.GetCenterPosition(), 4f, out var scanResults);
		UnityEngine.Debug.Log($"Spicy meatball setup: scanned={scan}, targetActive={target.gameObject.activeInHierarchy}, targetCollider={target.collider.enabled}, targetTrigger={target.collider.isTrigger}, targetLayer={target.gameObject.layer}, enemyMask={GameManager.Instance.whatIsEnemy.value}, targetPos={target.transform.position}, nearbyPos={nearby.transform.position}");
		var dc = new Assets.Scripts.Actors.DamageContainer(4f, "probe") { enemy = target, damage = 5f };
		player.inventory.itemInventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SpicyMeatball).ProcOnHitEffects(dc);
		int effects = Assets.Scripts.Objects.Pooling.PoolManager.Instance.spicyMeatballPool.CountActive;
		UnityEngine.Debug.Log($"Spicy meatball probe: targetHits={targetHits}, nearbyHits={nearbyHits}, allHits={allHits}, total={total}, activeEffects={effects}");
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= observer;
		player.inventory.itemInventory.RemoveItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SpicyMeatball);
		manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break;
	}

	private IEnumerator ProbeGoldenSneakers(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
		var inventory = player.inventory; var item = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.GoldenSneakers;
		int beforeGold = inventory.goldInt; Vector3 beforePos = body.position;
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.goldPool; int beforeCreated = pool.CountAll;
		inventory.itemInventory.AddItem(item);
		body.position = beforePos + Vector3.right * 20f; player.transform.position = body.position; Physics.SyncTransforms();
		yield return new WaitForSecondsRealtime(3f);
		UnityEngine.Debug.Log($"Golden sneakers probe: moved={Vector3.Distance(beforePos, body.position):F2}, goldBefore={beforeGold}, goldAfter={inventory.goldInt}, newCoins={pool.CountAll - beforeCreated}, activeCoins={pool.CountActive}");
		inventory.itemInventory.RemoveItem(item); body.position = beforePos; player.transform.position = beforePos; body.constraints = constraints;
		manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable();
	}

	private sealed class JoesProbeRandom : System.Random { public override double NextDouble() => 0d; }

	private IEnumerator ProbeJoesDagger(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var weapons = player.inventory.weaponInventory.weapons.Values.ToArray();
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in weapons) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin), player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		if (target == null || weapons.Length == 0) { UnityEngine.Debug.Log("Joes dagger probe: missing target or weapon"); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break; }
		target.enemyMovement.StopMovement(); yield return new WaitForSecondsRealtime(1f);
		var item = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.JoesDagger;
		var damageStat = Assets.Scripts.Menu.Shop.EStat.DamageMultiplier;
		float before = player.inventory.playerStats.GetStat(damageStat), hp = target.hp;
		player.inventory.itemInventory.AddItem(item);
		var previousRandom = global::Utility.MyRandom.random; global::Utility.MyRandom.random = new JoesProbeRandom();
		var dc = Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponUtility.GetDamageContainer(weapons[0], null, target, Vector3.forward);
		global::Utility.MyRandom.random = previousRandom;
		bool execute = dc.isExecute; string source = dc.damageSource; float damage = dc.damage;
		target.DamageFromPlayerWeapon(dc);
		yield return new WaitForSecondsRealtime(2f);
		player.inventory.playerStats.ForceUpdateStats();
		UnityEngine.Debug.Log($"Joes dagger probe: execute={execute}, source={source}, hpBefore={hp}, dealt={damage}, dead={target.IsDead()}, statBefore={before}, statAfter={player.inventory.playerStats.GetStat(damageStat)}");
		player.inventory.itemInventory.RemoveItem(item); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeBobDead(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
		Vector3 original = body.position; var item = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.BobDead;
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.ghostPool; int before = pool.CountAll;
		player.inventory.itemInventory.AddItem(item); body.position = original + Vector3.right * 14f; player.transform.position = body.position; Physics.SyncTransforms();
		yield return new WaitForSecondsRealtime(2f); int one = pool.CountAll - before;
		player.inventory.itemInventory.AddItem(item); body.position = original + Vector3.right * 28f; player.transform.position = body.position; Physics.SyncTransforms();
		yield return new WaitForSecondsRealtime(2f);
		UnityEngine.Debug.Log($"Bob Dead probe: firstBatchCreated={one}, totalCreated={pool.CountAll - before}, active={pool.CountActive}, amount={player.inventory.itemInventory.GetAmount(item)}");
		player.inventory.itemInventory.RemoveItem(item); player.inventory.itemInventory.RemoveItem(item); body.position = original; player.transform.position = original; body.constraints = constraints;
		manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeSoulHarvester(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin), player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		if (target == null) { UnityEngine.Debug.Log("Soul harvester probe: spawn failed"); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break; }
		target.enemyMovement.StopMovement();
		yield return new WaitForSecondsRealtime(1f);
		var item = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SoulHarvester;
		player.inventory.itemInventory.AddItem(item);
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.angrySoulPool;
		int before = pool.CountAll;
		target.ReleaseToPool();
		UnityEngine.Debug.Log($"Soul harvester probe: afterRelease created={pool.CountAll - before}, active={pool.CountActive}, itemAmount={player.inventory.itemInventory.GetAmount(item)}, gameTime={Assets.Scripts.Utility.MyTime.time}, paused={Assets.Scripts.Utility.MyTime.paused}");
		yield return new WaitForSecondsRealtime(20f);
		UnityEngine.Debug.Log($"Soul harvester probe: later active={pool.CountActive}, inactive={pool.CountInactive}, gameTime={Assets.Scripts.Utility.MyTime.time}, paused={Assets.Scripts.Utility.MyTime.paused}");
		player.inventory.itemInventory.RemoveItem(item); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeInputTip(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var tip = FindObjectOfType<InputTipUi>(true);
		if (tip == null) { UnityEngine.Debug.Log("Input tip probe: missing component"); yield break; }
		bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
		int oldSetting = SaveManager.Instance.config.cfGameSettings.show_tips;
		SaveManager.Instance.config.cfGameSettings.show_tips = 1;
		tip.SetTip("Jump", MyInputManager.Jump);
		yield return new WaitForSecondsRealtime(2f);
		UnityEngine.Debug.Log($"Input tip probe: active={tip.gameObject.activeInHierarchy}, alpha={tip.group.alpha}, text={tip.t_tip.text}, label={tip.glyphContainer.t_text.text}, labelVisible={tip.glyphContainer.t_text.gameObject.activeInHierarchy}");
		yield return new WaitForSecondsRealtime(5f);
		SaveManager.Instance.config.cfGameSettings.show_tips = oldSetting; EnemyManager.Instance.enabledWaves = waves;
	}

	private IEnumerator ProbeIceCrystal(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin), player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		if (target == null) { UnityEngine.Debug.Log("Ice crystal probe: target spawn failed"); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break; }
		yield return new WaitForSecondsRealtime(1f);
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.IceCrystal);
		var hit = new Assets.Scripts.Actors.DamageContainer(100f, "ice crystal probe") { enemy = target, damage = .1f };
		player.inventory.itemInventory.PostDamage(hit);
		var key = Assets.Scripts.Game.Combat.EnemyDebuffs.EDebuff.Freeze;
		bool queued = target.debuffsToAdd.TryGetValue(key, out var pending); float duration = queued ? pending.duration : -1f;
		yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
		bool active = target.debuffs.TryGetValue(key, out var freeze); int ticks = active ? freeze.ticksLeft : -1;
		float startGameTime = Assets.Scripts.Utility.MyTime.time;
		float deadline = Time.realtimeSinceStartup + 10f;
		bool remaining = target.HasDebuff(key);
		while (target != null && remaining && Time.realtimeSinceStartup < deadline)
		{
			yield return new WaitForSecondsRealtime(.1f);
			remaining = target.HasDebuff(key);
		}
		UnityEngine.Debug.Log($"Ice crystal probe: queued={queued}, duration={duration}, active={active}, ticks={ticks}, remaining={remaining}, elapsedGame={Assets.Scripts.Utility.MyTime.time - startGameTime:F3}, targetActive={(target != null && target.gameObject.activeInHierarchy)}");
		player.inventory.itemInventory.RemoveItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.IceCrystal);
		manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable();
	}

	private IEnumerator ProbeBonker(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin);
		var target = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 4f, -1, true, canBeElite: false);
		var nearby = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 6f, -1, true, canBeElite: false);
		if (target == null || nearby == null) { UnityEngine.Debug.Log("Bonker probe: target spawn failed"); manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break; }
		target.enemyMovement.StopMovement(); nearby.enemyMovement.StopMovement();
		yield return new WaitForSecondsRealtime(1f);
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Bonker);
		int targetHits = 0, nearbyHits = 0, allHits = 0; float total = 0f;
		Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> observer = (e, dc) => { if (dc.damageSource == "Bonker") { allHits++; total += dc.damage; if (e == target) targetHits++; if (e == nearby) nearbyHits++; } };
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += observer;
		Physics.SyncTransforms();
		int scan = Assets.Scripts.Inventory__Items__Pickups.Weapons.EnemyTargeting.GetEnemiesInRadiusSafe(player, target.GetCenterPosition(), 4f, out var scanResults);
		UnityEngine.Debug.Log($"Bonker setup: scanned={scan}, targetActive={target.gameObject.activeInHierarchy}, targetCollider={target.collider.enabled}, targetTrigger={target.collider.isTrigger}, targetLayer={target.gameObject.layer}, enemyMask={GameManager.Instance.whatIsEnemy.value}, targetPos={target.transform.position}, nearbyPos={nearby.transform.position}");
		var dc = new Assets.Scripts.Actors.DamageContainer(100f, "probe") { enemy = target, damage = .1f };
		player.inventory.itemInventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Bonker).ProcOnHitEffects(dc);
		int effects = Assets.Scripts.Objects.Pooling.PoolManager.Instance.bonkPool.CountActive;
		UnityEngine.Debug.Log($"Bonker probe: targetHits={targetHits}, nearbyHits={nearbyHits}, allHits={allHits}, total={total}, activeEffects={effects}");
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= observer;
		player.inventory.itemInventory.RemoveItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Bonker);
		manager.enabledWaves = waves; foreach (var weapon in disabled) weapon.Enable(); yield break;
	}

	private IEnumerator ProbeBeefyRing(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var inventory = player.inventory;
		var hp = Assets.Scripts.Menu.Shop.EStat.MaxHealth;
		var damage = Assets.Scripts.Menu.Shop.EStat.DamageMultiplier;
		var ring = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.BeefyRing;
		bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
		float baselineHp = inventory.playerStats.GetStat(hp), baselineDamage = inventory.playerStats.GetStat(damage);
		inventory.itemInventory.AddItem(ring);
		yield return new WaitForSecondsRealtime(2f);
		float oneHp = inventory.playerStats.GetStat(hp), oneDamage = inventory.playerStats.GetStat(damage);
		int oneCombinedHp = inventory.playerHealth.GetCombinedMaxHp();
		inventory.itemInventory.AddItem(ring);
		yield return new WaitForSecondsRealtime(2f);
		float twoHp = inventory.playerStats.GetStat(hp), twoDamage = inventory.playerStats.GetStat(damage);
		int twoCombinedHp = inventory.playerHealth.GetCombinedMaxHp();
		inventory.itemInventory.RemoveItem(ring); inventory.itemInventory.RemoveItem(ring);
		yield return new WaitForSecondsRealtime(.5f);
		UnityEngine.Debug.Log($"Beefy ring probe: baselineHp={baselineHp} baselineDamage={baselineDamage} oneHp={oneHp} oneCombinedHp={oneCombinedHp} oneDamage={oneDamage} twoHp={twoHp} twoCombinedHp={twoCombinedHp} twoDamage={twoDamage} removedHp={inventory.playerStats.GetStat(hp)} removedDamage={inventory.playerStats.GetStat(damage)}");
		EnemyManager.Instance.enabledWaves = waves;
	}

	private IEnumerator ProbeIdleJuice(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
		bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
		var inventory = player.inventory; var damage = Assets.Scripts.Menu.Shop.EStat.DamageMultiplier;
		float baseline = inventory.playerStats.GetStat(damage);
		inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.IdleJuice);
		yield return new WaitForSecondsRealtime(3.5f);
		inventory.playerStats.ForceUpdateStats(); float stationary = inventory.playerStats.GetStat(damage);
		player.transform.position += Vector3.right * 3f; inventory.itemInventory.Tick(); inventory.playerStats.ForceUpdateStats();
		UnityEngine.Debug.Log($"Idle juice probe: baseline={baseline}, stationary={stationary}, afterMove={inventory.playerStats.GetStat(damage)}");
		inventory.itemInventory.RemoveItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.IdleJuice);
		body.constraints = constraints; EnemyManager.Instance.enabledWaves = waves;
	}

	private IEnumerator ProbeBobsLantern(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		int hits = 0;
		Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> onDamage = (enemy, dc) => { if (dc.damageSource == "BobsLantern") hits++; };
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += onDamage;
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.BobsLantern);
		yield return new WaitForSecondsRealtime(5f);
		UnityEngine.Debug.Log($"Bobs lantern probe: hits={hits}, amount={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.BobsLantern)}");
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= onDamage;
		player.inventory.itemInventory.RemoveItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.BobsLantern);
	}

	private IEnumerator ProbeCursedDoll(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		int hits = 0; float total = 0f;
		Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> onDamage = (enemy, dc) => { if (dc.damageSource == "CursedDoll") { hits++; total += dc.damage; } };
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += onDamage;
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.CursedDoll);
		yield return new WaitForSecondsRealtime(3f);
		UnityEngine.Debug.Log($"Cursed doll probe: hits={hits}, damage={total}, amount={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.CursedDoll)}");
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= onDamage;
		player.inventory.itemInventory.RemoveItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.CursedDoll);
	}

	private IEnumerator ProbePhantomShroud(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var inv = player.inventory;
		var disabled = new List<Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase>();
		foreach (var weapon in inv.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
		inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.PhantomShroud, 2);
		inv.statInventory.ChangeMovingStat("phantom evade probe", new Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier { stat = Assets.Scripts.Menu.Shop.EStat.Evasion, modifyType = Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType.Flat, modification = 1000f });
		inv.statInventory.Tick(); inv.playerStats.ForceUpdateStats();
		inv.playerHealth.hp = Mathf.Min(100, inv.playerHealth.maxHp); inv.playerHealth.shield = 0f;
		int before = inv.playerHealth.hp; int evades = 0;
		Action<Assets.Scripts.Actors.Enemies.Enemy> onEvade = _ => evades++;
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_Evaded += onEvade;
		inv.playerHealth.DamagePlayerExternal(10f, 0f, Vector3.zero, true, "phantom probe", Assets.Scripts.Actors.DcFlags.None);
		inv.playerStats.ForceUpdateStats();
		var component = new Assets.Scripts.Inventory__Items__Pickups.Stats.StatComponents();
		inv.itemInventory.PreAttack(new Assets.Scripts.Actors.DamageContainer(0f, "phantom probe"), component); float first = component.multiplicativeValue;
		inv.itemInventory.PreAttack(new Assets.Scripts.Actors.DamageContainer(0f, "phantom probe"), component); float second = component.multiplicativeValue;
		UnityEngine.Debug.Log($"Phantom Shroud probe: hpBefore={before}, hpAfter={inv.playerHealth.hp}, evades={evades}, first={first}, second={second}, attackSpeed={inv.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.AttackSpeed)}, moveSpeed={inv.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.MoveSpeedMultiplier)}");
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_Evaded -= onEvade;
		foreach (var weapon in disabled) weapon.Enable();
		yield break;
	}

	private IEnumerator ProbeEvade(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		int playerLayer = LayerMask.NameToLayer("Player"), enemyLayer = LayerMask.NameToLayer("Enemy");
		bool previouslyIgnored = Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer);
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
		yield return new WaitForSecondsRealtime(0.5f);
		var inv = player.inventory;
		inv.statInventory.ChangeMovingStat("evade probe", new Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier { stat = Assets.Scripts.Menu.Shop.EStat.Evasion, modifyType = Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType.Flat, modification = 1000f });
		inv.statInventory.Tick();
		inv.playerStats.ForceUpdateStats();
		inv.playerHealth.hp = Mathf.Min(100, inv.playerHealth.maxHp);
		inv.playerHealth.shield = 0f;
		int evades = 0;
		Action<Assets.Scripts.Actors.Enemies.Enemy> handler = _ => evades++;
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_Evaded += handler;
		global::Utility.MyRandom.random = new System.Random(0);
		inv.playerHealth.DamagePlayerExternal(10f, 0f, Vector3.zero, true, "evade probe", Assets.Scripts.Actors.DcFlags.None);
		UnityEngine.Debug.Log($"Android evade probe normal: hp={inv.playerHealth.hp}, evades={evades}, chance={inv.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.Evasion)}");
		inv.playerHealth.DamagePlayerExternal(10f, 0f, Vector3.zero, true, "evade probe", Assets.Scripts.Actors.DcFlags.BypassEvade);
		UnityEngine.Debug.Log($"Android evade probe bypass: hp={inv.playerHealth.hp}, evades={evades}");
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_Evaded -= handler;
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, previouslyIgnored);
	}

	private IEnumerator ProbeRegen(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var inv = player.inventory;
		inv.statInventory.ChangeMovingStat("regen probe", new Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier { stat = Assets.Scripts.Menu.Shop.EStat.HealthRegen, modifyType = Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType.Flat, modification = 110f });
		inv.statInventory.ChangeMovingStat("shield probe", new Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier { stat = Assets.Scripts.Menu.Shop.EStat.Shield, modifyType = Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType.Flat, modification = 10f });
		inv.statInventory.Tick();
		inv.playerStats.ForceUpdateStats();
		int playerLayer = LayerMask.NameToLayer("Player"), enemyLayer = LayerMask.NameToLayer("Enemy");
		bool previouslyIgnored = Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer);
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
		inv.playerHealth.hp = 51;
		inv.playerHealth.shield = 0f;
		inv.playerHealth.DamagePlayerExternal(1f, 0f, Vector3.zero, true, "regen probe");
		UnityEngine.Debug.Log($"Android regen probe start: hp={inv.playerHealth.hp}, shield={inv.playerHealth.shield}, regen={inv.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.HealthRegen)}, maxShield={inv.playerHealth.maxShield}");
		yield return new WaitForSecondsRealtime(1.3f);
		UnityEngine.Debug.Log($"Android regen probe early: hp={inv.playerHealth.hp}, shield={inv.playerHealth.shield}");
		yield return new WaitForSecondsRealtime(6f);
		UnityEngine.Debug.Log($"Android regen probe late: hp={inv.playerHealth.hp}, shield={inv.playerHealth.shield}");
		inv.statInventory.ChangeMovingStat("regen probe", new Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier { stat = Assets.Scripts.Menu.Shop.EStat.HealthRegen, modifyType = Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType.Flat, modification = -130f });
		inv.statInventory.Tick();
		inv.playerStats.ForceUpdateStats();
		inv.playerHealth.hp = 3;
		inv.playerHealth.shield = 0f;
		yield return new WaitForSecondsRealtime(2.2f);
		UnityEngine.Debug.Log($"Android regen probe negative: hp={inv.playerHealth.hp}, shield={inv.playerHealth.shield}, regen={inv.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.HealthRegen)}");
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, previouslyIgnored);
	}

	private IEnumerator ProbeLifesteal(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		Assets.Scripts.Actors.Enemies.Enemy enemy = null;
		for (int i = 0; i < 10 && enemy == null; i++)
		{
			foreach (var candidate in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>())
				if (candidate.CanTakeDamage()) { enemy = candidate; break; }
			if (enemy == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (enemy == null) { UnityEngine.Debug.Log("Android lifesteal probe: no damageable enemy"); yield break; }
		var inv = player.inventory;
		inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Chonkplate, 5);
		inv.playerStats.ForceUpdateStats();
		inv.playerHealth.hp = 50;
		var bloodmark = new Assets.Scripts.Actors.DamageContainer(1f, "lifesteal probe") { damage = 0.01f, damageEffect = Assets.Scripts.Game.Combat.EDamageEffect.Bloodmark };
		enemy.DamageFromPlayerOther(bloodmark);
		yield return new WaitForFixedUpdate();
		UnityEngine.Debug.Log($"Android lifesteal probe bloodmark: hp={inv.playerHealth.hp}, enemy={enemy.name}, stat={inv.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.Lifesteal)}");
		var hit = new Assets.Scripts.Actors.DamageContainer(1f, "lifesteal probe") { damage = 0.01f, damageEffect = Assets.Scripts.Game.Combat.EDamageEffect.None };
		enemy.DamageFromPlayerOther(hit);
		yield return new WaitForFixedUpdate();
		UnityEngine.Debug.Log($"Android lifesteal probe normal hit: hp={inv.playerHealth.hp}, enemy={enemy.name}");
	}

	private IEnumerator ProbeSilverPot(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		GameObject prefab = null;
		var lists = new[] { randomObjectPlacer.randomObjects, MapController.currentMap.randomObjectsOverride };
		foreach (var list in lists)
		{
			if (list == null) continue;
			foreach (var entry in list)
			{
				if (entry == null || entry.prefabs == null) continue;
				foreach (var candidate in entry.prefabs)
				{
					if (candidate == null) continue;
					var pot = candidate.GetComponent<Assets.Scripts.Inventory__Items__Pickups.Interactables.InteractablePot>();
					if (pot != null && pot.isSilver && !pot.isBig) { prefab = candidate; break; }
				}
				if (prefab != null) break;
			}
			if (prefab != null) break;
		}
		if (prefab == null) { UnityEngine.Debug.Log("Android silver pot probe: original prefab unavailable"); yield break; }
		var copy = Instantiate(prefab, player.transform.position + player.transform.forward * 1.5f, prefab.transform.rotation);
		int before = SaveManager.Instance.progression.silver;
		UnityEngine.Debug.Log($"Android silver pot probe ready: source={prefab.name}, silver={before}, position={copy.transform.position}");
		yield return new WaitForSecondsRealtime(10f);
		UnityEngine.Debug.Log($"Android silver pot probe result: destroyed={(copy == null)}, silverBefore={before}, silverAfter={SaveManager.Instance.progression.silver}, poolActive={Assets.Scripts.Objects.Pooling.PoolManager.Instance.silverPool.CountActive}");
	}

	private IEnumerator ProbeTomeAchievement()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && player != null && player.hasStarted && player.inventory?.tomeInventory != null && DataManager.Instance != null) break;
			yield return null;
		}
		if (player == null || player.inventory?.tomeInventory == null || DataManager.Instance == null)
		{ UnityEngine.Debug.Log("Tome achievement probe: missing player, inventory or data"); yield break; }
		const string achievement = "a_turboSocks";
		var inventory = player.inventory.tomeInventory;
		bool before = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(achievement);
		if (before || inventory.GetTomeLevel(ETome.Agility) != 0)
		{ UnityEngine.Debug.Log("Tome achievement probe: unsuitable initial state"); yield break; }
		if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
		inventory.tomeLevels[ETome.Agility] = 4;
		inventory.AddTome(DataManager.Instance.GetTome(ETome.Agility), new List<Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier>(), Assets.Scripts.Inventory__Items__Pickups.ERarity.New);
		bool after = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(achievement);
		UnityEngine.Debug.Log($"Tome achievement probe: seededLevel=4, before={before}, levelAfter={inventory.GetTomeLevel(ETome.Agility)}, unlocked={after}");
	}

	private IEnumerator ProbeItemAchievement()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && player != null && player.hasStarted && player.inventory?.itemInventory != null && DataManager.Instance != null) break;
			yield return null;
		}
		if (player == null || player.inventory?.itemInventory == null || DataManager.Instance == null)
		{ UnityEngine.Debug.Log("Item achievement probe: missing player, inventory or data"); yield break; }
		const string achievement = "a_kevin";
		var inventory = player.inventory.itemInventory;
		var item = Assets.Scripts.Inventory__Items__Pickups.Items.EItem.LeechingCrystal;
		bool before = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(achievement);
		if (before || inventory.GetAmount(item) != 0) { UnityEngine.Debug.Log("Item achievement probe: unsuitable initial state"); yield break; }
		if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
		inventory.AddItem(item, 2);
		bool afterTwo = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(achievement);
		inventory.AddItem(item);
		bool afterThree = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(achievement);
		UnityEngine.Debug.Log($"Item achievement probe: before={before}, afterTwo={afterTwo}, count={inventory.GetAmount(item)}, afterThree={afterThree}");
	}

	private IEnumerator ProbeWeaponAchievement()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			if (!isGenerating && player != null && player.hasStarted && player.inventory?.weaponInventory != null && DataManager.Instance != null) break;
			yield return null;
		}
		if (player == null || player.inventory?.weaponInventory == null || DataManager.Instance == null)
		{ UnityEngine.Debug.Log("Weapon achievement probe: missing player, inventory or data"); yield break; }
		const string name = "a_durationTome";
		if (Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(name))
		{ UnityEngine.Debug.Log("Weapon achievement probe: already unlocked"); yield break; }
		if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
		var inventory = player.inventory.weaponInventory;
		var data = DataManager.Instance.GetWeapon(EWeapon.Axe);
		if (!inventory.weapons.ContainsKey(EWeapon.Axe)) inventory.AddWeapon(data, null);
		if (!inventory.weapons.TryGetValue(EWeapon.Axe, out var weapon))
		{ UnityEngine.Debug.Log("Weapon achievement probe: axe was not added"); yield break; }
		weapon.level = 9;
		bool before = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(name);
		inventory.AddWeapon(data, null);
		bool after = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone(name);
		UnityEngine.Debug.Log($"Weapon achievement probe: seededLevel=9, before={before}, levelAfter={inventory.GetWeaponLevel(EWeapon.Axe)}, unlocked={after}");
	}

	private IEnumerator ProbePotAchievement()
	{
		float deadline = Time.realtimeSinceStartup + 120f;
		Assets.Scripts.Actors.Player.MyPlayer player = null;
		Assets.Scripts.Inventory__Items__Pickups.Interactables.InteractablePot original = null;
		while (Time.realtimeSinceStartup < deadline)
		{
			player = gameManager != null ? gameManager.GetPlayer() : null;
			original = FindObjectsOfType<Assets.Scripts.Inventory__Items__Pickups.Interactables.InteractablePot>().FirstOrDefault(p => !p.isSilver && !p.isBig);
			if (!isGenerating && player != null && player.hasStarted && player.playerInput != null && player.playerInput.detectInteractables != null && original != null) break;
			yield return null;
		}
		if (player == null || original == null || player.playerInput == null || player.playerInput.detectInteractables == null)
		{ UnityEngine.Debug.Log("Pot achievement probe: player, detector or pot unavailable"); yield break; }
		yield return new WaitForSecondsRealtime(2f);
		deadline = Time.realtimeSinceStartup + 45f;
		while ((Assets.Scripts.Utility.MyTime.paused || gameManager.cutscene || player.isTeleporting) && Time.realtimeSinceStartup < deadline) yield return null;
		if (Assets.Scripts.Utility.MyTime.paused || gameManager.cutscene || player.isTeleporting)
		{ UnityEngine.Debug.Log($"Pot achievement probe: input unavailable paused={Assets.Scripts.Utility.MyTime.paused}, cutscene={gameManager.cutscene}, teleporting={player.isTeleporting}"); yield break; }
		var achievements = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone("a_xpTome");
		int previous = Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats.GetStat("potsBroken");
		if (achievements || previous > 19) { UnityEngine.Debug.Log($"Pot achievement probe: unsuitable state unlocked={achievements}, runPots={previous}"); yield break; }
		Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats.AddValue("potsBroken", 19 - previous);
		if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
		var body = player.playerMovement.GetRb(); var oldConstraints = body.constraints;
		body.constraints = RigidbodyConstraints.FreezeAll; body.velocity = Vector3.zero;
		body.position += Vector3.up * 10f; Physics.SyncTransforms();
		yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
		var detector = player.playerInput.detectInteractables;
		var copy = Instantiate(original.gameObject, detector.transform.position, original.transform.rotation);
		copy.SetActive(true); Physics.SyncTransforms();
		yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return new WaitForSecondsRealtime(0.5f);
		var selected = detector.GetType().GetField("currentInteractable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(detector) as BaseInteractable;
		bool selectedPot = selected != null && selected.gameObject == copy;
		var potCollider = copy.GetComponent<Collider>();
		UnityEngine.Debug.Log($"Pot achievement probe: detector={detector.transform.position}, pot={copy.transform.position}, selected={(selected != null ? selected.name : "none")}, active={detector.isActiveAndEnabled}, potLayer={copy.layer}, potTag={copy.tag}, collider={(potCollider != null && potCollider.enabled)}, queriesTriggers={Physics.queriesHitTriggers}, paused={Assets.Scripts.Utility.MyTime.paused}, cutscene={gameManager.cutscene}, teleporting={player.isTeleporting}");
		bool before = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone("a_xpTome");
		var statInventory = player.inventory.statInventory;
		statInventory.permanentChanges.TryGetValue(Assets.Scripts.Menu.Shop.EStat.Luck, out var beforeModifiers);
		int luckCountBefore = beforeModifiers != null ? beforeModifiers.Count : 0;
		if (selectedPot) detector.TryInteract();
		int after = Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats.GetStat("potsBroken");
		bool unlocked = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsAchievementDone("a_xpTome");
		statInventory.permanentChanges.TryGetValue(Assets.Scripts.Menu.Shop.EStat.Luck, out var afterModifiers);
		int luckCountAfter = afterModifiers != null ? afterModifiers.Count : 0;
		float lastLuck = luckCountAfter > luckCountBefore ? afterModifiers[luckCountAfter - 1].modification : 0f;
		UnityEngine.Debug.Log($"Pot achievement probe: seeded=19, selectedPot={selectedPot}, before={before}, runPotsAfter={after}, unlocked={unlocked}, stage={MapController.index}, newLuckModifiers={luckCountAfter - luckCountBefore}, luckAmount={lastLuck:F4}");
		body.constraints = oldConstraints;
		if (copy != null) Destroy(copy);
	}

	private IEnumerator ProbePot(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var pots = FindObjectsOfType<Assets.Scripts.Inventory__Items__Pickups.Interactables.InteractablePot>();
		Assets.Scripts.Inventory__Items__Pickups.Interactables.InteractablePot original = null;
		foreach (var pot in pots) if (!pot.isSilver && !pot.isBig) { original = pot; break; }
		if (original == null) { UnityEngine.Debug.Log("Android pot probe: no original small pot found"); yield break; }
		Utility.MyRandom.random = new System.Random(androidPotProbeSeed);
		if (androidPotProbeSeed == 8) player.inventory.playerHealth.hp = player.inventory.playerHealth.maxHp / 2;
		var copy = Instantiate(original.gameObject, player.transform.position + player.transform.forward * 1.5f, original.transform.rotation);
		copy.GetComponent<Assets.Scripts.Inventory__Items__Pickups.Interactables.InteractablePot>().androidProbeSeed = androidPotProbeSeed;
		UnityEngine.Debug.Log($"Android pot probe ready: seed={androidPotProbeSeed}, source={original.name}, position={copy.transform.position}, xp={player.inventory.playerXp.xp}, gold={player.inventory.goldInt}, hp={player.inventory.playerHealth.hp}");
		yield return new WaitForSecondsRealtime(8f);
		if (androidPotProbeSeed == 8)
		{
			for (int i = 0; i < 6; i++)
			{
				foreach (var pickup in FindObjectsOfType<Pickup>())
					if (pickup.ePickup == Assets.Scripts.Inventory__Items__Pickups.Pickups.EPickup.Health)
					{
						var body = player.GetComponent<Rigidbody>();
						body.position = pickup.transform.position;
						UnityEngine.Debug.Log($"Android pot probe: moved player to health pickup at {pickup.transform.position}");
						break;
					}
				yield return new WaitForSecondsRealtime(1f);
			}
		}
		UnityEngine.Debug.Log($"Android pot probe result: destroyed={(copy == null)}, xp={player.inventory.playerXp.xp}, gold={player.inventory.goldInt}, hp={player.inventory.playerHealth.hp}");
	}

	private IEnumerator ProbeEnemyGold(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var inventory = player.inventory;
		var stats = inventory.playerStats;
		float regenBefore = stats.GetStat(Assets.Scripts.Menu.Shop.EStat.HealthRegen);
		float xpBefore = stats.GetStat(Assets.Scripts.Menu.Shop.EStat.XpIncreaseMultiplier);
		inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Medkit, 2);
		inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.TimeBracelet, 2);
		inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Wrench, 2);
		inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.GoldenGlove, 7);
		stats.ForceUpdateStats();
		var wrench = (Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations.ItemWrench)inventory.itemInventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Wrench);
		UnityEngine.Debug.Log($"Common item probe: regenBefore={regenBefore}, regenAfter={stats.GetStat(Assets.Scripts.Menu.Shop.EStat.HealthRegen)}, xpBefore={xpBefore}, xpAfter={stats.GetStat(Assets.Scripts.Menu.Shop.EStat.XpIncreaseMultiplier)}, chargeSpeed={wrench.GetChargeSpeedIncrease()}, chargeReward={wrench.GetChargeRewardMultiplier()}");
		Assets.Scripts.Actors.Enemies.Enemy target = null;
		for (int i = 0; i < 10 && target == null; i++)
		{
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { target = enemy; break; }
			if (target == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (target == null) { UnityEngine.Debug.Log("Enemy gold probe: no live enemy"); yield break; }
		var goldPool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.goldPool;
		int before = goldPool.CountActive;
		global::Utility.MyRandom.random = new System.Random(12345);
		target.EnemyDied(new Assets.Scripts.Actors.DamageContainer(1f, "enemy gold probe"));
		UnityEngine.Debug.Log($"Enemy gold probe: target={target.enemyData.enemyName}, baseGold={target.GetMoney()}, gloveStacks=7, goldObjectsBefore={before}, goldObjectsAfter={goldPool.CountActive}, spawned={goldPool.CountActive - before}");
	}

	private IEnumerator ProbeMoldyCheese(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		Assets.Scripts.Actors.Enemies.Enemy target = null;
		for (int i = 0; i < 10 && target == null; i++)
		{
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { target = enemy; break; }
			if (target == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (target == null) { UnityEngine.Debug.Log("Moldy probe: no live enemy"); yield break; }
		var inventory = player.inventory.itemInventory;
		inventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.MoldyCheese, 5);
		target.hp = target.maxHp = 1000f;
		var hit = new Assets.Scripts.Actors.DamageContainer(0f, "moldy probe") { enemy = target };
		target.DamageFromPlayerOther(hit);
		int zeroQueued = target.debuffsToAdd.Count;
		hit.procCoefficient = 1f;
		target.DamageFromPlayerOther(hit);
		var key = Assets.Scripts.Game.Combat.EnemyDebuffs.EDebuff.Poison;
		int queued = target.debuffsToAdd.TryGetValue(key, out var pending) ? pending.stacks : 0;
		yield return new WaitForFixedUpdate();
		yield return new WaitForFixedUpdate();
		if (!target.debuffs.TryGetValue(key, out var poison)) { UnityEngine.Debug.Log($"Moldy probe: poison missing, zeroQueued={zeroQueued}, queued={queued}"); yield break; }
		int stacks = poison.GetStacks(), ticks = poison.ticksLeft;
		float hpBefore = target.hp;
		target.DebuffTick();
		float damage = hpBefore - target.hp;
		int remaining = poison.ticksLeft;
		while (target.HasDebuff(key) && target.gameObject.activeInHierarchy) target.DebuffTick();
		UnityEngine.Debug.Log($"Moldy probe: zeroQueued={zeroQueued}, queued={queued}, stacks={stacks}, ticks={ticks}, damage={damage}, remaining={remaining}, expired={!target.HasDebuff(key)}");
		inventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.OverpoweredLamp);
		target.DamageFromPlayerOther(hit);
		target.DamageFromPlayerOther(hit);
		int combined = target.debuffsToAdd.TryGetValue(key, out pending) ? pending.stacks : 0;
		yield return new WaitForFixedUpdate();
		yield return new WaitForFixedUpdate();
		if (!target.debuffs.TryGetValue(key, out poison)) { UnityEngine.Debug.Log($"Moldy lamp probe: missing, queued={combined}"); yield break; }
		int beforeTicks = poison.ticksLeft;
		float beforeHp = target.hp;
		yield return new WaitForSecondsRealtime(0.7f);
		UnityEngine.Debug.Log($"Moldy lamp probe: queued={combined}, stacks={poison.GetStacks()}, beforeTicks={beforeTicks}, afterTicks={poison.ticksLeft}, damage={beforeHp - target.hp}");
	}

	private IEnumerator ProbeBorgor(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		Assets.Scripts.Actors.Enemies.Enemy target = null;
		for (int i = 0; i < 10 && target == null; i++)
		{
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { target = enemy; break; }
			if (target == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (target == null) { UnityEngine.Debug.Log("Borgor probe: no live enemy"); yield break; }
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Borgar, 100);
		target.enemyMovement.distanceToTarget = 1f;
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.borgorPool;
		int before = pool.CountActive;
		target.EnemyDied(new Assets.Scripts.Actors.DamageContainer(0f, "borgor probe"));
		int spawned = pool.CountActive - before;
		var pickups = FindObjectsOfType<Borgar>();
		if (spawned != 1 || pickups.Length == 0) { UnityEngine.Debug.Log($"Borgor probe: spawned={spawned}, pickups={pickups.Length}"); yield break; }
		player.inventory.playerHealth.hp = 1;
		pickups[0].SendMessage("Collect");
		UnityEngine.Debug.Log($"Borgor probe: spawned={spawned}, hpAfter={player.inventory.playerHealth.hp}, maxHp={player.inventory.playerHealth.GetCombinedMaxHp()}, activeAfter={pool.CountActive}");
	}

	private IEnumerator ProbeBurn(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		Assets.Scripts.Actors.Enemies.Enemy target = null;
		for (int i = 0; i < 10 && target == null; i++)
		{
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { target = enemy; break; }
			if (target == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (target == null) { UnityEngine.Debug.Log("Burn probe: no live enemy"); yield break; }
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Dragonfire);
		target.hp = target.maxHp = 1000f;
		var key = Assets.Scripts.Game.Combat.EnemyDebuffs.EDebuff.Burn;
		var hit = new Assets.Scripts.Actors.DamageContainer(100f, "burn probe") { enemy = target, element = Assets.Scripts.Game.Combat.EElement.Fire };
		target.DamageFromPlayerOther(hit);
		bool queued = target.debuffsToAdd.ContainsKey(key);
		yield return new WaitForFixedUpdate();
		yield return new WaitForFixedUpdate();
		if (!target.debuffs.TryGetValue(key, out var burn)) { UnityEngine.Debug.Log($"Burn probe: missing, queued={queued}"); yield break; }
		int ticks = burn.ticksLeft;
		float before = target.hp;
		target.DebuffTick();
		float first = before - target.hp;
		before = target.hp;
		target.DebuffTick();
		float second = before - target.hp;
		while (target.HasDebuff(key) && target.gameObject.activeInHierarchy) target.DebuffTick();
		UnityEngine.Debug.Log($"Burn probe: queued={queued}, ticks={ticks}, first={first}, second={second}, expired={!target.HasDebuff(key)}");
	}

	private IEnumerator ProbeFirefield(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		Assets.Scripts.Actors.Enemies.Enemy target = null;
		for (int i = 0; i < 10 && target == null; i++)
		{
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { target = enemy; break; }
			if (target == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (target == null) { UnityEngine.Debug.Log("Firefield probe: no live enemy"); yield break; }
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Dragonfire);
		target.hp = target.maxHp = 1000f;
		int damageEvents = 0;
		float firstDamage = 0f;
		System.Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> handler = (enemy, dc) =>
		{
			if (dc.damageSource != "Dragonfire") return;
			if (damageEvents++ == 0) firstDamage = dc.damage;
		};
		target.A_DamageNonStatic += handler;
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.firefieldPool;
		int before = pool.CountActive;
		var hit = new Assets.Scripts.Actors.DamageContainer(100f, "firefield probe") { enemy = target, damage = 10f };
		target.DamageFromPlayerOther(hit);
		target.DamageFromPlayerOther(hit);
		int spawned = pool.CountActive - before;
		yield return new WaitForSecondsRealtime(0.7f);
		UnityEngine.Debug.Log($"Firefield probe: spawned={spawned}, damageEvents={damageEvents}, firstDamage={firstDamage}, active={pool.CountActive}");
		target.A_DamageNonStatic -= handler;
	}

	private IEnumerator ProbeIce(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		Assets.Scripts.Actors.Enemies.Enemy target = null;
		for (int i = 0; i < 10 && target == null; i++)
		{
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { target = enemy; break; }
			if (target == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (target == null) { UnityEngine.Debug.Log("Ice probe: no live enemy"); yield break; }
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.IceCube);
		target.hp = target.maxHp = 1000f;
		var key = Assets.Scripts.Game.Combat.EnemyDebuffs.EDebuff.Freeze;
		var hit = new Assets.Scripts.Actors.DamageContainer(100f, "ice probe") { enemy = target, damage = 10f, element = Assets.Scripts.Game.Combat.EElement.Ice };
		target.DamageFromPlayerOther(hit);
		bool queued = target.debuffsToAdd.ContainsKey(key);
		float totalDamage = 1000f - target.hp;
		yield return new WaitForFixedUpdate();
		yield return new WaitForFixedUpdate();
		if (!target.debuffs.TryGetValue(key, out var freeze)) { UnityEngine.Debug.Log($"Ice probe: missing, queued={queued}, damage={totalDamage}"); yield break; }
		int ticks = freeze.ticksLeft;
		target.enemyMovement.FindNextPosition();
		var velocityField = typeof(Actors.Enemies.EnemyMovementRb).GetField("baseVelocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		float frozenSpeed = ((Vector3)velocityField.GetValue(target.enemyMovement)).magnitude;
		while (target.HasDebuff(key)) target.DebuffTick();
		target.enemyMovement.FindNextPosition();
		float normalSpeed = ((Vector3)velocityField.GetValue(target.enemyMovement)).magnitude;
		int cap = Assets.Scripts.Inventory.Stats.EnemyStats.GetCapCC();
		int applied = 1;
		for (int i = 1; i <= cap; i++)
		{
			target.AddDebuff(key, hit, 3f);
			target.MyFixedUpdate();
			if (target.HasDebuff(key)) { applied++; target.RemoveDebuff(key, false); }
		}
		UnityEngine.Debug.Log($"Ice probe: queued={queued}, damage={totalDamage}, ticks={ticks}, frozenSpeed={frozenSpeed}, normalSpeed={normalSpeed}, cap={cap}, applicationsAccepted={applied}, remainingFrozen={Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations.DebuffIce.numFrozenEnemies}");
	}

	private IEnumerator ProbeFlex(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		Assets.Scripts.Actors.Enemies.Enemy target = null;
		for (int i = 0; i < 10 && target == null; i++)
		{
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { target = enemy; break; }
			if (target == null) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (target == null) { UnityEngine.Debug.Log("Flex probe: no live enemy"); yield break; }
		var owned = player.inventory.passiveAbility as Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations.PassiveAbilityFlex;
		var flex = owned ?? new Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations.PassiveAbilityFlex();
		if (owned == null) flex.Init();
		var readyField = typeof(Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations.PassiveAbilityFlex).GetField("flexReadyAtTime", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
		float previousReady = (float)readyField.GetValue(null);
		readyField.SetValue(null, Assets.Scripts.Utility.MyTime.time - 1f);
		Vector3 position = player.transform.position + player.transform.forward * 2f;
		target.transform.position = position;
		target.rb.position = position;
		target.hp = target.maxHp = 1000f;
		Physics.SyncTransforms();
		var bypass = new Assets.Scripts.Actors.DamageContainer(0f, "flex probe") { damage = 40f, flags = Assets.Scripts.Actors.DcFlags.BypassAll };
		bool bypassStopped = player.inventory.playerHealth.CheckStopDamage(bypass, false);
		var normal = new Assets.Scripts.Actors.DamageContainer(0f, "flex probe") { damage = 40f, flags = Assets.Scripts.Actors.DcFlags.BypassEvade };
		bool stopped = player.inventory.playerHealth.CheckStopDamage(normal, false);
		float enemyDamage = 1000f - target.hp;
		bool queued = target.debuffsToAdd.ContainsKey(Assets.Scripts.Game.Combat.EnemyDebuffs.EDebuff.Stun);
		float bonus = 0f;
		if (flex.statModifiers.TryGetValue(Assets.Scripts.Menu.Shop.EStat.DamageMultiplier, out var modifiers)) foreach (var modifier in modifiers.GetModifiers()) bonus += modifier.modification;
		readyField.SetValue(null, Assets.Scripts.Utility.MyTime.time - 1f);
		var boss = new Assets.Scripts.Actors.DamageContainer(0f, "flex probe") { damage = 40f, flags = Assets.Scripts.Actors.DcFlags.BypassEvade | Assets.Scripts.Actors.DcFlags.BossDamage };
		bool bossStopped = player.inventory.playerHealth.CheckStopDamage(boss, false);
		yield return new WaitForFixedUpdate();
		yield return new WaitForFixedUpdate();
		bool stunned = target.HasDebuff(Assets.Scripts.Game.Combat.EnemyDebuffs.EDebuff.Stun);
		target.enemyMovement.FindNextPosition();
		var velocityField = typeof(Actors.Enemies.EnemyMovementRb).GetField("baseVelocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		float stunnedSpeed = ((Vector3)velocityField.GetValue(target.enemyMovement)).magnitude;
		UnityEngine.Debug.Log($"Flex probe: bypassStopped={bypassStopped}, bypassDamage={bypass.damage}, stopped={stopped}, damage={normal.damage}, enemyDamage={enemyDamage}, queued={queued}, bonus={bonus}, bossStopped={bossStopped}, bossDamage={boss.damage}, stunned={stunned}, stunnedSpeed={stunnedSpeed}");
		readyField.SetValue(null, previousReady);
		if (owned == null) flex.Cleanup();
	}

	private IEnumerator ProbeLightning(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var targets = new List<Assets.Scripts.Actors.Enemies.Enemy>();
		for (int attempt = 0; attempt < 20 && targets.Count < 3; attempt++)
		{
			targets.Clear();
			foreach (var enemy in FindObjectsOfType<Assets.Scripts.Actors.Enemies.Enemy>()) if (enemy.CanTakeDamage()) { targets.Add(enemy); if (targets.Count == 3) break; }
			if (targets.Count < 3) yield return new WaitForSecondsRealtime(0.5f);
		}
		if (targets.Count < 3) { UnityEngine.Debug.Log($"Lightning probe: targets={targets.Count}"); yield break; }
		for (int i = 0; i < targets.Count; i++)
		{
			Vector3 position = player.transform.position + new Vector3(60f + i * 2f, 0f, 0f);
			targets[i].transform.position = position;
			targets[i].rb.position = position;
			targets[i].hp = targets[i].maxHp = 1000f;
		}
		Physics.SyncTransforms();
		var chainDc = new Assets.Scripts.Actors.DamageContainer(0f, "chain probe") { enemy = targets[0], damage = 20f, element = Assets.Scripts.Game.Combat.EElement.Lightning };
		Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponUtility.LightningStrike(targets[0], 2, chainDc, 8f, 0f);
		var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance;
		var lines = FindObjectsOfType<ChainLightning>();
		int linePoints = lines.Length > 0 ? lines[0].lineRenderer.positionCount : 0;
		float first = 1000f - targets[0].hp, second = 1000f - targets[1].hp, third = 1000f - targets[2].hp;
		Vector3 near = player.transform.position + player.transform.forward * 2f;
		targets[0].transform.position = near;
		targets[0].rb.position = near;
		Physics.SyncTransforms();
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.LightningOrb);
		player.inventory.itemInventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.LightningOrb).Tick();
		int orbEvents = 0;
		float orbDamage = 0f;
		System.Action<Assets.Scripts.Actors.Enemies.Enemy, Assets.Scripts.Actors.DamageContainer> handler = (enemy, dc) => { if (dc.damageSource == "LightningOrb") { orbEvents++; orbDamage = dc.damage; } };
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage += handler;
		var hit = new Assets.Scripts.Actors.DamageContainer(100f, "orb probe") { enemy = targets[0], damage = 10f, element = Assets.Scripts.Game.Combat.EElement.Lightning };
		targets[0].DamageFromPlayerOther(hit);
		bool queued = targets[0].debuffsToAdd.ContainsKey(Assets.Scripts.Game.Combat.EnemyDebuffs.EDebuff.Stun);
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= handler;
		UnityEngine.Debug.Log($"Lightning probe: chainFirst={first}, chainSecond={second}, chainThird={third}, orbEvents={orbEvents}, orbDamage={orbDamage}, stunQueued={queued}");
		var orb = player.inventory.itemInventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.LightningOrb);
		typeof(Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations.ItemLightningOrb).GetField("procChance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(orb, 0f);
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.IceCube);
		var ice = player.inventory.itemInventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.IceCube);
		ice.ProcOnHitEffects(new Assets.Scripts.Actors.DamageContainer(100f, "ice visual probe") { enemy = targets[0], damage = 1f });
		typeof(Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations.ItemIceCube).GetField("procChance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(ice, 0f);
		int strikes = pools.lightningStrikePool.CountActive, chains = pools.chainLightningPool.CountActive, freezes = pools.freezeFxPool.CountActive;
		yield return new WaitForSecondsRealtime(1.5f);
		UnityEngine.Debug.Log($"Lightning visual probe: strikes={strikes}, chains={chains}, linePoints={linePoints}, freezes={freezes}, strikesAfter={pools.lightningStrikePool.CountActive}, chainsAfter={pools.chainLightningPool.CountActive}, freezesAfter={pools.freezeFxPool.CountActive}");
	}

	private IEnumerator ProbeMicrowave(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var graveyard = DataManager.Instance.maps.Find(m => m != null && m.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard);
		GameObject prefab = null;
		if (graveyard != null && graveyard.randomObjectsOverride != null)
			foreach (var entry in graveyard.randomObjectsOverride)
				if (entry != null && entry.prefabs != null)
					foreach (var candidate in entry.prefabs)
						if (candidate != null && candidate.GetComponentInChildren<InteractableMicrowave>(true) != null) { prefab = candidate; break; }
		if (prefab == null) { UnityEngine.Debug.Log("Microwave probe: original prefab missing"); yield break; }
		player.inventory.playerHealth.hp = 100000;
		var inventory = player.inventory.itemInventory;
		inventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover);
		inventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Borgar);
		player.inventory.ChangeGold(100 - player.inventory.goldInt);
		var spawned = Instantiate(prefab, player.transform.position + player.transform.forward * 2f, Quaternion.identity);
		var machine = spawned.GetComponentInChildren<InteractableMicrowave>(true);
		yield return null;
		var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
		typeof(InteractableMicrowave).GetField("<rarity>k__BackingField", flags).SetValue(machine, Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Common);
		typeof(InteractableMicrowave).GetField("<usesLeft>k__BackingField", flags).SetValue(machine, 1);
		int cloverBefore = inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover);
		int borgarBefore = inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Borgar);
		float goldBefore = player.inventory.gold;
		UnityEngine.Debug.Log($"Microwave probe ready: eligible={machine.CanInteract()}, clover={cloverBefore}, borgar={borgarBefore}, gold={goldBefore}, price={machine.GetPrice()}");
		var ui = UiManager.Instance.encounterWindows.microwaveWindow as MicrowaveUi;
		if (androidMicrowaveControllerProbe)
		{
			yield return new WaitForSecondsRealtime(1f);
			var detector = player.playerInput != null ? player.playerInput.detectInteractables : null;
			var current = detector != null ? typeof(Assets.Scripts.Inventory__Items__Pickups.Interactables.DetectInteractables).GetField("currentInteractable", flags).GetValue(detector) as BaseInteractable : null;
			UnityEngine.Debug.Log($"Microwave probe waiting for controller interaction: detected={current == machine}, current={(current != null ? current.name : "<none>")}");
			float openDeadline = Time.realtimeSinceStartup + 30f;
			while (ui != null && !ui.gameObject.activeInHierarchy && Time.realtimeSinceStartup < openDeadline)
			{
				if (Input.GetKeyDown(KeyCode.JoystickButton2))
				{
					current = detector != null ? typeof(Assets.Scripts.Inventory__Items__Pickups.Interactables.DetectInteractables).GetField("currentInteractable", flags).GetValue(detector) as BaseInteractable : null;
					UnityEngine.Debug.Log($"Microwave probe interact key: action={MyInputManager.GetButtonDown(MyInputManager.Interact)}, detected={current == machine}, current={(current != null ? current.name : "<none>")}, paused={Assets.Scripts.Utility.MyTime.paused}, player={player.transform.position}, machine={machine.transform.position}");
				}
				yield return null;
			}
		}
		else machine.Interact();
		yield return null;
		if (ui == null || !ui.gameObject.activeInHierarchy) { UnityEngine.Debug.Log("Microwave probe: selection window did not open"); Destroy(spawned); yield break; }
		var buttons = typeof(MicrowaveUi).GetField("itemPrefabs", flags).GetValue(ui) as List<MicrowaveItemButton>;
		var clover = DataManager.Instance.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover);
		MicrowaveItemButton target = null;
		if (buttons != null) foreach (var button in buttons) if (button != null && button.gameObject.activeInHierarchy && button.icon.texture == clover.icon) { target = button; break; }
		UnityEngine.Debug.Log($"Microwave probe window: open={ui.gameObject.activeInHierarchy}, target={(target != null)}, price={ui.t_price.text}");
		if (target == null || target.GetButton() == null) { Destroy(spawned); yield break; }
		if (androidMicrowaveControllerProbe)
		{
			var selectedButton = Assets.Scripts.Managers.ButtonManager.GetCurrentButton();
			UnityEngine.Debug.Log($"Microwave probe waiting for controller selection: selected={(selectedButton != null ? selectedButton.name : "<none>")}, target={target.name}, matches={selectedButton == target}");
			float selectionDeadline = Time.realtimeSinceStartup + 30f;
			while (ui.gameObject.activeInHierarchy && Time.realtimeSinceStartup < selectionDeadline) yield return null;
			if (!machine.isCooking) { UnityEngine.Debug.Log("Microwave probe: controller selection did not start cooking"); Destroy(spawned); yield break; }
		}
		else target.GetButton().onClick.Invoke();
		UnityEngine.Debug.Log($"Microwave probe started: cooking={machine.isCooking}, uses={machine.usesLeft}, clover={inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}, borgar={inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Borgar)}, gold={player.inventory.gold}, paused={Assets.Scripts.Utility.MyTime.paused}");
		float deadline = Time.realtimeSinceStartup + 20f;
		while (machine.isCooking && Time.realtimeSinceStartup < deadline) yield return null;
		bool eligible = machine.CanInteract();
		bool collected;
		if (androidMicrowaveControllerProbe)
		{
			yield return new WaitForSecondsRealtime(1f);
			var detector = player.playerInput != null ? player.playerInput.detectInteractables : null;
			var current = detector != null ? typeof(Assets.Scripts.Inventory__Items__Pickups.Interactables.DetectInteractables).GetField("currentInteractable", flags).GetValue(detector) as BaseInteractable : null;
			UnityEngine.Debug.Log($"Microwave probe waiting for controller collection: eligible={eligible}, detected={current == machine}, current={(current != null ? current.name : "<none>")}");
			float collectDeadline = Time.realtimeSinceStartup + 30f;
			while (inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover) == cloverBefore && Time.realtimeSinceStartup < collectDeadline) yield return null;
			collected = inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover) > cloverBefore;
		}
		else collected = eligible && machine.Interact();
		UnityEngine.Debug.Log($"Microwave probe collected: eligible={eligible}, collected={collected}, clover={inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}, borgar={inventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Borgar)}, gold={player.inventory.gold}");
		yield return new WaitForSecondsRealtime(1f);
		UnityEngine.Debug.Log($"Microwave probe final: exploded={(machine.explosion != null && machine.explosion.activeSelf)}, minimap={(machine.minimapIcon != null && machine.minimapIcon.activeSelf)}");
		Destroy(spawned);
	}

	private IEnumerator ProbeWorldChest(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var sources = FindObjectsOfType<Assets.Scripts.Inventory__Items__Pickups.Chests.InteractableChest>();
		if (sources.Length == 0) { UnityEngine.Debug.Log("World chest probe: no spawned original chest found"); yield break; }
		var pool = Assets.Scripts.Inventory__Items__Pickups.RunUnlockables.availableItems[Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Common];
		var oldPool = new List<ItemData>(pool);
		var stats = SaveManager.Instance.stats.stats;
		string[] names = { "chestsOpened", "chestsBought", "itemsPickedUp", "goldEarned", "goldSpent" };
		var savedStats = new float[names.Length];
		for (int i = 0; i < names.Length; i++) savedStats[i] = stats[names[i]].value;
		pool.Clear(); pool.Add(DataManager.Instance.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover));
		stats["chestsOpened"].value = -1f;
		var config = SaveManager.Instance.config.cfGameSettings;
		int oldSkip = config.skip_chest_animation;
		config.skip_chest_animation = 0;
		int price = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice();
		player.inventory.ChangeGold(price - player.inventory.goldInt);
		var copy = Instantiate(sources[0].gameObject, player.transform.position + player.transform.forward * 1.5f, sources[0].transform.rotation);
		var chest = copy.GetComponent<Assets.Scripts.Inventory__Items__Pickups.Chests.InteractableChest>();
		chest.chestType = Assets.Scripts.Inventory__Items__Pickups.Interactables.EChest.Normal;
		bool interacted = false;
		System.Action<BaseInteractable, bool> onInteract = (target, success) => { if (target == chest) { interacted = success; UnityEngine.Debug.Log($"World chest probe interaction: success={success}, paused={Assets.Scripts.Utility.MyTime.paused}"); } };
		Assets.Scripts.Inventory__Items__Pickups.Interactables.DetectInteractables.A_Interacted += onInteract;
		yield return new WaitForSecondsRealtime(1f);
		UnityEngine.Debug.Log($"World chest probe ready: source={sources[0].name}, position={copy.transform.position}, prompt={player.playerInput.detectInteractables.t_interact.text}, price={price}, gold={player.inventory.goldInt}, eligible={chest.CanInteract()}");
		yield return new WaitForSecondsRealtime(35f);
		UnityEngine.Debug.Log($"World chest probe result: interacted={interacted}, destroyed={(copy == null)}, clover={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}, gold={player.inventory.goldInt}, paused={Assets.Scripts.Utility.MyTime.paused}");
		Assets.Scripts.Inventory__Items__Pickups.Interactables.DetectInteractables.A_Interacted -= onInteract;
		if (copy != null) Destroy(copy);
		pool.Clear(); pool.AddRange(oldPool); config.skip_chest_animation = oldSkip;
		for (int i = 0; i < names.Length; i++) stats[names[i]].value = savedStats[i];
	}

	private IEnumerator ProbeChestWindow(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var pool = Assets.Scripts.Inventory__Items__Pickups.RunUnlockables.availableItems[Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Common];
		var oldPool = new List<ItemData>(pool);
		var stats = SaveManager.Instance.stats.stats;
		float oldChests = stats["chestsOpened"].value, oldItems = stats["itemsPickedUp"].value;
		pool.Clear(); pool.Add(DataManager.Instance.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover));
		stats["chestsOpened"].value = 0;
		var encounter = UiManager.Instance.encounterWindows;
		var chest = (ChestWindowUi)encounter.chestWindow;
		var controls = SaveManager.Instance.config.cfControlSettings;
		int oldHorizontalNavigation = controls.horizontal_navigation;
		controls.horizontal_navigation = 0;
		var eventSystem = UnityEngine.EventSystems.EventSystem.current;
		var inputModule = eventSystem != null ? eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() : null;
		int oldBanishes = player.inventory.banishes, oldBanishUsed = player.inventory.banishesUsed;
		float oldEarned = stats["goldEarned"].value, oldSpent = stats["goldSpent"].value;
		if (androidChestBanishProbe) { player.inventory.banishes = 1; player.inventory.banishesUsed = 1; player.inventory.ChangeGold(-player.inventory.goldInt); }
		encounter.AddEncounter(Assets.Scripts.UI.InGame.Rewards.EEncounter.ChestNormal);
		yield return new WaitForSecondsRealtime(0.6f);
		UnityEngine.Debug.Log($"Chest window probe opened: active={chest.gameObject.activeInHierarchy}, openButton={chest.b_open.gameObject.activeInHierarchy}, paused={Assets.Scripts.Utility.MyTime.paused}");
		UnityEngine.Debug.Log($"Chest window probe navigation before opening: axis={(inputModule != null ? inputModule.horizontalAxis : "<missing>")}");
		if (androidChestControllerProbe)
		{
			var selected = Assets.Scripts.Managers.ButtonManager.GetCurrentButton();
			UnityEngine.Debug.Log($"Chest window probe open focus: current={(selected != null ? selected.name : "<none>")}, open={chest.b_open.name}, matches={selected == chest.b_open}");
			UnityEngine.Debug.Log("Chest window probe: waiting for controller open");
			float deadline = Time.realtimeSinceStartup + 30f;
			while (chest.b_open.gameObject.activeInHierarchy && Time.realtimeSinceStartup < deadline) yield return null;
			if (chest.b_open.gameObject.activeInHierarchy)
			{
				UnityEngine.Debug.Log("Chest window probe: controller open timed out");
				controls.horizontal_navigation = oldHorizontalNavigation;
				pool.Clear(); pool.AddRange(oldPool);
				yield break;
			}
		}
		else chest.OpenButton();
		UnityEngine.Debug.Log($"Chest window probe navigation after opening: axis={(inputModule != null ? inputModule.horizontalAxis : "<missing>")}");
		yield return new WaitForSecondsRealtime(5f);
		UnityEngine.Debug.Log($"Chest window probe reveal: name={chest.t_itemName.text}, description={chest.t_itemDesc.text}, takeButton={chest.b_take.gameObject.activeInHierarchy}, selected={chest.itemData.eItem}");
		var skippedField = typeof(ChestOpening).GetField("skipped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		UnityEngine.Debug.Log($"Chest window probe animation: skipped={(bool)skippedField.GetValue(chest.chestOpening)}");
		UnityEngine.Debug.Log($"Chest window probe reveal state: clover={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}, inProgress={encounter.encounterInProgress}");
		UnityEngine.Debug.Log($"Chest window probe focus: current={(Assets.Scripts.Managers.ButtonManager.GetCurrentButton() != null ? Assets.Scripts.Managers.ButtonManager.GetCurrentButton().name : "<none>")}, take={chest.b_take.name}, matches={Assets.Scripts.Managers.ButtonManager.GetCurrentButton() == chest.b_take}");
		yield return new WaitForSecondsRealtime(2f);
		if (chest.b_take.gameObject.activeInHierarchy && androidChestBanishProbe)
		{
			int price = chest.levelupScreen.GetShopToolPrice(chest.levelupScreen.GetBanishesUsed());
			chest.BanishButton();
			bool rejected = encounter.encounterInProgress && pool.Contains(chest.itemData);
			player.inventory.ChangeGold(price);
			chest.BanishButton();
			UnityEngine.Debug.Log($"Chest window probe banish: rejected={rejected}, cost={price}, gold={player.inventory.goldInt}, banishes={player.inventory.banishes}, used={player.inventory.banishesUsed}, removed={!pool.Contains(chest.itemData)}, banished={Assets.Scripts.Inventory__Items__Pickups.RunUnlockables.banishedItems.Contains(chest.itemData)}, inProgress={encounter.encounterInProgress}, itemAmount={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}");
			Assets.Scripts.Inventory__Items__Pickups.RunUnlockables.banishedItems.Remove(chest.itemData);
		}
		else if (chest.b_take.gameObject.activeInHierarchy && androidChestControllerProbe)
		{
			UnityEngine.Debug.Log("Chest window probe: waiting for controller take");
			float deadline = Time.realtimeSinceStartup + 30f;
			while (encounter.encounterInProgress && Time.realtimeSinceStartup < deadline) yield return null;
		}
		else if (chest.b_take.gameObject.activeInHierarchy) chest.TakeButton();
		UnityEngine.Debug.Log($"Chest window probe taken: clover={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}, active={chest.gameObject.activeInHierarchy}, paused={Assets.Scripts.Utility.MyTime.paused}, inProgress={encounter.encounterInProgress}");
		UnityEngine.Debug.Log($"Chest window probe navigation after closing: axis={(inputModule != null ? inputModule.horizontalAxis : "<missing>")}");
		controls.horizontal_navigation = oldHorizontalNavigation;
		pool.Clear(); pool.AddRange(oldPool);
		stats["chestsOpened"].value = oldChests;
		stats["itemsPickedUp"].value = oldItems;
		stats["goldEarned"].value = oldEarned; stats["goldSpent"].value = oldSpent;
		player.inventory.banishes = oldBanishes; player.inventory.banishesUsed = oldBanishUsed;
	}

	private IEnumerator ProbeProgress(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var save = SaveManager.Instance.progression;
		var oldMeta = save.menuMeta;
		int oldTier = MapController.runConfig.mapTierIndex;
		float oldRunTimer = Assets.Scripts.Utility.MyTime.runTimer;
		var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
		var tracker = typeof(Assets.Scripts.Saves___Serialization.Progression.Challenges.ChallengesTracker);
		var victoryField = tracker.GetField("victory", flags);
		bool oldVictory = (bool)victoryField.GetValue(null);
		string path = System.IO.Path.Combine(Application.temporaryCachePath, "megabonk_progress_probe.json");
		try
		{
			save.menuMeta = new Assets.Scripts.Saves___Serialization.Progression.MenuMeta();
			var map = MapController.currentMap.eMap;
			int tier = MapController.GetStageIndex();
			save.menuMeta.SetTier(map, tier);
			tracker.GetMethod("OnNewRunStarted", flags).Invoke(null, null);
			MapController.runConfig.mapTierIndex = tier + 1;
			tracker.GetMethod("OnStagebossDefeated", flags).Invoke(null, null);
			bool premature = (bool)victoryField.GetValue(null);
			MapController.runConfig.mapTierIndex = tier;
			Assets.Scripts.Utility.MyTime.runTimer = 250f;
			tracker.GetMethod("OnStagebossDefeated", flags).Invoke(null, null);
			tracker.GetMethod("OnGameOver", flags).Invoke(null, null);
			var progress = save.menuMeta.GetMapProgress(map);
			progress.SetKills(9);
			progress.SetKills(3);
			progress.SetCompletedTime(300f);
			progress.SetCompletedTime(220f);
			string json = Newtonsoft.Json.JsonConvert.SerializeObject(save.menuMeta);
			System.IO.File.WriteAllText(path, json);
			var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<Assets.Scripts.Saves___Serialization.Progression.MenuMeta>(System.IO.File.ReadAllText(path));
			var copy = restored.GetMapProgress(map);
			var character = new Assets.Scripts.Saves___Serialization.Progression.CharacterProgression();
			character.OnRunFinished(50);
			UnityEngine.Debug.Log($"Progress probe: premature={premature}, completed={copy.IsTierCompleted(tier)}, highest={restored.GetHighestCompletedTier(map)}, runs={copy.GetNumTierRuns(tier)}, character={copy.GetTierCompletionCharacters(tier).Contains(player.character)}, nextNotification={copy.tierNotifications.Contains(tier + 1)}, score={copy.tierHighscores[tier]}, time={copy.tierFastestTimes[tier]}, charXp={character.xp}, charRuns={character.numRuns}");
		}
		finally
		{
			save.menuMeta = oldMeta;
			MapController.runConfig.mapTierIndex = oldTier;
			Assets.Scripts.Utility.MyTime.runTimer = oldRunTimer;
			victoryField.SetValue(null, oldVictory);
			if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
		}
		yield break;
	}

	private IEnumerator ProbeVictory(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		DeathScreen screen = UiManager.Instance.deathScreen;
		if (screen == null || screen.victoryScreen == null) { UnityEngine.Debug.Log("Victory probe: missing death screen or victory panel"); yield break; }
		player.inventory.playerHealth.hp = 10000;
		screen.gameObject.SetActive(true);
		int oldTier = MapController.runConfig.mapTierIndex;
		var field = typeof(DeathScreen).GetField("tierVictory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		MapController.runConfig.mapTierIndex = MapController.GetStageIndex() + 1;
		InteractableBossSpawner.A_BossDefeated?.Invoke(true);
		bool early = (bool)field.GetValue(screen);
		MapController.runConfig.mapTierIndex = MapController.GetStageIndex();
		InteractableBossSpawner.A_BossDefeated?.Invoke(false);
		bool completed = (bool)field.GetValue(screen);
		if (screen.deadUiWindow != null) screen.deadUiWindow.SetActive(true);
		screen.ShowStats();
		yield return new WaitForSecondsRealtime(2f);
		VictoryScreen panel = screen.victoryScreen.GetComponentInChildren<VictoryScreen>(true);
		UnityEngine.Debug.Log($"Victory probe: early={early}, completed={completed}, visible={screen.victoryScreen.activeInHierarchy}, content={(panel != null && panel.content != null && panel.content.activeInHierarchy)}, button={(panel != null && panel.btnContinue != null && panel.btnContinue.activeInHierarchy)}, title={(panel != null && panel.itemNameText != null ? panel.itemNameText.text : "missing")}");
		screen.HideVictoryScreen();
		UnityEngine.Debug.Log($"Victory probe close: victory={screen.victoryScreen.activeSelf}, stats={(screen.statsWindow != null && screen.statsWindow.activeSelf)}");
		MapController.runConfig.mapTierIndex = oldTier;
	}

	private IEnumerator ProbeTimeline(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var controller = EnemyManager.Instance.summonerController;
		var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
		var baseType = typeof(Assets.Scripts.Game.Spawning.New.BaseSummoner);
		var stageType = typeof(Assets.Scripts.Game.Spawning.New.Summoners.StageSummoner);
		var controllerType = typeof(Assets.Scripts.Game.Spawning.New.SummonerController);
		var enemiesField = baseType.GetField("currentEnemies", flags);
		var poolField = stageType.GetField("enemyPool", flags);
		var swarmField = controllerType.GetField("swarmSummoner", flags);
		var finalField = controllerType.GetField("finalSwarmSummoner", flags);
		var idField = baseType.GetField("id", flags);
		var stage = controller.stageSummoner;
		var enemies = (System.Collections.Generic.List<Actors.Enemies.EEnemy>)enemiesField.GetValue(stage);
		var pool = (System.Collections.Generic.List<Actors.Enemies.EEnemy>)poolField.GetValue(stage);
		int originalHp = player.inventory.playerHealth.hp;
		player.inventory.playerHealth.hp = 10000;
		int minibossEvents = 0, swarmEvents = 0, finalEvents = 0;
		System.Action<string> logAlert = kind => {
			var alert = UiManager.Instance.alertUi;
			UnityEngine.Debug.Log($"Timeline probe alert {kind}: active={(alert != null && alert.t_alert != null && alert.t_alert.gameObject.activeInHierarchy)}, text={(alert != null && alert.t_alert != null ? alert.t_alert.text : "<missing>")}, clip={(alert != null && alert.audio != null && alert.audio.clip != null ? alert.audio.clip.name : "<missing>")}");
		};
		System.Action mini = () => minibossEvents++;
		System.Action swarm = () => { swarmEvents++; logAlert("swarm"); };
		System.Action final = () => { finalEvents++; logAlert("final"); };
		Assets.Scripts.Game.Spawning.New.SummonerController.A_MiniBoss += mini;
		Assets.Scripts.Game.Spawning.New.SummonerController.A_SwarmStarted += swarm;
		Assets.Scripts.Game.Spawning.New.SummonerController.A_FinalSwarmStarted += final;
		UnityEngine.Debug.Log($"Timeline probe initial: stage={MapController.currentStage.name}, pool={pool.Count}, cards={string.Join(",", enemies)}");
		Assets.Scripts.Utility.MyTime.stageTimer = 61f;
		yield return new WaitForSecondsRealtime(1f);
		UnityEngine.Debug.Log($"Timeline probe card: pool={pool.Count}, cards={string.Join(",", enemies)}");
		Assets.Scripts.Utility.MyTime.stageTimer = 181f;
		yield return new WaitForSecondsRealtime(1f);
		UnityEngine.Debug.Log($"Timeline probe miniboss: events={minibossEvents}, count={controller.minibossCount}");
		Assets.Scripts.Utility.MyTime.stageTimer = 241f;
		yield return new WaitForSecondsRealtime(3f);
		var swarmSummoner = (Assets.Scripts.Game.Spawning.New.BaseSummoner)swarmField.GetValue(controller);
		int swarmId = swarmSummoner == null ? -1 : (int)idField.GetValue(swarmSummoner);
		UnityEngine.Debug.Log($"Timeline probe swarm: events={swarmEvents}, active={swarmSummoner != null}, spawned={EnemyManager.Instance.GetEnemiesFromSummoner(swarmId)}, stageSlow={stage.slowmode}");
		Assets.Scripts.Utility.MyTime.stageTimer = controller.GetStageTimeMax();
		yield return new WaitForSecondsRealtime(3f);
		var finalSummoner = (Assets.Scripts.Game.Spawning.New.BaseSummoner)finalField.GetValue(controller);
		int finalId = finalSummoner == null ? -1 : (int)idField.GetValue(finalSummoner);
		UnityEngine.Debug.Log($"Timeline probe final: events={finalEvents}, active={controller.IsFinalSwarm()}, summoner={finalSummoner != null}, spawned={EnemyManager.Instance.GetEnemiesFromSummoner(finalId)}, playing={GameManager.Instance.IsPlaying()}");
		Assets.Scripts.Utility.MyTime.finalSwarmTimer = 61f;
		yield return new WaitForSecondsRealtime(2f);
		var tierField = typeof(Assets.Scripts.Game.Spawning.New.Summoners.SwarmSummoner).GetField("ghostTier", flags);
		UnityEngine.Debug.Log($"Timeline probe tier: tier={(finalSummoner == null ? -1 : (int)tierField.GetValue(finalSummoner))}");
		var silverUi = FindObjectOfType<FinalSwarmSilverUi>(true);
		bool hasSwarmModifier = player.inventory.statInventory.movingStats.TryGetValue("Swarm Silver Multiplier", out var swarmModifier);
		UnityEngine.Debug.Log($"Timeline probe silver: active={(silverUi != null && silverUi.contentParent.activeInHierarchy)}, timer={Assets.Scripts.Utility.MyTime.finalSwarmTimer:F2}, modifier={(hasSwarmModifier ? swarmModifier.modification : -1f)}, silver={player.inventory.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.SilverIncreaseMultiplier)}, label={(silverUi != null ? silverUi.t_silverMultiplier.text : "<missing>")}");
		var alertUi = UiManager.Instance.alertUi;
		if (alertUi != null) {
			alertUi.SetAlertBoss(); logAlert("boss");
			alertUi.SetAlertTimesUp(); logAlert("time-up");
			yield return new WaitForSecondsRealtime(5f);
			logAlert("expired");
		}
		Assets.Scripts.Game.Spawning.New.SummonerController.A_MiniBoss -= mini;
		Assets.Scripts.Game.Spawning.New.SummonerController.A_SwarmStarted -= swarm;
		Assets.Scripts.Game.Spawning.New.SummonerController.A_FinalSwarmStarted -= final;
		player.inventory.playerHealth.hp = originalHp;
	}

	private IEnumerator ProbeStageBoss(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var altar = FindObjectOfType<InteractableBossSpawner>();
		if (altar == null) { UnityEngine.Debug.Log("Boss probe: altar missing"); yield break; }
		int oldCurses = GameManager.Instance.bossCurses;
		GameManager.Instance.bossCurses = 1;
		MapController.runConfig.mapTierIndex = 1;
		int spawnedEvents = 0, defeatedEvents = 0, defeatedCount = 0;
		bool openedPortal = false;
		System.Action onSpawn = () => spawnedEvents++;
		System.Action<bool> onDefeat = open => { defeatedEvents++; openedPortal = open; };
		System.Action<int> onCount = n => defeatedCount = n;
		InteractableBossSpawner.A_BossSpawned += onSpawn;
		InteractableBossSpawner.A_BossDefeated += onDefeat;
		InteractableBossSpawner.A_NumBossesDefeated += onCount;
		bool activated = altar.Interact();
		var field = typeof(InteractableBossSpawner).GetField("bossEnemies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var bosses = (System.Collections.Generic.List<Assets.Scripts.Actors.Enemies.Enemy>)field.GetValue(altar);
		if (!activated || bosses == null || bosses.Count != 2)
		{
			UnityEngine.Debug.Log($"Boss probe: activation={activated}, bossCount={(bosses == null ? -1 : bosses.Count)}");
			InteractableBossSpawner.A_BossSpawned -= onSpawn; InteractableBossSpawner.A_BossDefeated -= onDefeat; InteractableBossSpawner.A_NumBossesDefeated -= onCount;
			GameManager.Instance.bossCurses = oldCurses;
			yield break;
		}
		var first = bosses[0];
		var second = bosses[1];
		float hp = first.hp;
		first.EnemyDied(new Assets.Scripts.Actors.DamageContainer(0f, "boss probe"));
		first.ReleaseToPool();
		bool prematurePortal = altar.portal.activeSelf;
		second.EnemyDied(new Assets.Scripts.Actors.DamageContainer(0f, "boss probe"));
		second.ReleaseToPool();
		bool stageBossDead = EnemyManager.Instance.stageBossIsDead;
		bool portalActive = altar.portal.activeSelf;
		float remaining = EnemyManager.Instance.summonerController.GetStageTimeMax() - Assets.Scripts.Utility.MyTime.stageTimer;
		var portal = altar.portal.GetComponentInChildren<InteractablePortal>(true);
		androidBossInventory = player.inventory;
		UnityEngine.Debug.Log($"Boss probe: activated={activated}, spawned={spawnedEvents}, bossCount=2, bossHp={hp:F1}, prematurePortal={prematurePortal}, defeated={defeatedEvents}, count={defeatedCount}, opensPortal={openedPortal}, portalActive={portalActive}, stageBossDead={stageBossDead}, remaining={remaining:F1}, portalComponent={portal != null}");
		InteractableBossSpawner.A_BossSpawned -= onSpawn;
		InteractableBossSpawner.A_BossDefeated -= onDefeat;
		InteractableBossSpawner.A_NumBossesDefeated -= onCount;
		GameManager.Instance.bossCurses = oldCurses;
		yield return new WaitForSecondsRealtime(0.2f);
		if (portal != null && portalActive) UnityEngine.Debug.Log($"Boss probe: enterPortal={portal.Interact()}, stageBefore={MapController.GetStageIndex()}");
	}

	private IEnumerator ProbeStatusEffects(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance;
		bool waves = manager.enabledWaves;
		manager.enabledWaves = false;
		var health = player.inventory.playerHealth;
		int oldHp = health.hp;
		float oldShield = health.shield;
		health.hp = 2000;
		health.shield = 0f;
		var status = player.inventory.statusEffects;
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Skeleton);
		var enemy = manager.SpawnEnemy(data, player.transform.position + Vector3.right * 20f, -1, true, canBeElite: false);
		if (enemy == null) { UnityEngine.Debug.Log("Status probe: spawn failed"); health.hp = oldHp; health.shield = oldShield; manager.enabledWaves = waves; yield break; }
		status.TestPickup(Assets.Scripts.Inventory__Items__Pickups.Pickups.EPickup.Time);
		bool active = status.HasStatusEffect(Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.TimeFreeze);
		bool frozenExisting = enemy.rb.isKinematic;
		float duration = status.statusEffects[Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.TimeFreeze].expirationTime - Assets.Scripts.Utility.MyTime.time;
		var spawnedFrozen = manager.SpawnEnemy(data, player.transform.position + Vector3.left * 20f, -1, true, canBeElite: false);
		bool frozenNew = spawnedFrozen != null && spawnedFrozen.rb.isKinematic;
		status.statusEffects[Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.TimeFreeze].expirationTime = Assets.Scripts.Utility.MyTime.time + 0.2f;
		yield return new WaitForSecondsRealtime(0.8f);
		bool unfrozen = !status.HasStatusEffect(Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.TimeFreeze) && !enemy.rb.isKinematic && !spawnedFrozen.rb.isKinematic;
		float speedBefore = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.MoveSpeedMultiplier);
		status.TestPickup(Assets.Scripts.Inventory__Items__Pickups.Pickups.EPickup.Haste);
		yield return new WaitForSecondsRealtime(0.3f);
		float speedDuring = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.MoveSpeedMultiplier);
		status.statusEffects[Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.Haste].expirationTime = Assets.Scripts.Utility.MyTime.time + 0.2f;
		yield return new WaitForSecondsRealtime(0.8f);
		float speedAfter = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.MoveSpeedMultiplier);
		int poisonTicks = 0;
		System.Action<Assets.Scripts.Inventory__Items__Pickups.PlayerHealth, Assets.Scripts.Actors.DamageContainer, bool> onDamage = (h, dc, shield) => { if (h == health && dc.damageSource == "Poison") poisonTicks++; };
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += onDamage;
		status.PoisonPlayer(2f);
		yield return new WaitForSecondsRealtime(1.4f);
		bool poisoned = status.HasStatusEffect(Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.Poison);
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= onDamage;
		status.statusEffects[Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.Poison].expirationTime = Assets.Scripts.Utility.MyTime.time;
		status.Tick();
		UnityEngine.Debug.Log($"Status probe: freezeActive={active}, duration={duration:F1}, existingFrozen={frozenExisting}, spawnedFrozen={frozenNew}, resumed={unfrozen}, haste={speedBefore:F2}/{speedDuring:F2}/{speedAfter:F2}, poisonActive={poisoned}, poisonTicks={poisonTicks}");
		health.hp = oldHp;
		health.shield = oldShield;
		if (enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
		if (spawnedFrozen != null && spawnedFrozen.gameObject.activeInHierarchy) spawnedFrozen.ReleaseToPool();
		manager.enabledWaves = waves;
	}

	private IEnumerator ProbeCactus(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance;
		bool waves = manager.enabledWaves;
		manager.enabledWaves = false;
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.CactusShooter);
		var enemy = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 10f, -1, true, canBeElite: false);
		if (enemy == null) { UnityEngine.Debug.Log("Cactus probe: spawn failed"); manager.enabledWaves = waves; yield break; }
		enemy.hp = 10000f;
		var health = player.inventory.playerHealth;
		int oldHp = health.hp;
		float oldShield = health.shield;
		var playerBody = player.playerMovement.GetRb();
		var oldConstraints = playerBody.constraints;
		playerBody.constraints = RigidbodyConstraints.FreezeAll;
		int playerLayer = LayerMask.NameToLayer("Player"), enemyLayer = LayerMask.NameToLayer("Enemy");
		bool oldCollisionIgnore = Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer);
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
		health.hp = 2000;
		health.shield = 0f;
		int attributableHits = 0;
		System.Action<Assets.Scripts.Inventory__Items__Pickups.PlayerHealth, Assets.Scripts.Actors.DamageContainer, bool> onDamage = (h, dc, shield) => { if (h == health && dc.enemy == enemy) attributableHits++; };
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += onDamage;
		var controllerField = typeof(Assets.Scripts.Actors.Enemies.Enemy).GetField("specialAttackController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var controller = (Assets.Scripts.Game.Combat.EnemySpecialAttacks.SpecialAttackController)controllerField.GetValue(enemy);
		controller.GetType().GetField("nextCheckTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(controller, 0f);
		enemy.enemyMovement.distanceToTarget = 10f;
		controller.Tick();
		var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance;
		var projectile = FindObjectsOfType<EnemySpecialAttackCactusProjectile>().First(a => a.specialAttack.attackName == "CactusProjectile");
		int warningStart = pools.warningSpherePool.CountActive;
		float startY = enemy.GetCenterPosition().y;
		yield return new WaitForSecondsRealtime(0.45f);
		float projectileY = projectile.transform.position.y;
		yield return new WaitForSecondsRealtime(2f);
		UnityEngine.Debug.Log($"Cactus probe: warningStart={warningStart}, arcHeightAboveStart={projectileY - startY:F2}, hitsFromCactus={attributableHits}, hpAfter={health.hp}, warningAfter={pools.warningSpherePool.CountActive}, projectileAfter={pools.enemyAttacksPools["CactusProjectile"].CountActive}, resumed={!enemy.rb.isKinematic}");
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= onDamage;
		playerBody.constraints = oldConstraints;
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, oldCollisionIgnore);
		health.hp = oldHp;
		health.shield = oldShield;
		if (enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
		manager.enabledWaves = waves;
	}

	private IEnumerator ProbeHitscan(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance;
		bool waves = manager.enabledWaves;
		manager.enabledWaves = false;
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Bandit);
		var enemy = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 10f, -1, true, canBeElite: false);
		if (enemy == null) { UnityEngine.Debug.Log("Hitscan probe: spawn failed"); manager.enabledWaves = waves; yield break; }
		var health = player.inventory.playerHealth;
		int oldHp = health.hp;
		float oldShield = health.shield;
		var playerBody = player.playerMovement.GetRb();
		var oldConstraints = playerBody.constraints;
		playerBody.constraints = RigidbodyConstraints.FreezeAll;
		int playerLayer = LayerMask.NameToLayer("Player"), enemyLayer = LayerMask.NameToLayer("Enemy");
		bool oldCollisionIgnore = Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer);
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
		health.hp = 2000;
		health.shield = 0f;
		int hits = 0;
		System.Action<Assets.Scripts.Inventory__Items__Pickups.PlayerHealth, Assets.Scripts.Actors.DamageContainer, bool> onDamage = (h, dc, shield) => { if (h == health && dc.damageSource == "Enemy") hits++; };
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += onDamage;
		var controllerField = typeof(Assets.Scripts.Actors.Enemies.Enemy).GetField("specialAttackController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var controller = (Assets.Scripts.Game.Combat.EnemySpecialAttacks.SpecialAttackController)controllerField.GetValue(enemy);
		controller.GetType().GetField("nextCheckTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(controller, 0f);
		enemy.enemyMovement.distanceToTarget = 10f;
		controller.Tick();
		var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance;
		bool startedIdle = enemy.rb.isKinematic;
		int initialWarnings = pools.warningTubePool.CountActive;
		int initialAttacks = pools.enemyAttacksPools["BanditAttack"].CountActive;
		var activeAttack = FindObjectsOfType<EnemySpecialAttackHitscanMultiple>().First(a => a.specialAttack.attackName == "BanditAttack");
		var countField = typeof(EnemySpecialAttackHitscanMultiple).GetField("numSpawned", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		yield return new WaitForSecondsRealtime(4f);
		UnityEngine.Debug.Log($"Hitscan probe: startedIdle={startedIdle}, warningStart={initialWarnings}, attackStart={initialAttacks}, shots={countField.GetValue(activeAttack)}, hits={hits}, hpAfter={health.hp}, warningsAfter={pools.warningTubePool.CountActive}, attacksAfter={pools.enemyAttacksPools["BanditAttack"].CountActive}, resumed={!enemy.rb.isKinematic}");
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= onDamage;
		playerBody.constraints = oldConstraints;
		Physics.IgnoreLayerCollision(playerLayer, enemyLayer, oldCollisionIgnore);
		health.hp = oldHp;
		health.shield = oldShield;
		if (enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
		manager.enabledWaves = waves;
	}

	private IEnumerator ProbeSpecialAttack(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance;
		bool waves = manager.enabledWaves;
		manager.enabledWaves = false;
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.MinibossGolem);
		var enemy = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 10f, -1, true, canBeElite: false);
		if (enemy == null) { UnityEngine.Debug.Log("Special attack probe: spawn failed"); manager.enabledWaves = waves; yield break; }
		float spawnDeadline = Time.realtimeSinceStartup + 20f;
		while (enemy != null && enemy.IsTeleporting() && Time.realtimeSinceStartup < spawnDeadline) yield return null;
		if (enemy == null || enemy.IsTeleporting()) { UnityEngine.Debug.Log("Special attack probe: teleport did not finish"); manager.enabledWaves = waves; yield break; }
		var health = player.inventory.playerHealth;
		int oldHp = health.hp;
		float oldShield = health.shield;
		health.hp = health.maxHp;
		health.shield = 0f;
		int initialHp = health.hp;
		var controllerField = typeof(Assets.Scripts.Actors.Enemies.Enemy).GetField("specialAttackController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var controller = controllerField?.GetValue(enemy) as Assets.Scripts.Game.Combat.EnemySpecialAttacks.SpecialAttackController;
		if (controller == null) { UnityEngine.Debug.Log("Special attack probe: controller unavailable"); manager.enabledWaves = waves; yield break; }
		var attacksField = controller.GetType().GetField("attacks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var available = attacksField?.GetValue(controller) as System.Collections.Generic.HashSet<Assets.Scripts.Game.Combat.EnemySpecialAttacks.EnemySpecialAttack>;
		if (available == null) { UnityEngine.Debug.Log("Special attack probe: attack list unavailable"); manager.enabledWaves = waves; yield break; }
		available.RemoveWhere(a => a.attackName != "StoneAttackClose");
		var checkField = controller.GetType().GetField("nextCheckTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		checkField.SetValue(controller, 0f);
		enemy.enemyMovement.distanceToTarget = 10f;
		controller.Tick();
		if (Assets.Scripts.Objects.Pooling.PoolManager.Instance.enemyAttacksPools == null || !Assets.Scripts.Objects.Pooling.PoolManager.Instance.enemyAttacksPools.ContainsKey("StoneAttackClose"))
		{ UnityEngine.Debug.Log("Special attack probe: target attack did not start"); manager.enabledWaves = waves; yield break; }
		bool idle = enemy.state == Assets.Scripts.Actors.Enemies.EEnemyState.Idle && enemy.rb.isKinematic;
		int warningStart = Assets.Scripts.Objects.Pooling.PoolManager.Instance.warningSpherePool.CountActive;
		int activeAttack = Assets.Scripts.Objects.Pooling.PoolManager.Instance.enemyAttacksPools["StoneAttackClose"].CountActive;
		yield return new WaitForSecondsRealtime(1.4f);
		int hpAfter = health.hp;
		int warningAfter = Assets.Scripts.Objects.Pooling.PoolManager.Instance.warningSpherePool.CountActive;
		yield return new WaitForSecondsRealtime(2.5f);
		int poolAfter = Assets.Scripts.Objects.Pooling.PoolManager.Instance.enemyAttacksPools["StoneAttackClose"].CountActive;
		UnityEngine.Debug.Log($"Special attack probe: startedIdle={idle}, warningStart={warningStart}, activeAttack={activeAttack}, hpBefore={initialHp}, hpAfter={hpAfter}, warningAfter={warningAfter}, poolAfter={poolAfter}, resumed={enemy.state == Assets.Scripts.Actors.Enemies.EEnemyState.Default && !enemy.rb.isKinematic}");
		health.hp = oldHp;
		health.shield = oldShield;
		if (enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
		manager.enabledWaves = waves;
	}

	private IEnumerator ProbeKnockback(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var manager = EnemyManager.Instance;
		bool waves = manager.enabledWaves;
		manager.enabledWaves = false;
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Skeleton);
		var enemy = manager.SpawnEnemy(data, player.transform.position + Vector3.right * 25f, -1, true, canBeElite: false);
		if (enemy == null) { UnityEngine.Debug.Log("Knockback probe: spawn failed"); manager.enabledWaves = waves; yield break; }
		var movement = enemy.enemyMovement;
		var field = typeof(Actors.Enemies.EnemyMovementRb).GetField("knockbackVelocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var dc = new Assets.Scripts.Actors.DamageContainer(0f, "knockback probe") { direction = Vector3.right, knockback = 6f };
		float resistance = Assets.Scripts.Inventory.Stats.EnemyStats.GetKnockbackResistance(enemy);
		movement.Knockback(dc);
		Vector3 direct = (Vector3)field.GetValue(movement);
		field.SetValue(movement, Vector3.zero);
		enemy.rb.isKinematic = true;
		movement.Knockback(dc);
		Vector3 kinematic = (Vector3)field.GetValue(movement);
		enemy.rb.isKinematic = false;
		movement.KnockUp(3f);
		float knockUpVelocity = enemy.rb.velocity.y;
		enemy.rb.velocity = Vector3.zero;
		GameObject source = new GameObject("Knockback diagnostic explosion");
		source.transform.position = enemy.transform.position - Vector3.right * 4f;
		var explosion = source.AddComponent<CombatExplosion>();
		explosion.radius = 8f;
		explosion.enemyKnockback = 6f;
		Physics.SyncTransforms();
		yield return null;
		yield return null;
		Vector3 blast = (Vector3)field.GetValue(movement);
		UnityEngine.Debug.Log($"Knockback probe: resistance={resistance:F3}, direct={direct}, kinematic={kinematic}, knockUpY={knockUpVelocity:F3}, explosion={blast}, alive={enemy.gameObject.activeInHierarchy}");
		Destroy(source);
		if (enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
		manager.enabledWaves = waves;
	}

	private IEnumerator ProbeBoomer(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.BoomerSpider);
		var manager = EnemyManager.Instance;
		bool waves = manager.enabledWaves;
		manager.enabledWaves = false;
		var health = player.inventory.playerHealth;
		int savedHp = health.hp;
		float savedShield = health.shield;
		health.hp = health.maxHp;
		health.shield = 0f;
		int hpBefore = health.hp;
		var enemy = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 2f, -1, true, canBeElite: false);
		if (enemy == null) { UnityEngine.Debug.Log("Boomer probe: spawn failed"); health.hp = savedHp; health.shield = savedShield; manager.enabledWaves = waves; yield break; }
		bool released = false;
		System.Action<Assets.Scripts.Actors.Enemies.Enemy> onRelease = e => { if (e == enemy) released = true; };
		Assets.Scripts.Actors.Enemies.Enemy.A_EnemyReleasedFromPool += onRelease;
		enemy.enemyMovement.distanceToTarget = 1f;
		Actors.Enemies.EnemyBehaviour.FixedUpdate(enemy);
		bool started = EffectManager.Instance.currentlyExplodingEnemy.Contains(enemy);
		int warnings = Assets.Scripts.Objects.Pooling.PoolManager.Instance.warningSpherePool.CountActive;
		bool dying = enemy.IsDeadOrDyingNextFrame();
		yield return new WaitForSecondsRealtime(1.5f);
		UnityEngine.Debug.Log($"Boomer probe: started={started}, dying={dying}, warningAtStart={warnings}, warningAfter={Assets.Scripts.Objects.Pooling.PoolManager.Instance.warningSpherePool.CountActive}, released={released}, stillExploding={EffectManager.Instance.currentlyExplodingEnemy.Contains(enemy)}, playerHpBefore={hpBefore}, playerHpAfter={health.hp}, damage={data.damage}, activeExplosions={FindObjectsOfType<CombatExplosion>().Length}");
		Assets.Scripts.Actors.Enemies.Enemy.A_EnemyReleasedFromPool -= onRelease;
		health.hp = savedHp;
		health.shield = savedShield;
		manager.enabledWaves = waves;
	}

	private IEnumerator LogOriginalForestState()
	{
		yield return new WaitForSecondsRealtime(12f);
		if (androidGrassProbe && grassRenderer != null) grassRenderer.LogAndroidDiagnostic();
		if (androidGrassHighlightProbe && grassRenderer != null) grassRenderer.HighlightAndroidDiagnostic();
		if (androidGrassForcedProbe && grassRenderer != null) grassRenderer.PlaceAndroidDiagnosticGrass();
		var player = gameManager.GetPlayer();
		var view = PlayerCamera.Instance;
		if (player == null || view == null)
		{
			UnityEngine.Debug.Log("Original Forest diagnostic: player or camera missing");
			yield break;
		}
		var camera = view.GetComponentInChildren<Camera>(true);
		if (androidShrineProbe) StartCoroutine(ProbeShrines(player));
		if (androidBananarangProbe) StartCoroutine(ProbeBananarang(player));
		if (androidBowProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Bow));
		if (androidAxeProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Axe));
		if (androidMineProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Mine));
		if (androidDiceProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Dice));
		if (androidFlamewalkerProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Flamewalker));
		if (androidBloodMagicProbe) StartCoroutine(ProbeWeapon(player, EWeapon.BloodMagic));
		if (androidWirelessDaggerProbe) StartCoroutine(ProbeWeapon(player, EWeapon.BluetoothDagger));
		if (androidPoisonFlaskProbe) StartCoroutine(ProbeWeapon(player, EWeapon.PoisonFlask));
		if (androidTornadoProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Tornado));
		if (androidDexecutionerProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Dexecutioner));
		if (androidHeroSwordProbe) StartCoroutine(ProbeWeapon(player, EWeapon.HeroSword));
		if (androidCorruptSwordProbe) StartCoroutine(ProbeWeapon(player, EWeapon.CorruptSword));
		if (androidScytheProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Scythe));
		if (androidAuraProbe == 1) StartCoroutine(ProbeWeapon(player, EWeapon.Aura));
		if (androidAuraProbe == 2) StartCoroutine(ProbeWeapon(player, EWeapon.Frostwalker));
		if (androidAuraProbe == 3) StartCoroutine(ProbeWeapon(player, EWeapon.DragonsBreath));
		if (androidKatanaProbe) StartCoroutine(ProbeWeapon(player, EWeapon.Katana));
		if (androidLightningWeaponProbe) StartCoroutine(ProbeWeapon(player, EWeapon.LightningStaff));
		if (androidShotgunProbe) StartCoroutine(ProbeShotgun(player));
		if (androidBossProbe && MapController.GetStageIndex() > 0)
			UnityEngine.Debug.Log($"Boss probe destination: index={MapController.GetStageIndex()}, stage={MapController.currentStage.name}, sameInventory={ReferenceEquals(androidBossInventory, player.inventory)}, inventoryPaused={player.inventory.pause}, stageTimer={Assets.Scripts.Utility.MyTime.stageTimer:F1}, playing={gameManager.IsPlaying()}");
		bool ground = Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 500f);
		UnityEngine.Debug.Log($"Original Forest diagnostic: player={player.transform.position}, active={player.gameObject.activeInHierarchy}, camera={view.transform.position}, rotation={view.transform.eulerAngles}, state={view.GetCameraState()}, cameraEnabled={(camera != null && camera.enabled)}, mask={(camera != null ? camera.cullingMask : 0)}, ground={(ground ? hit.collider.name : "none")}, groundDistance={(ground ? hit.distance : -1f)}, playing={gameManager.IsPlaying()}, generating={isGenerating}");
		UnityEngine.Debug.Log($"Original Forest waves: count={(EnemyManager.Instance != null ? EnemyManager.Instance.numEnemies : -1)}, stageCount={(EnemyManager.Instance != null ? EnemyManager.Instance.GetEnemiesFromSummoner(0) : -1)}");
		UnityEngine.Debug.Log($"Original Forest health: hp={player.inventory.playerHealth.hp}, shield={player.inventory.playerHealth.shield}, overheal={player.inventory.playerHealth.overheal}");
		if (androidChestWindowProbe || androidChestBanishProbe) StartCoroutine(ProbeChestWindow(player));
		if (androidWorldChestProbe) StartCoroutine(ProbeWorldChest(player));
		if (androidMicrowaveProbe) StartCoroutine(ProbeMicrowave(player));
		if (androidEnemyGoldProbe) StartCoroutine(ProbeEnemyGold(player));
		if (androidMoldyProbe) StartCoroutine(ProbeMoldyCheese(player));
		if (androidBorgorProbe) StartCoroutine(ProbeBorgor(player));
		if (androidBurnProbe) StartCoroutine(ProbeBurn(player));
		if (androidFirefieldProbe) StartCoroutine(ProbeFirefield(player));
		if (androidIceProbe) StartCoroutine(ProbeIce(player));
		if (androidFlexProbe) StartCoroutine(ProbeFlex(player));
		if (androidLightningProbe) StartCoroutine(ProbeLightning(player));
		if (androidBoomerProbe) StartCoroutine(ProbeBoomer(player));
		if (androidKnockbackProbe) StartCoroutine(ProbeKnockback(player));
		if (androidSpecialAttackProbe) StartCoroutine(ProbeSpecialAttack(player));
		if (androidHitscanProbe) StartCoroutine(ProbeHitscan(player));
		if (androidCactusProbe) StartCoroutine(ProbeCactus(player));
		if (androidStatusProbe) StartCoroutine(ProbeStatusEffects(player));
		if (androidBossProbe && MapController.GetStageIndex() == 0) StartCoroutine(ProbeStageBoss(player));
		if (androidTimelineProbe) StartCoroutine(ProbeTimeline(player));
		if (androidVictoryProbe) StartCoroutine(ProbeVictory(player));
		if (androidProgressProbe) StartCoroutine(ProbeProgress(player));
		if (androidLifetimeStatsProbe != 0)
		{
			const string key = "android_port_lifetime_test";
			var allStats = SaveManager.Instance.stats.stats;
			if (androidLifetimeStatsProbe == 1)
			{
				int notifications = 0;
				System.Action<string, Assets.Scripts.Saves___Serialization.Progression.Stats.MyStat> callback = (name, stat) => { if (name == key) notifications++; };
				Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.A_StatUpdated += callback;
				Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.SetValueForce(key, 3f);
				Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.AddValue(key, 4f);
				bool rejected = false;
				try { Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.AddValue(key, -1f); } catch (System.Exception) { rejected = true; }
				Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.A_StatUpdated -= callback;
				SaveManager.Instance.SaveStats();
				UnityEngine.Debug.Log($"Lifetime stats probe write: value={allStats[key].value}, notifications={notifications}, rejectedDecrease={rejected}");
			}
			else
			{
				bool present = allStats.TryGetValue(key, out var saved);
				UnityEngine.Debug.Log($"Lifetime stats probe reload: present={present}, value={(present ? saved.value : -1f)}");
				allStats.Remove(key);
				SaveManager.Instance.SaveStats();
			}
		}
		var pools = Assets.Scripts.Inventory__Items__Pickups.RunUnlockables.availableItems;
		if (pools != null)
		{
			var counts = new List<string>();
			foreach (var pool in pools) counts.Add($"{pool.Key}={pool.Value.Count}");
			UnityEngine.Debug.Log($"Original item pools: {string.Join(", ", counts)}");
			if (androidChestPurchaseProbe)
			{
				var stats = SaveManager.Instance.stats.stats;
				string[] names = { "chestsOpened", "chestsBought", "itemsPickedUp", "goldEarned", "goldSpent" };
				var savedStats = new float[names.Length];
				for (int i = 0; i < names.Length; i++) savedStats[i] = stats[names[i]].value;
				var common = pools[Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Common];
				var oldPool = new List<ItemData>(common);
				var config = SaveManager.Instance.config.cfGameSettings;
				int oldSkip = config.skip_chest_animation;
				try
				{
					config.skip_chest_animation = 1;
					common.Clear(); common.Add(DataManager.Instance.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover));
					stats["chestsOpened"].value = 0;
					int price = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice();
					player.inventory.ChangeGold(-player.inventory.goldInt);
					var chest = new GameObject("Chest purchase probe").AddComponent<Assets.Scripts.Inventory__Items__Pickups.Chests.InteractableChest>();
					bool rejected = !chest.Interact();
					player.inventory.ChangeGold(price);
					bool bought = chest.Interact();
					int nextPrice = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice();
					float opened = stats["chestsOpened"].value;
					float paid = stats["chestsBought"].value - savedStats[1];
					stats["chestsOpened"].value = 0;
					var free = new GameObject("Free chest probe").AddComponent<Assets.Scripts.Inventory__Items__Pickups.Chests.InteractableChest>();
					free.chestType = Assets.Scripts.Inventory__Items__Pickups.Interactables.EChest.Free;
					bool freeOpened = free.Interact();
					UnityEngine.Debug.Log($"Original chest purchase probe: rejected={rejected}, bought={bought}, price={price}, gold={player.inventory.goldInt}, nextPrice={nextPrice}, opened={opened}, paidEvents={paid}, freeOpened={freeOpened}, freePriceUnchanged={nextPrice == Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice()}, clover={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}");
				}
				finally
				{
					common.Clear(); common.AddRange(oldPool); config.skip_chest_animation = oldSkip;
					for (int i = 0; i < names.Length; i++) stats[names[i]].value = savedStats[i];
				}
			}
			if (androidChestAwardProbe)
			{
				var common = pools[Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Common];
				var previousPool = new List<ItemData>(common);
				var chestStat = SaveManager.Instance.stats.stats["chestsOpened"];
				float previousChestCount = chestStat.value;
				float previousPickupCount = Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat("itemsPickedUp");
				float previousLuck = player.inventory.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.Luck);
				int mappings = 0;
				foreach (Assets.Scripts.Inventory__Items__Pickups.Interactables.EChest type in Enum.GetValues(typeof(Assets.Scripts.Inventory__Items__Pickups.Interactables.EChest)))
					if (Assets.Scripts.Inventory__Items__Pickups.Interactables.ChestUtility.EncounterToChestType(Assets.Scripts.Inventory__Items__Pickups.Interactables.ChestUtility.ChestTypeToEncounter(type)) == type) mappings++;
				try
				{
					common.Clear();
					common.Add(DataManager.Instance.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover));
					chestStat.value = 0;
					Assets.Scripts.Inventory__Items__Pickups.Interactables.ChestUtility.OpenChestNoAnimation(Assets.Scripts.Inventory__Items__Pickups.Interactables.EChest.Normal);
					player.inventory.playerStats.ForceUpdateStats();
					UnityEngine.Debug.Log($"Original chest award probe: item=Clover, amount={player.inventory.itemInventory.GetAmount(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover)}, luckBefore={previousLuck}, luckAfter={player.inventory.playerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.Luck)}, pickups={Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat("itemsPickedUp") - previousPickupCount}, mappings={mappings}");
					var cloverData = DataManager.Instance.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover);
					UnityEngine.Debug.Log($"Original item display probe: name={cloverData.GetName()}, description={cloverData.GetDescription()}, icon={(cloverData.GetIcon() != null)}, dummy={cloverData.GetDummyItem().GetType().Name}");
				}
				finally
				{
					common.Clear(); common.AddRange(previousPool); chestStat.value = previousChestCount;
					Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.SetValueForce("itemsPickedUp", previousPickupCount);
				}
			}
			if (androidChestSelectionProbe)
			{
				var stat = SaveManager.Instance.stats.stats[Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.chestsOpened.ToString()];
				float originalCount = stat.value;
				var results = new List<string>();
				for (int i = 1; i <= 6; i++)
				{
					stat.value = i;
					var item = Assets.Scripts.Inventory__Items__Pickups.Items.ItemUtility.GetRandomChestItem(Assets.Scripts.Inventory__Items__Pickups.Interactables.EChest.Normal, 0f);
					results.Add($"{i}:{item.eItem}/{item.rarity}");
				}
				stat.value = 7;
				int invalid = 0;
				for (int i = 0; i < 100; i++)
				{
					var item = Assets.Scripts.Inventory__Items__Pickups.Items.ItemUtility.GetRandomChestItem(Assets.Scripts.Inventory__Items__Pickups.Interactables.EChest.Normal, 0.15f);
					if (!pools[item.rarity].Contains(item)) invalid++;
				}
				stat.value = originalCount;
				UnityEngine.Debug.Log($"Original chest selection probe: first={string.Join(",", results)}, sampled=100, outsidePool={invalid}");
			}
		}
		if (androidCoinProbe) StartCoroutine(ProbeGoldCoins(player));
		if (androidPotProbe) StartCoroutine(ProbePot(player));
		if (androidSilverPotProbe) StartCoroutine(ProbeSilverPot(player));
		if (androidLifestealProbe) StartCoroutine(ProbeLifesteal(player));
		if (androidRegenProbe) StartCoroutine(ProbeRegen(player));
		if (androidEvadeProbe) StartCoroutine(ProbeEvade(player));
		if (androidPhantomShroudProbe) StartCoroutine(ProbePhantomShroud(player));
		if (androidCursedDollProbe) StartCoroutine(ProbeCursedDoll(player));
		if (androidBobsLanternProbe) StartCoroutine(ProbeBobsLantern(player));
		if (androidIdleJuiceProbe) StartCoroutine(ProbeIdleJuice(player));
		if (androidBeefyRingProbe) StartCoroutine(ProbeBeefyRing(player));
		if (androidSpicyMeatballProbe) StartCoroutine(ProbeSpicyMeatball(player));
		if (androidBonkerProbe) StartCoroutine(ProbeBonker(player));
		if (androidIceCrystalProbe) StartCoroutine(ProbeIceCrystal(player));
		if (androidInputTipProbe) StartCoroutine(ProbeInputTip(player));
		if (androidGoldenSneakersProbe) StartCoroutine(ProbeGoldenSneakers(player));
		if (androidSoulHarvesterProbe) StartCoroutine(ProbeSoulHarvester(player));
		if (androidBobDeadProbe) StartCoroutine(ProbeBobDead(player));
		if (androidJoesDaggerProbe) StartCoroutine(ProbeJoesDagger(player));
		if (androidFinalFightProbe) AndroidFinalFightProbe.Begin(androidPylonPhysicsProbe);
		if (androidGameplayInputProbe) AndroidGameplayInputProbe.Begin(player);
		if (androidChallengeMenuProbe)
		{
			var stats = player.inventory.playerStats;
			var chestPrice = Assets.Scripts.Menu.Shop.EStat.ChestPriceMultiplier;
			var enemyScaling = Assets.Scripts.Menu.Shop.EStat.EnemyScalingMultiplier;
			var silver = Assets.Scripts.Menu.Shop.EStat.SilverIncreaseMultiplier;
			var challenge = MapController.runConfig?.challenge;
			var counter = FindObjectOfType<KillsAndGoldCounter>();
			UnityEngine.Debug.Log($"Challenge run probe: challenge={challenge?.internalName ?? "<none>"}, turbo={Assets.Scripts.Saves___Serialization.Progression.Challenges.ChallengesTracker.HasChallengeModifier("turbo")}, chestPrice={stats.GetStat(chestPrice)}, enemyScaling={stats.GetStat(enemyScaling)}, silver={stats.GetStat(silver)}, cost={Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice()}, hud={counter?.t_chestPrice?.text}");
		}
		if (androidItemProbe)
		{
			var inventory = player.inventory.itemInventory;
			int before = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetPotMoney(false);
			inventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Pumpkin, 2);
			var item = inventory.GetItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Pumpkin) as Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations.ItemPumpkin;
			UnityEngine.Debug.Log($"Android item probe: type={item?.GetType().Name}, amount={item?.amount}, multiplier={item?.GetRewardMultiplier()}, potBefore={before}, potAfter={Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetPotMoney(false)}");
		}
		if (androidItemStatsProbe)
		{
			var inv = player.inventory;
			var stats = inv.playerStats;
			var damage = Assets.Scripts.Menu.Shop.EStat.DamageMultiplier;
			var health = Assets.Scripts.Menu.Shop.EStat.MaxHealth;
			var projectiles = Assets.Scripts.Menu.Shop.EStat.Projectiles;
			var luck = Assets.Scripts.Menu.Shop.EStat.Luck;
			UnityEngine.Debug.Log($"Android item stats probe before: damage={stats.GetStat(damage)}, maxHealth={stats.GetStat(health)}, healthCap={inv.playerHealth.maxHp}, projectiles={stats.GetStat(projectiles)}, luck={stats.GetStat(luck)}");
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Beer, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Backpack, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Clover, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.GymSauce, 2);
			stats.ForceUpdateStats();
			UnityEngine.Debug.Log($"Android item stats probe after: damage={stats.GetStat(damage)}, maxHealth={stats.GetStat(health)}, healthCap={inv.playerHealth.maxHp}, projectiles={stats.GetStat(projectiles)}, luck={stats.GetStat(luck)}");
		}
		if (androidMoreItemStatsProbe)
		{
			var inv = player.inventory;
			var stats = inv.playerStats;
			var speed = Assets.Scripts.Menu.Shop.EStat.MoveSpeedMultiplier;
			var attack = Assets.Scripts.Menu.Shop.EStat.AttackSpeed;
			var hp = Assets.Scripts.Menu.Shop.EStat.MaxHealth;
			var evasion = Assets.Scripts.Menu.Shop.EStat.Evasion;
			var crit = Assets.Scripts.Menu.Shop.EStat.CritChance;
			var lifesteal = Assets.Scripts.Menu.Shop.EStat.Lifesteal;
			UnityEngine.Debug.Log($"Android more item stats before: speed={stats.GetStat(speed)}, attack={stats.GetStat(attack)}, maxHealth={stats.GetStat(hp)}, health={inv.playerHealth.hp}, evasionRaw={stats.GetRawStat(evasion)}, crit={stats.GetStat(crit)}, lifesteal={stats.GetStat(lifesteal)}, maxOverheal={inv.playerHealth.maxOverheal}");
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.TurboSocks, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Battery, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Oats, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SlipperyRing, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.ForbiddenJuice, 2);
			inv.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Chonkplate, 2);
			stats.ForceUpdateStats();
			UnityEngine.Debug.Log($"Android more item stats after: speed={stats.GetStat(speed)}, attack={stats.GetStat(attack)}, maxHealth={stats.GetStat(hp)}, health={inv.playerHealth.hp}, evasionRaw={stats.GetRawStat(evasion)}, crit={stats.GetStat(crit)}, lifesteal={stats.GetStat(lifesteal)}, maxOverheal={inv.playerHealth.maxOverheal}");
		}
		if (androidInteractionProbe)
		{
			var probe = new GameObject("Android interaction probe");
			probe.tag = "Interactable";
			probe.layer = LayerMask.NameToLayer("Object");
			probe.transform.position = player.transform.position + player.transform.forward * 1.5f;
			probe.AddComponent<SphereCollider>();
			probe.AddComponent<AndroidInteractionProbe>();
			UnityEngine.Debug.Log($"Android interaction probe ready: position={probe.transform.position}, layer={probe.layer}");
		}
		if (androidGoldProbe)
		{
			var inventory = player.inventory;
			int before = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice();
			inventory.ChangeGold(50);
			int acquired = inventory.goldInt;
			inventory.ChangeGold(-10);
			Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.IncreaseChestPrice();
			UnityEngine.Debug.Log($"Original gold probe: acquired={acquired}, afterSpend={inventory.goldInt}, priceBefore={before}, priceAfter={Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice()}, earned={Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats.GetStat(Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.goldEarned)}, spent={Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats.GetStat(Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.goldSpent)}");
		}
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage += LogOriginalForestDamage;
		if (androidDeathProbe)
		{
			UnityEngine.Debug.Log("Original death probe: applying lethal external damage");
			player.inventory.playerHealth.DamagePlayerExternal(10000f, 0f, Vector3.zero, true);
		}
		if (androidWeaponDamageProbe)
		{
			var characterVisual = player.playerRenderer != null ? player.playerRenderer.renderer : null;
			var visualMaterial = characterVisual != null ? characterVisual.sharedMaterial : null;
			UnityEngine.Debug.Log($"Original player shader probe: level={SystemInfo.graphicsShaderLevel}, shader={(visualMaterial != null ? visualMaterial.shader.name : "none")}, albedo={(visualMaterial != null && visualMaterial.GetTexture("_AlbedoMap") != null ? visualMaterial.GetTexture("_AlbedoMap").name : "none")}");
			UnityEngine.Debug.Log($"Original player visual probe: character={player.character}, selected={CharacterMenu.selectedCharacter}, renderer={(characterVisual != null ? characterVisual.name : "none")}, mesh={(characterVisual != null && characterVisual.sharedMesh != null ? characterVisual.sharedMesh.name : "none")}, material={(characterVisual != null && characterVisual.sharedMaterial != null ? characterVisual.sharedMaterial.name : "none")}, texture={(characterVisual != null && characterVisual.sharedMaterial != null && characterVisual.sharedMaterial.mainTexture != null ? characterVisual.sharedMaterial.mainTexture.name : "none")}");
			StartCoroutine(LogOriginalPlayerMotion(player));
			if (androidXpProbe) StartCoroutine(ProbeXpPickup(player));
			if (androidUpgradePoolProbe)
			{
				var offers = Assets.Scripts.Inventory__Items__Pickups.InventoryUtility.GetRandomUpgrades();
				var descriptions = new System.Collections.Generic.List<string>();
				foreach (var offer in offers)
					descriptions.Add($"{((Assets.Scripts.Saves___Serialization.Progression.Achievements.UnlockableBase)offer).GetInternalName()}:{offer.GetLevel()}");
				UnityEngine.Debug.Log($"Original upgrade pool probe: count={offers.Count}, offers={string.Join(",", descriptions)}");
			}
			Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully += LogOriginalProjectile;
			Assets.Scripts.Actors.Enemies.Enemy target = null;
			foreach (var enemy in EnemyManager.Instance.enemies.Values) { target = enemy; break; }
			foreach (var weapon in player.inventory.weaponInventory.weapons.Values)
			{
				var dc = Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponUtility.GetDamageContainer(weapon, null, target, Vector3.forward);
				UnityEngine.Debug.Log($"Original weapon damage probe: weapon={weapon.weaponData.eWeapon}, target={(target != null ? target.enemyData.enemyName.ToString() : "none")}, damage={dc.damage}, crit={dc.crit}, source={dc.damageSource}, knockback={dc.knockback}, proc={dc.procCoefficient}");
				Vector3 origin = player.transform.position + Vector3.up * weapon.weaponData.spawnOffset.y;
				float range = Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponUtility.GetWeaponRange(weapon);
				var selected = Assets.Scripts.Inventory__Items__Pickups.Weapons.EnemyTargeting.GetEnemy(origin, range, 0, weapon.weaponData.useVision, null);
				UnityEngine.Debug.Log($"Original weapon targeting probe: weapon={weapon.weaponData.eWeapon}, target={(selected != null ? selected.enemyData.enemyName.ToString() : "none")}, distance={(selected != null ? Vector3.Distance(origin, selected.GetCenterPosition()) : -1f)}, range={range}");
				break;
			}
		}
		if (androidAutoEnemyProbe)
		{
			EnemyData data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Skeleton);
			var sampled = EnemyManager.Instance.SpawnEnemy(data, -2, true);
			UnityEngine.Debug.Log($"Original automatic enemy probe: spawned={(sampled != null)}, position={(sampled != null ? sampled.transform.position : Vector3.zero)}, distance={(sampled != null ? Vector3.Distance(sampled.transform.position, player.transform.position) : -1f)}");
		}
		if (androidEnemyProbe)
		{
			Vector3 sample = player.transform.position + player.transform.forward * 6f;
			if (!Physics.Raycast(sample + Vector3.up * 10f, Vector3.down, out RaycastHit enemyGround, 30f))
			{
				UnityEngine.Debug.Log("Original enemy probe: no ground");
				yield break;
			}
			EnemyData data = DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Skeleton);
			var enemy = EnemyManager.Instance.SpawnEnemy(data, enemyGround.point, -1, true, canBeElite: false);
			UnityEngine.Debug.Log($"Original enemy probe: spawned={(enemy != null)}, hp={(enemy != null ? enemy.hp : 0f)}, height={(enemy != null ? enemy.meshHeight : 0f)}, position={(enemy != null ? enemy.transform.position : Vector3.zero)}");
			if (enemy != null)
			{
				Vector3 start = enemy.transform.position;
				float startDistance = Vector3.Distance(start, player.transform.position);
				yield return new WaitForSecondsRealtime(2f);
				UnityEngine.Debug.Log($"Original enemy probe movement: start={start}, end={enemy.transform.position}, distanceBefore={startDistance}, distanceAfter={Vector3.Distance(enemy.transform.position, player.transform.position)}");
			}
		}
	}
	private void LogOriginalForestDamage(Assets.Scripts.Inventory__Items__Pickups.PlayerHealth health, Assets.Scripts.Actors.DamageContainer damage, bool shieldDamage)
	{
		var player = gameManager.GetPlayer();
		var body = player != null ? player.GetComponent<Rigidbody>() : null;
		UnityEngine.Debug.Log($"Original Forest damage: hp={health.hp}, shield={health.shield}, damage={damage.damage}, shieldHit={shieldDamage}, player={(player != null ? player.transform.position : Vector3.zero)}, velocity={(body != null ? body.velocity : Vector3.zero)}, hitDirection={damage.direction}");
	}
	private IEnumerator LogOriginalPlayerMotion(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		for (int i = 0; i < 24; i++)
		{
			var body = player.GetComponent<Rigidbody>();
			bool ground = Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 500f, gameManager.whatIsGround, QueryTriggerInteraction.Ignore);
			UnityEngine.Debug.Log($"Original player motion: time={Assets.Scripts.Utility.MyTime.time}, hp={player.inventory.playerHealth.hp}, position={player.transform.position}, velocity={body.velocity}, grounded={player.playerMovement.grounded}, ground={(ground ? hit.collider.name : "none")}, groundDistance={(ground ? hit.distance : -1f)}, gameOver={gameManager.isGameOver}, paused={Assets.Scripts.Utility.MyTime.paused}, cutscene={gameManager.cutscene}, deathWindow={(UiManager.Instance != null && UiManager.Instance.deathScreen != null && UiManager.Instance.deathScreen.deadUiWindow != null && UiManager.Instance.deathScreen.deadUiWindow.activeInHierarchy)}");
			yield return new WaitForSecondsRealtime(1f);
		}
	}
	private void LogOriginalProjectile(Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileBase projectile)
	{
		UnityEngine.Debug.Log($"Original weapon projectile: weapon={projectile.weaponBase.weaponData.eWeapon}, position={projectile.transform.position}, direction={projectile.direction}");
	}
	private void LogEnemyDamage(Assets.Scripts.Actors.Enemies.Enemy enemy, Assets.Scripts.Actors.DamageContainer dc)
	{
		UnityEngine.Debug.Log($"Original enemy damage: enemy={enemy.enemyData.enemyName}, hp={enemy.hp}, damage={dc.damage}, source={dc.damageSource}");
	}
	private void LogEnemyDeath(Assets.Scripts.Actors.Enemies.Enemy enemy, Assets.Scripts.Actors.DamageContainer dc)
	{
		UnityEngine.Debug.Log($"Original enemy death: enemy={enemy.enemyData.enemyName}, source={dc.damageSource}");
	}
	private IEnumerator ProbeAttackTimeout(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		if (!player.inventory.weaponInventory.weapons.TryGetValue(EWeapon.FireStaff, out var weapon)) yield break;
		bool wasEnabled = weapon.enabled;
		weapon.Disable();
		var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance;
		var attack = pools.GetAttack(weapon);
		if (attack == null)
		{
			if (wasEnabled) weapon.Enable();
			UnityEngine.Debug.Log("Tracked attack timeout probe: attack unavailable");
			yield break;
		}
		attack.gameObject.SetActive(true);
		attack.SetAttack(weapon, player);
		float started = Time.realtimeSinceStartup;
		while (attack.gameObject.activeSelf && Time.realtimeSinceStartup - started < 12f)
			yield return new WaitForSecondsRealtime(0.1f);
		bool returned = !attack.gameObject.activeSelf;
		var pool = pools.weaponAttackPools[EWeapon.FireStaff];
		UnityEngine.Debug.Log($"Tracked attack timeout probe: returned={returned}, elapsed={Time.realtimeSinceStartup - started}, active={pool.CountActive}, inactive={pool.CountInactive}, total={pool.CountAll}");
		if (wasEnabled) weapon.Enable();
	}

	private IEnumerator ProbeShrines(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		StartCoroutine(ProbeAttackTimeout(player));
		var pauseMenu = UiManager.Instance.pause;
		bool couldPause = pauseMenu != null && pauseMenu.CanPause() && !Assets.Scripts.Utility.MyTime.paused;
		if (couldPause)
		{
			pauseMenu.Pause();
			bool opened = pauseMenu.IsPaused(), gamePaused = Assets.Scripts.Utility.MyTime.paused, mainOpen = pauseMenu.main.activeInHierarchy;
			pauseMenu.GoToOptions();
			bool optionsOpen = pauseMenu.options.activeInHierarchy && !pauseMenu.main.activeSelf;
			pauseMenu.GoToMap();
			bool mapOpen = pauseMenu.map.activeInHierarchy && !pauseMenu.options.activeSelf;
			pauseMenu.GoToMain();
			var dynamicWindows = AlwaysUi.Instance != null ? AlwaysUi.Instance.dynamicWindows : null;
			if (dynamicWindows != null)
			{
				int beforePrompts = dynamicWindows.contentParent.childCount;
				pauseMenu.Exit();
				var exitPrompt = dynamicWindows.contentParent.GetChild(dynamicWindows.contentParent.childCount - 1).GetComponent<Assets.Scripts.UI.DynamicWindows.DWindowPrompt>();
				bool exitCreated = exitPrompt != null && dynamicWindows.contentParent.childCount == beforePrompts + 1;
				string exitText = exitPrompt != null ? exitPrompt.t_content.text : "missing";
				if (exitPrompt != null) exitPrompt.Close();
				yield return null;
				pauseMenu.Restart();
				var restartPrompt = dynamicWindows.contentParent.GetChild(dynamicWindows.contentParent.childCount - 1).GetComponent<Assets.Scripts.UI.DynamicWindows.DWindowPrompt>();
				bool restartCreated = restartPrompt != null && dynamicWindows.contentParent.childCount == beforePrompts + 1;
				string restartText = restartPrompt != null ? restartPrompt.t_content.text : "missing";
				if (restartPrompt != null) restartPrompt.Close();
				yield return null;
				int accepted = 0;
				dynamicWindows.NewWindowPrompt("Diagnostic", "No run will be changed", () => accepted++);
				var safePrompt = dynamicWindows.contentParent.GetChild(dynamicWindows.contentParent.childCount - 1).GetComponent<Assets.Scripts.UI.DynamicWindows.DWindowPrompt>();
				if (safePrompt != null) safePrompt.Accept();
				yield return null;
				UnityEngine.Debug.Log($"Pause confirmation probe: exit={exitCreated}, restart={restartCreated}, accepted={accepted}, remaining={dynamicWindows.contentParent.childCount - beforePrompts}, exitText={exitText}, restartText={restartText}");
			}
			else UnityEngine.Debug.Log("Pause confirmation probe: dynamic window manager missing");
			pauseMenu.Toggle();
			UnityEngine.Debug.Log($"Pause menu probe: eligible={couldPause}, opened={opened}, gamePaused={gamePaused}, main={mainOpen}, options={optionsOpen}, map={mapOpen}, closed={!pauseMenu.IsPaused()}, gameResumed={!Assets.Scripts.Utility.MyTime.paused}");
		}
		else UnityEngine.Debug.Log($"Pause menu probe: skipped eligible={couldPause}");
		var projectileHits = Assets.Scripts.Objects.Pooling.PoolManager.Instance.projectileHitPools;
		bool fireStaffHitSeen = projectileHits != null && projectileHits.TryGetValue(EWeapon.FireStaff.ToString(), out var fireStaffHitPool) && fireStaffHitPool.CountAll > 0;
		UnityEngine.Debug.Log($"Natural Fire Staff hit effect probe: seen={fireStaffHitSeen}");
		var attackPools = Assets.Scripts.Objects.Pooling.PoolManager.Instance.weaponAttackPools;
		bool attackPoolSeen = attackPools.TryGetValue(EWeapon.FireStaff, out var attackPool);
		int attackTotalBefore = attackPoolSeen ? attackPool.CountAll : -1;
		int attackInactiveBefore = attackPoolSeen ? attackPool.CountInactive : -1;
		var stat = player.inventory.playerStats;
		var movement = player.playerMovement;
		var movementBody = movement.GetRb();
		Vector3 savedVelocity = movementBody.velocity;
		movementBody.velocity = new Vector3(3f, -4f, 12f);
		float observedSpeed = movement.GetSpeed();
		movement.StopFallVelocity();
		Vector3 stoppedVelocity = movementBody.velocity;
		movementBody.velocity = new Vector3(3f, 4f, 12f);
		movement.StopFallVelocity();
		Vector3 risingVelocity = movementBody.velocity;
		movementBody.velocity = savedVelocity;
		var privateFields = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
		Vector3 storedHead = (Vector3)typeof(PlayerMovement).GetField(movement.IsCrouching() ? "crouchHeadHeight" : "headHeight", privateFields).GetValue(movement);
		Vector3 storedFeet = (Vector3)typeof(PlayerMovement).GetField("feetHeight", privateFields).GetValue(movement);
		bool headMatches = Vector3.Distance(movement.GetRbHeadPosition(), movementBody.position + storedHead) < 0.001f;
		bool feetMatch = Vector3.Distance(movement.FeetPositionToRb(Vector3.one), Vector3.one + storedFeet) < 0.001f && Mathf.Abs(movement.GetFeetOffset() - storedFeet.y) < 0.001f;
		UnityEngine.Debug.Log($"Movement helper probe: speed={observedSpeed}, stopped={stoppedVelocity}, rising={risingVelocity}, collider={movement.GetPlayerCollider() == player.GetComponent<Collider>()}, camera={movement.GetPlayerCamTransform() == movement.playerCam}, head={headMatches}, feet={feetMatch}");
		var pushedField = typeof(PlayerMovement).GetField("pushed", privateFields);
		var pushCounterField = typeof(PlayerMovement).GetField("resetPushCounter", privateFields);
		var pushMultiplierField = typeof(PlayerMovement).GetField("pushMultiplier", privateFields);
		object savedPushed = pushedField.GetValue(movement), savedPushCounter = pushCounterField.GetValue(movement), savedPushMultiplier = pushMultiplierField.GetValue(movement);
		bool savedGravity = movementBody.useGravity;
		var padObject = new GameObject("DiagnosticJumpPad");
		var pad = padObject.AddComponent<JumpPad>();
		pad.force = 10f;
		var nonPlayer = new GameObject("DiagnosticNonPlayer");
		var otherCollider = nonPlayer.AddComponent<BoxCollider>();
		var trigger = typeof(JumpPad).GetMethod("OnTriggerEnter", privateFields);
		movementBody.velocity = new Vector3(3f, -4f, 2f);
		trigger.Invoke(pad, new object[] { otherCollider });
		Vector3 ignoredVelocity = movementBody.velocity;
		trigger.Invoke(pad, new object[] { movement.GetPlayerCollider() });
		Vector3 upwardLaunch = movementBody.velocity;
		pad.direction = nonPlayer.transform;
		pad.direction.rotation = Quaternion.LookRotation(Vector3.right);
		movementBody.velocity = new Vector3(3f, 4f, 2f);
		movement.JumpPad(pad);
		Vector3 directionalLaunch = movementBody.velocity;
		movementBody.velocity = new Vector3(1f, 2f, 3f);
		movement.RocketJump(new Vector3(4f, 5f, 6f));
		Vector3 rocketVelocity = movementBody.velocity;
		movement.BouncePlayer(new Vector3(7f, 8f, 9f));
		Vector3 bounceVelocity = movementBody.velocity;
		movementBody.velocity = savedVelocity;
		movementBody.useGravity = savedGravity;
		pushedField.SetValue(movement, savedPushed);
		pushCounterField.SetValue(movement, savedPushCounter);
		pushMultiplierField.SetValue(movement, savedPushMultiplier);
		Destroy(padObject);
		Destroy(nonPlayer);
		UnityEngine.Debug.Log($"Jump pad probe: ignored={ignoredVelocity}, upward={upwardLaunch}, directional={directionalLaunch}, rocket={rocketVelocity}, bounce={bounceVelocity}");
		var ladderFields = new[] { "ladder", "ladderNormal", "ladderWishDir", "ladderWallVec", "onLadderLastFrame", "ladderRefreshCount", "resetJumpCounter" };
		var ladderValues = new object[ladderFields.Length];
		for (int i = 0; i < ladderFields.Length; i++) ladderValues[i] = typeof(PlayerMovement).GetField(ladderFields[i], privateFields).GetValue(movement);
		bool savedOnLadder = movement.onLadder;
		var ladderObject = new GameObject("DiagnosticLadder");
		ladderObject.AddComponent<BoxCollider>();
		var ladder = ladderObject.AddComponent<Ladder>();
		ladderObject.transform.position = movement.feet.position + Vector3.down * 2f;
		typeof(PlayerMovement).GetField("resetJumpCounter", privateFields).SetValue(movement, 12);
		movement.onLadder = false;
		var ladderTrigger = typeof(Ladder).GetMethod("OnTriggerStay", privateFields);
		ladderTrigger.Invoke(ladder, new object[] { ladderObject.GetComponent<Collider>() });
		bool ignoredNonPlayerLadder = !movement.onLadder;
		ladderTrigger.Invoke(ladder, new object[] { movement.GetPlayerCollider() });
		bool attachedLadder = movement.onLadder;
		typeof(PlayerMovement).GetMethod("LadderMovementTick", privateFields).Invoke(movement, new object[] { 0f, 1f });
		Vector3 ladderVelocity = movementBody.velocity;
		Vector3 expectedLadderVelocity = Quaternion.AngleAxis(90f, ladderObject.transform.right) * movement.orientation.forward * 11f * (movement.IsCrouching() ? 0.5f : 1f);
		typeof(PlayerMovement).GetMethod("StopLadder", privateFields).Invoke(movement, null);
		bool detachedLadder = !movement.onLadder;
		for (int i = 0; i < ladderFields.Length; i++) typeof(PlayerMovement).GetField(ladderFields[i], privateFields).SetValue(movement, ladderValues[i]);
		movement.onLadder = savedOnLadder;
		movementBody.velocity = savedVelocity;
		Destroy(ladderObject);
		UnityEngine.Debug.Log($"Ladder probe: ignoredNonPlayer={ignoredNonPlayerLadder}, attached={attachedLadder}, velocity={ladderVelocity}, expected={expectedLadderVelocity}, detached={detachedLadder}");
		if (PlayerCamera.Instance != null)
		{
			var waterObject = new GameObject("DiagnosticWater");
			var water = waterObject.AddComponent<Water>();
			var waterType = typeof(Water);
			var waterUpdate = waterType.GetMethod("Update", privateFields);
			var waterFixed = waterType.GetMethod("FixedUpdate", privateFields);
			waterType.GetProperty("color").SetValue(water, new Color(0.2f, 0.4f, 0.6f, 1f));
			var waterFilter = FindObjectOfType<Assets.Scripts.Game.Other.Water.WaterFilterUI>();
			var lowpass = FindObjectOfType<LowpassController>();
			float savedFilterAlpha = waterFilter != null ? waterFilter.group.alpha : 0f;
			Color savedFilterColor = waterFilter != null ? waterFilter.i_color.color : Color.clear;
			var submergedField = typeof(PlayerMovement).GetField("isUnderwater", privateFields);
			object savedSubmerged = submergedField.GetValue(movement);
			var playerHealth = player.inventory.playerHealth;
			var cooldownField = typeof(Assets.Scripts.Inventory__Items__Pickups.PlayerHealth).GetField("damageCooldown", privateFields);
			var invincibleField = typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("isInvincible", privateFields);
			object savedCooldown = cooldownField.GetValue(playerHealth), savedInvincibility = invincibleField.GetValue(player);
			int savedHp = playerHealth.hp;
			float savedShield = playerHealth.shield, savedOverheal = playerHealth.overheal;
			int playerEnter = 0, playerExit = 0, cameraEnter = 0, cameraExit = 0;
			System.Action<Water> onPlayerEnter = w => { if (w == water) playerEnter++; };
			System.Action<Water> onPlayerExit = w => { if (w == water) playerExit++; };
			System.Action<Water> onCameraEnter = w => { if (w == water) cameraEnter++; };
			System.Action<Water> onCameraExit = w => { if (w == water) cameraExit++; };
			Water.A_PlayerEnterWater += onPlayerEnter;
			Water.A_PlayerExitWater += onPlayerExit;
			Water.A_CameraEnterWater += onCameraEnter;
			Water.A_CameraExitWater += onCameraExit;
			try
			{
				float feetY = player.GetFeetPosition().y, cameraY = PlayerCamera.Instance.transform.position.y;
				waterObject.transform.position = new Vector3(0f, Mathf.Max(feetY + 0.15f, cameraY) + 1f, 0f);
				waterUpdate.Invoke(water, null);
				bool underwaterAfterEnter = (bool)submergedField.GetValue(movement);
				bool filterOnEntry = waterFilter != null && (bool)waterFilter.GetType().GetField("usingFilter", privateFields).GetValue(waterFilter);
				Color tintOnEntry = waterFilter != null ? waterFilter.i_color.color : Color.clear;
				float cutoffOnEntry = lowpass != null ? (float)typeof(LowpassController).GetMethod("GetDesiredCutoff", privateFields).Invoke(lowpass, null) : -1f;
				waterType.GetField("isDamageZone", privateFields).SetValue(water, true);
				waterType.GetField("damage", privateFields).SetValue(water, 10f);
				waterType.GetField("nextDamageTime", privateFields).SetValue(water, 0f);
				cooldownField.SetValue(playerHealth, 0f);
				invincibleField.SetValue(player, false);
				playerHealth.hp = 100;
				playerHealth.shield = playerHealth.overheal = 0f;
				waterFixed.Invoke(water, null);
				int hpAfterDamage = playerHealth.hp;
				waterFixed.Invoke(water, null);
				int hpAfterImmediateRepeat = playerHealth.hp;
				waterObject.transform.position = new Vector3(0f, Mathf.Min(feetY + 0.15f, cameraY) - 1f, 0f);
				waterUpdate.Invoke(water, null);
				bool filterOnExit = waterFilter != null && (bool)waterFilter.GetType().GetField("usingFilter", privateFields).GetValue(waterFilter);
				bool audioUnderwaterOnExit = lowpass != null && (bool)typeof(LowpassController).GetField("isUnderwater", privateFields).GetValue(lowpass);
				UnityEngine.Debug.Log($"Water probe: playerEnter={playerEnter}, playerExit={playerExit}, cameraEnter={cameraEnter}, cameraExit={cameraExit}, submergedOnEntry={underwaterAfterEnter}, submergedOnExit={submergedField.GetValue(movement)}, hpAfterDamage={hpAfterDamage}, hpAfterRepeat={hpAfterImmediateRepeat}");
				UnityEngine.Debug.Log($"Water feedback probe: filterFound={waterFilter != null}, filterOnEntry={filterOnEntry}, tint={tintOnEntry}, filterOnExit={filterOnExit}, lowpassFound={lowpass != null}, cutoffOnEntry={cutoffOnEntry}, audioUnderwaterOnExit={audioUnderwaterOnExit}");
			}
			finally
			{
				Water.A_PlayerEnterWater -= onPlayerEnter;
				Water.A_PlayerExitWater -= onPlayerExit;
				Water.A_CameraEnterWater -= onCameraEnter;
				Water.A_CameraExitWater -= onCameraExit;
				submergedField.SetValue(movement, savedSubmerged);
				cooldownField.SetValue(playerHealth, savedCooldown);
				invincibleField.SetValue(player, savedInvincibility);
				playerHealth.hp = savedHp;
				playerHealth.shield = savedShield;
				playerHealth.overheal = savedOverheal;
				if (waterFilter != null) { waterFilter.group.alpha = savedFilterAlpha; waterFilter.i_color.color = savedFilterColor; }
				movementBody.velocity = savedVelocity;
				Destroy(waterObject);
			}
		}
		if (PlayerCamera.Instance != null)
		{
			var lavaObject = new GameObject("DiagnosticLava");
			var lavaCollider = lavaObject.AddComponent<BoxCollider>();
			lavaCollider.size = Vector3.one * 50f;
			lavaObject.transform.position = (player.GetFeetPosition() + PlayerCamera.Instance.transform.position) * 0.5f;
			var lava = lavaObject.AddComponent<Lava>();
			var lavaType = typeof(Lava);
			var lavaStart = lavaType.GetMethod("Start", privateFields);
			var lavaBounds = lavaType.GetMethod("UpdateBounds", privateFields);
			var lavaUpdate = lavaType.GetMethod("Update", privateFields);
			var lavaFixed = lavaType.GetMethod("FixedUpdate", privateFields);
			var submergedField = typeof(PlayerMovement).GetField("isUnderwater", privateFields);
			object savedSubmerged = submergedField.GetValue(movement);
			var playerHealth = player.inventory.playerHealth;
			var cooldownField = typeof(Assets.Scripts.Inventory__Items__Pickups.PlayerHealth).GetField("damageCooldown", privateFields);
			var invincibleField = typeof(Assets.Scripts.Actors.Player.MyPlayer).GetField("isInvincible", privateFields);
			object savedCooldown = cooldownField.GetValue(playerHealth), savedInvincibility = invincibleField.GetValue(player);
			int savedHp = playerHealth.hp;
			float savedShield = playerHealth.shield, savedOverheal = playerHealth.overheal;
			int playerEnter = 0, playerExit = 0, cameraEnter = 0, cameraExit = 0;
			System.Action onPlayerEnter = () => playerEnter++;
			System.Action onPlayerExit = () => playerExit++;
			System.Action onCameraEnter = () => cameraEnter++;
			System.Action onCameraExit = () => cameraExit++;
			Lava.A_PlayerEnterWater += onPlayerEnter;
			Lava.A_PlayerExitWater += onPlayerExit;
			Lava.A_CameraEnterWater += onCameraEnter;
			Lava.A_CameraExitWater += onCameraExit;
			try
			{
				Physics.SyncTransforms();
				lavaStart.Invoke(lava, null);
				lavaBounds.Invoke(lava, null);
				lavaUpdate.Invoke(lava, null);
				bool underwaterAfterEnter = (bool)submergedField.GetValue(movement);
				float initialDamage = (float)lavaType.GetField("damage", privateFields).GetValue(lava);
				cooldownField.SetValue(playerHealth, 0f);
				invincibleField.SetValue(player, false);
				playerHealth.hp = 100;
				playerHealth.shield = playerHealth.overheal = 0f;
				lavaFixed.Invoke(lava, null);
				int hpAfterDamage = playerHealth.hp;
				lavaFixed.Invoke(lava, null);
				int hpAfterRepeat = playerHealth.hp;
				lavaObject.transform.position += Vector3.right * 1000f;
				Physics.SyncTransforms();
				lavaFixed.Invoke(lava, null);
				lavaUpdate.Invoke(lava, null);
				UnityEngine.Debug.Log($"Lava probe: playerEnter={playerEnter}, playerExit={playerExit}, cameraEnter={cameraEnter}, cameraExit={cameraExit}, submergedOnEntry={underwaterAfterEnter}, submergedOnExit={submergedField.GetValue(movement)}, initialDamage={initialDamage}, hpAfterDamage={hpAfterDamage}, hpAfterRepeat={hpAfterRepeat}");
			}
			finally
			{
				Lava.A_PlayerEnterWater -= onPlayerEnter;
				Lava.A_PlayerExitWater -= onPlayerExit;
				Lava.A_CameraEnterWater -= onCameraEnter;
				Lava.A_CameraExitWater -= onCameraExit;
				submergedField.SetValue(movement, savedSubmerged);
				cooldownField.SetValue(playerHealth, savedCooldown);
				invincibleField.SetValue(player, savedInvincibility);
				playerHealth.hp = savedHp;
				playerHealth.shield = savedShield;
				playerHealth.overheal = savedOverheal;
				movementBody.velocity = savedVelocity;
				Destroy(lavaObject);
			}
		}
		var levelUp = typeof(Assets.Scripts.Inventory__Items__Pickups.PlayerHealth).GetMethod("OnLevelUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		int levelProbeBase = player.inventory.playerHealth.maxHp;
		int levelProbeCurrentHp = player.inventory.playerHealth.hp;
		bool hadLevelModifier = player.inventory.statInventory.movingStats.TryGetValue("LevelHp", out var originalLevelModifier);
		levelUp.Invoke(player.inventory.playerHealth, new object[] { 2 });
		player.inventory.statInventory.Tick();
		stat.ForceUpdateStats();
		int levelTwoHp = player.inventory.playerHealth.maxHp;
		levelUp.Invoke(player.inventory.playerHealth, new object[] { 5 });
		player.inventory.statInventory.Tick();
		stat.ForceUpdateStats();
		int levelFiveHp = player.inventory.playerHealth.maxHp;
		if (hadLevelModifier) player.inventory.statInventory.ChangeMovingStat("LevelHp", originalLevelModifier);
		else player.inventory.statInventory.RemoveMovingStat("LevelHp");
		player.inventory.statInventory.Tick();
		stat.ForceUpdateStats();
		player.inventory.playerHealth.hp = levelProbeCurrentHp;
		UnityEngine.Debug.Log($"Level health probe: base={levelProbeBase}, level2={levelTwoHp}, level5={levelFiveHp}, restored={player.inventory.playerHealth.maxHp}");
		var health = player.inventory.playerHealth;
		var landed = typeof(Assets.Scripts.Inventory__Items__Pickups.PlayerHealth).GetMethod("OnPlayerLanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		int oldLandingHp = health.hp;
		float oldLandingShield = health.shield, oldLandingOverheal = health.overheal, oldLandingTime = health.fallDamageTakenAtTime;
		var oldCharacter = player.character;
		player.character = ECharacter.Fox;
		health.hp = 100;
		health.shield = 0f;
		health.overheal = 0f;
		landed.Invoke(health, new object[] { 10f });
		int softLandingHp = health.hp;
		int softEffects = System.Linq.Enumerable.Count(FindObjectsOfType<DestroyObject>(), fx => fx.name == "FallDamageEffect(Clone)");
		landed.Invoke(health, new object[] { 40f });
		int hardLandingHp = health.hp;
		var landingEffect = System.Linq.Enumerable.FirstOrDefault(FindObjectsOfType<DestroyObject>(), fx => fx.name == "FallDamageEffect(Clone)");
		var landingSound = landingEffect != null ? landingEffect.GetComponent<RandomSfx>() : null;
		float landingDistance = landingEffect != null ? Vector3.Distance(landingEffect.transform.position, player.playerMovement.GetRbFeetPosition()) : -1f;
		health.hp = 5;
		landed.Invoke(health, new object[] { 1000f });
		int lowHpLanding = health.hp;
		player.character = ECharacter.Spaceman;
		health.hp = 100;
		landed.Invoke(health, new object[] { 1000f });
		UnityEngine.Debug.Log($"Fall damage probe: softHp={softLandingHp}, hardHp={hardLandingHp}, lowHp={lowHpLanding}, immuneHp={health.hp}, softEffects={softEffects}, effectFound={landingEffect != null}, effectFeetDistance={landingDistance}, soundClip={(landingSound != null && landingSound.s != null && landingSound.s.clip != null)}");
		player.character = oldCharacter;
		health.hp = oldLandingHp;
		health.shield = oldLandingShield;
		health.overheal = oldLandingOverheal;
		health.fallDamageTakenAtTime = oldLandingTime;
		float healthBefore = stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.MaxHealth);
		float regenBefore = stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.HealthRegen);
		float armorBefore = stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.Armor);
		float thornsBefore = stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.Thorns);
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.LeechingCrystal);
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SpikyShield);
		yield return new WaitForSecondsRealtime(1.5f);
		UnityEngine.Debug.Log($"Item stat probe: healthBefore={healthBefore}, healthAfter={stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.MaxHealth)}, regenBefore={regenBefore}, regenAfter={stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.HealthRegen)}, armorBefore={armorBefore}, armorAfter={stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.Armor)}, thornsBefore={thornsBefore}, thornsAfter={stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.Thorns)}");
		player.inventory.itemInventory.AddItem(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SpikyShield);
		yield return new WaitForSecondsRealtime(1.5f);
		attackPoolSeen = attackPools.TryGetValue(EWeapon.FireStaff, out attackPool);
		UnityEngine.Debug.Log($"Natural attack pool probe: totalBefore={attackTotalBefore}, inactiveBefore={attackInactiveBefore}, totalAfter={(attackPoolSeen ? attackPool.CountAll : -1)}, inactiveAfter={(attackPoolSeen ? attackPool.CountInactive : -1)}, activeAfter={(attackPoolSeen ? attackPool.CountActive : -1)}");
		UnityEngine.Debug.Log($"Fall effect cleanup probe: remaining={System.Linq.Enumerable.Count(FindObjectsOfType<DestroyObject>(), fx => fx.name == "FallDamageEffect(Clone)")}");
		var target = System.Linq.Enumerable.FirstOrDefault(EnemyManager.Instance.enemies.Values, enemy => enemy != null && enemy.CanTakeDamage());
		if (target != null)
		{
			var weapon = System.Linq.Enumerable.FirstOrDefault(player.inventory.weaponInventory.weapons.Values);
			if (weapon != null && target.collider != null)
			{
				var probeObject = new GameObject("ProjectileCollisionProbe");
				var attack = probeObject.AddComponent<Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack>();
				attack.weaponBase = weapon;
				var projectile = probeObject.AddComponent<AndroidProjectileCollisionProbe>();
				projectile.Configure(weapon, attack);
				var oldMode = weapon.weaponData.amplificationMode;
				bool oldCanBounce = weapon.weaponData.canBounce;
				float oldHp = target.hp;
				var wall = new GameObject("ProjectileCollisionProbeWall");
				try
				{
					weapon.weaponData.amplificationMode = Assets.Scripts.Inventory__Items__Pickups.Weapons.EAmplificationMode.Pierce;
					weapon.weaponData.canBounce = true;
					target.SetDiagnosticHealth(1000f);
					bool first = projectile.TestCollision(target.collider, Vector3.zero);
					float afterHit = target.hp;
					bool repeated = projectile.TestCollision(target.collider, Vector3.zero);
					wall.layer = LayerMask.NameToLayer("Object");
					var wallCollider = wall.AddComponent<BoxCollider>();
					bool wallHit = projectile.TestCollision(wallCollider, Vector3.left);
					UnityEngine.Debug.Log($"Ordinary projectile collision probe: enemyHit={first}, damage={1000f - afterHit}, duplicateHit={repeated}, hpAfterDuplicate={target.hp}, wallHit={wallHit}, reflected={projectile.direction}, bounces={projectile.bounces}");
				}
				finally
				{
					target.SetDiagnosticHealth(oldHp);
					weapon.weaponData.amplificationMode = oldMode;
					weapon.weaponData.canBounce = oldCanBounce;
					Destroy(probeObject);
					Destroy(wall);
				}
			}
			float enemyHpBefore = target.hp;
			var incoming = new Assets.Scripts.Actors.DamageContainer(0f, "probe") { enemy = target, direction = Vector3.forward };
			player.inventory.playerHealth.Retaliate(incoming);
			UnityEngine.Debug.Log($"Thorns combat probe: armor={stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.Armor)}, thorns={stat.GetRawStat(Assets.Scripts.Menu.Shop.EStat.Thorns)}, enemy={target.enemyData.enemyName}, hpBefore={enemyHpBefore}, hpAfter={target.hp}");
			var pools = Assets.Scripts.Objects.Pooling.PoolManager.Instance.projectileHitPools;
			var hitPool = pools[Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.thornsDamageSource];
			int activeBefore = hitPool.CountActive;
			yield return new WaitForSecondsRealtime(0.8f);
			UnityEngine.Debug.Log($"Thorns hit effect probe: activeBefore={activeBefore}, activeAfter={hitPool.CountActive}, inactive={hitPool.CountInactive}");
		}
		else UnityEngine.Debug.Log("Thorns combat probe: no active enemy");
		foreach (var item in new[] { Assets.Scripts.Inventory__Items__Pickups.Items.EItem.Beer, Assets.Scripts.Inventory__Items__Pickups.Items.EItem.LeechingCrystal, Assets.Scripts.Inventory__Items__Pickups.Items.EItem.SpikyShield, Assets.Scripts.Inventory__Items__Pickups.Items.EItem.GloveLightning })
		{
			var data = DataManager.Instance.GetItem(item);
			UnityEngine.Debug.Log($"Item description probe: item={item}, text={data.GetDummyItem().GetDescription(data.localizedDescription)}");
		}
		var commonPrice = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetItemPriceShadyGuy(Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Common);
		var rarePrice = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetItemPriceShadyGuy(Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Rare);
		var epicPrice = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetItemPriceShadyGuy(Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Epic);
		var legendaryPrice = Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetItemPriceShadyGuy(Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Legendary);
		UnityEngine.Debug.Log($"Shady price probe: stage={MapController.index}, common={commonPrice}, rare={rarePrice}, epic={epicPrice}, legendary={legendaryPrice}");
		var shadyOffers = Assets.Scripts.Inventory__Items__Pickups.InventoryUtility.GetRandomItemsShadyGuy(Assets.Scripts.Inventory__Items__Pickups.Items.EItemRarity.Common);
		var shadyUnique = new System.Collections.Generic.HashSet<ItemData>(shadyOffers);
		UnityEngine.Debug.Log($"Shady offers probe: total={shadyOffers.Count}, unique={shadyUnique.Count}");
		var forcedRare = Assets.Scripts.Inventory__Items__Pickups.Rarity.GetShadyGuyRarity(0f, new[] { 0f, 1f, 0f, 0f });
		var forcedLegendary = Assets.Scripts.Inventory__Items__Pickups.Rarity.GetShadyGuyRarity(0f, new[] { 0f, 0f, 0f, 1f });
		UnityEngine.Debug.Log($"Shady rarity probe: forcedRare={forcedRare}, forcedLegendary={forcedLegendary}");
		var magnet = FindObjectOfType<InteractableShrineMagnet>();
		var moai = FindObjectOfType<InteractableShrineMoai>();
		var pickup = PickupManager.Instance.SpawnPickup(Assets.Scripts.Inventory__Items__Pickups.Pickups.EPickup.Xp,
			player.transform.position + Vector3.right * 15f, 1, false);
		bool canPickupBefore = pickup != null && pickup.CanPickup();
		bool magnetResult = magnet != null && magnet.Interact();
		bool secondMagnetResult = magnet != null && magnet.Interact();
		bool following = pickup != null && !pickup.CanPickup() && !pickup.collider.enabled;
		UnityEngine.Debug.Log($"Shrine magnet probe: found={magnet != null}, pickup={pickup != null}, availableBefore={canPickupBefore}, first={magnetResult}, second={secondMagnetResult}, following={following}");
		yield return new WaitForSecondsRealtime(2f);
		bool moaiResult = moai != null && moai.Interact();
		bool secondMoaiResult = moai != null && moai.Interact();
		var encounter = UiManager.Instance.encounterWindows;
		var screen = encounter.levelupScreen.GetComponent<LevelupScreen>();
		UnityEngine.Debug.Log($"Shrine moai probe: found={moai != null}, first={moaiResult}, second={secondMoaiResult}, encounter={encounter.encounterInProgress}, window={screen.window.activeInHierarchy}, choices={screen.upgradePicker.GetNumUpgrades()}");
		int before = 0;
		foreach (var item in player.inventory.itemInventory.items.Values) before += item.amount;
		if (screen.window.activeInHierarchy && screen.upgradePicker.GetNumUpgrades() > 0)
			screen.upgradePicker.buttons[0].GetButton().onClick.Invoke();
		int after = 0;
		foreach (var item in player.inventory.itemInventory.items.Values) after += item.amount;
		UnityEngine.Debug.Log($"Shrine moai selection probe: itemsBefore={before}, itemsAfter={after}, encounter={encounter.encounterInProgress}, window={screen.window.activeInHierarchy}");
		if (encounter.encounterInProgress) encounter.RewardFinished();
		foreach (GameObject prefab in MapController.currentMap.shrines)
		{
			if (prefab.GetComponentInChildren<InteractableShrineCursed>() == null) continue;
			GameObject spawned = Instantiate(prefab, player.transform.position + Vector3.right * 20f, Quaternion.identity);
			var cursed = spawned.GetComponentInChildren<InteractableShrineCursed>();
			int previousCurses = gameManager.bossCurses;
			bool first = cursed.Interact();
			bool second = cursed.Interact();
			UnityEngine.Debug.Log($"Shrine cursed probe: first={first}, second={second}, cursesBefore={previousCurses}, cursesAfter={gameManager.bossCurses}, impact={cursed.impactFx.activeSelf}, constant={cursed.constantFx.activeSelf}");
			gameManager.bossCurses = previousCurses;
			Destroy(spawned);
			break;
		}
		var shady = FindObjectOfType<InteractableShadyGuy>();
		if (shady == null) { UnityEngine.Debug.Log("Shady purchase probe: merchant not spawned"); yield break; }
		int existingGold = player.inventory.goldInt;
		if (existingGold > 0) player.inventory.ChangeGold(-existingGold);
		shady.Interact();
		int itemCountBefore = 0;
		foreach (var item in player.inventory.itemInventory.items.Values) itemCountBefore += item.amount;
		int offered = screen.upgradePicker.GetNumUpgrades();
		if (offered == 0) { UnityEngine.Debug.Log("Shady purchase probe: no offers"); yield break; }
		int cost = shady.prices[0];
		screen.upgradePicker.buttons[0].GetButton().onClick.Invoke();
		int afterRejected = 0;
		foreach (var item in player.inventory.itemInventory.items.Values) afterRejected += item.amount;
		bool remainedOpen = screen.window.activeInHierarchy;
		encounter.RewardFinished();
		player.inventory.ChangeGold(5000);
		yield return null;
		shady.Interact();
		int goldBeforePurchase = player.inventory.goldInt;
		cost = shady.prices[0];
		screen.upgradePicker.buttons[0].GetButton().onClick.Invoke();
		int afterPurchase = 0;
		foreach (var item in player.inventory.itemInventory.items.Values) afterPurchase += item.amount;
		UnityEngine.Debug.Log($"Shady purchase probe: offers={offered}, rejectedBefore={itemCountBefore}, rejectedAfter={afterRejected}, remainedOpen={remainedOpen}, cost={cost}, goldBefore={goldBeforePurchase}, goldAfter={player.inventory.goldInt}, itemsAfter={afterPurchase}, closed={!screen.window.activeInHierarchy}, consumed={!shady.CanInteract()}");
		if (encounter.encounterInProgress) encounter.RewardFinished();
	}
	private IEnumerator ProbeXpPickup(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var pickup = PickupManager.Instance.SpawnPickup(Assets.Scripts.Inventory__Items__Pickups.Pickups.EPickup.Xp, player.transform.position, androidXpProbeValue, false);
		UnityEngine.Debug.Log($"Original xp probe spawned: active={(pickup != null && pickup.gameObject.activeInHierarchy)}, value={(pickup != null ? pickup.GetValue() : -1)}, pickup={(pickup != null ? pickup.transform.position : Vector3.zero)}, player={player.transform.position}, tag={player.gameObject.tag}");
		yield return new WaitForSecondsRealtime(4f);
		UnityEngine.Debug.Log($"Original xp probe result: total={player.inventory.playerXp.xp}, level={player.inventory.playerXp.level}");
	}
	private IEnumerator ProbeGoldCoins(Assets.Scripts.Actors.Player.MyPlayer player)
	{
		var pool = Assets.Scripts.Objects.Pooling.PoolManager.Instance.goldPool;
		int before = player.inventory.goldInt;
		Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.SpawnMoney(26, player.transform.position + Vector3.up);
		UnityEngine.Debug.Log($"Original coin probe spawned: active={pool.CountActive}, before={before}");
		yield return new WaitForSecondsRealtime(4f);
		UnityEngine.Debug.Log($"Original coin probe result: active={pool.CountActive}, gold={player.inventory.goldInt}, gained={player.inventory.goldInt - before}");
	}
	private void LogLevelWindowOpened()
	{
		UnityEngine.Debug.Log($"Original level window opened: paused={Assets.Scripts.Utility.MyTime.paused}, choices={UiManager.Instance.encounterWindows.levelupScreen.GetComponent<LevelupScreen>().upgradePicker.GetNumUpgrades()}");
		StartCoroutine(LogLevelSelection());
	}
	private IEnumerator LogLevelSelection()
	{
		var screen = UiManager.Instance.encounterWindows.levelupScreen.GetComponent<LevelupScreen>();
		GameObject previous = null;
		while (screen.window.activeInHierarchy && LevelupScreen.isLevelingUp)
		{
			var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
			if (selected != previous)
			{
				previous = selected;
				int index = System.Array.FindIndex(screen.upgradePicker.buttons, button => button != null && button.gameObject == selected);
				UnityEngine.Debug.Log($"Original level selection: index={index}, object={(selected != null ? selected.name : "none")}");
			}
			yield return new WaitForSecondsRealtime(0.2f);
		}
	}
	private void LogLevelWindowClosed()
	{
		UnityEngine.Debug.Log($"Original level window closed: paused={Assets.Scripts.Utility.MyTime.paused}");
	}
	private void LogTomeUpgrade(Assets.Scripts._Data.Tomes.ETome tome, Assets.Scripts.Menu.Shop.EStat stat)
	{
		UnityEngine.Debug.Log($"Original tome upgraded: tome={tome}, stat={stat}");
	}
	private void LogWeaponUpgrade(Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase weapon)
	{
		UnityEngine.Debug.Log($"Original weapon upgraded: weapon={weapon.weaponData.eWeapon}, level={weapon.level}");
	}
	private void LogPickup(Pickup pickup)
	{
		UnityEngine.Debug.Log($"Original pickup collected: type={pickup.ePickup}, value={pickup.GetValue()}");
	}
	private void LogXp(Inventory__Items__Pickups.Xp_and_Levels.PlayerXp xp, int gained)
	{
		UnityEngine.Debug.Log($"Original xp added: gained={gained}, total={xp.xp}, level={xp.level}");
	}
	private void OnDestroy()
	{
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= LogGraveyardDamage;
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_Died -= LogGraveyardDeath;
		LevelupScreen.A_LevelupEnabled -= LogLevelWindowOpened;
		LevelupScreen.A_LevelUpClose -= LogLevelWindowClosed;
		Assets.Scripts.Inventory__Items__Pickups.TomeInventory.A_TomeUpgrade -= LogTomeUpgrade;
		Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponInventory.A_WeaponAdded -= LogWeaponUpgrade;
		Pickup.A_PickupTriggered -= LogPickup;
		Inventory__Items__Pickups.Xp_and_Levels.PlayerXp.A_XpAdded -= LogXp;
		Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= LogEnemyDamage;
		Assets.Scripts.Actors.Enemies.Enemy.A_EnemyDied -= LogEnemyDeath;
		Assets.Scripts.Inventory__Items__Pickups.PlayerHealth.A_TakeDamage -= LogOriginalForestDamage;
		Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks.WeaponAttack.A_SpawnedProjectileSuccessfully -= LogOriginalProjectile;
	}
#endif

	private void CryptGeneration(int seed, out Vector3 spawnPos, out Vector3 spawnDir)
	{
		cryptParent.SetActive(true);
		rsgController.Generate(seed, RsgController.EDungeonType.Normal, out float traversalTime);
		rsgController.rsgEnd.SetTeleportTransform(tileSpawnPosition, tileSpawnDir, -tileSpawnDir);
		gameManager.StartDungeon(traversalTime);
		Transform finalTile = proceduralTileGeneration.flatTiles[proceduralTileGeneration.flatTiles.Count - 1].transform;
		Vector3 direction = -finalTile.GetComponent<ProceduralTile>().parentDir;
		direction.y = 0f;
		rsgController.roomBoss.interactableGhostBossLeave.SetTeleportTransform(finalTile.position + direction * 15f, direction);
		Instantiate(cryptExitOutside, tileSpawnPosition, Quaternion.LookRotation(tileSpawnDir)).SetActive(true);
		spawnPos = rsgController.rsgStart.spawnTransform.position;
		spawnDir = rsgController.rsgStart.spawnTransform.forward;
	}

	private IEnumerator GenerateProceduralMap(MapData map, StageData stage)
	{
		proceduralMapMeshGenerator.gameObject.SetActive(true);
#if UNITY_ANDROID && !UNITY_EDITOR
		proceduralMapMeshGenerator.GenerateMap(map, stage, AndroidSpawnSurfaceProbe.TerrainSeed(mapSeed));
		AndroidSpawnSurfaceProbe.LogTerrainHeights(proceduralMapMeshGenerator, mapSeed);
#else
		proceduralMapMeshGenerator.GenerateMap(map, stage, mapSeed);
#endif
		MapDisplay display = proceduralMapMeshGenerator.display;
		Mesh worldMesh = display.meshFilter.sharedMesh;
		Physics.SyncTransforms();
		Bounds bounds = display.meshCollider.bounds;
		Vector3 worldSize = bounds.size;
		Vector3 worldCenter = bounds.center;
		minimapMesh.transform.localScale = display.meshRenderer.transform.localScale;
		proceduralMeshRenderer.sharedMaterial = stage.triplanarMaterial;
		yield return null;

		Vector3 spawnPosition = SpawnPositions.GetObjectSpawnPosition(worldCenter + Vector3.up * 999f, worldSize,
			5f, gameManager.whatIsGroundAndObjects, out _, 9999, true, false, false, 10f);
		if ((spawnPosition - SpawnPositions.INVALID_POS).sqrMagnitude < 1e-10f)
			throw new InvalidOperationException("Could not find a suitable procedural map spawn position.");
		spawnPosition += Vector3.up * 2f;
		Vector3 spawnDirection = UnityEngine.Random.insideUnitSphere;
		spawnDirection.y = 0f;
		if (spawnDirection.sqrMagnitude < 0.0001f) spawnDirection = Vector3.forward;
		spawnDirection.Normalize();
		tileSpawnPosition = spawnPosition;
		tileSpawnDir = spawnDirection;
		if (spawnPlatform != null) Instantiate(spawnPlatform, spawnPosition, spawnPlatform.transform.rotation);
		Instantiate(spawnPortal, spawnPosition + spawnDirection * 3f, Quaternion.LookRotation(-spawnDirection)).GetComponent<SpawnPlayerPortal>().StartPortal();

		Vector3 area = new Vector3(worldSize.x, 1f, worldSize.z);
		Vector3 areaCenter = worldCenter + Vector3.up * (worldSize.y * 0.5f + 50f);
		randomObjectPlacer.size = area;
		randomObjectPlacer.center = areaCenter;
		randomObjectPlacer.Generate(stage.randomMapObjects, 1f);
		yield return null;
		randomObjectPlacer.GenerateInteractables(map);
		yield return null;
		Vector3 bossPosition = SpawnPositions.GetObjectSpawnPosition(worldCenter + Vector3.up * 999f, worldSize,
			5f, gameManager.whatIsGroundAndObjects, out _, 9999, true, false, false, 10f);
		if ((bossPosition - SpawnPositions.INVALID_POS).sqrMagnitude >= 1e-10f)
		{
			GameObject bossPrefab = MapController.IsLastStage() ? bossPortalFinal : bossPortal;
			if (bossPrefab != null) Instantiate(bossPrefab, bossPosition, bossPrefab.transform.rotation);
		}
		interactablesSpawner.SetArea(area, areaCenter, area.x * area.z);
		interactablesSpawner.SpawnShit();
		if (stage.grassMaterial != null && stage.grassPerChunk > 0)
		{
			grassRenderer.Set(stage.grassMaterial, stage.grassPerChunk);
			grassRenderer.gameObject.SetActive(true);
		}
		if (water != null && stage.waterMaterial != null) { water.gameObject.SetActive(true); water.Set(map, stage); }
		if (proceduralMapWorldEdges != null && !stage.proceduralTerrainData.useFalloff)
		{
			proceduralMapWorldEdges.SetActive(true);
			proceduralMapWorldEdges.transform.localScale *= stage.proceduralMapScale;
		}
		colliderBox.transform.position = worldCenter + Vector3.down * (worldSize.y * 0.5f + 20f);
		colliderBox.transform.localScale = (worldSize + Vector3.up * 200f) * 1.55f;
		MapInfo.mapBoundsLower = worldCenter - worldSize * 0.5f;
		MapInfo.mapBoundsUpper = worldCenter + worldSize * 0.5f;
		MapInfo.mapCenter = worldCenter;
		MapInfo.mapSize = worldSize;
		stage.ApplyFogAndSky(sunLight);
		stage.SpawnParticles();
		minimapMesh.Set(worldMesh, stage.fogColor);
		isGenerating = false;
		A_GenerationComplete?.Invoke();
		UnityEngine.Debug.Log($"Procedural map generated: map={map.name}, stage={stage.name}, vertices={worldMesh.vertexCount}, bounds={bounds}, spawn={spawnPosition}, boss={bossPosition}");
	}

	private IEnumerator GenerateMap()
	{
		A_PreGeneration?.Invoke();
		isGenerating = true;
		mapSeed = UnityEngine.Random.Range(0, int.MaxValue);
		gameManager.CreateInstances();
		StageData stage = MapController.currentStage;
		MapData map = MapController.currentMap;
		cryptParent.SetActive(false);
		if (map.mapType == EMapType.ProceduralMesh)
		{
			yield return GenerateProceduralMap(map, stage);
			yield break;
		}
		Vector3 spawnPosition = proceduralTileGeneration.Generate(out Vector3 spawnDirection, stage, stage.mapParameters);
		tileSpawnPosition = spawnPosition;
		tileSpawnDir = spawnDirection;
		Vector3 worldSize = proceduralTileGeneration.GetWorldSize();
		Vector3 worldCenter = proceduralTileGeneration.GetWorldCenter();
		RandomTileObjects.Generate(proceduralTileGeneration.flatTiles, stage);
		Mesh worldMesh = proceduralTileGeneration.collider.GetComponent<MeshFilter>().sharedMesh;
		if (map.eMap == EMap.Graveyard) CryptGeneration(mapSeed, out spawnPosition, out spawnDirection);
		Instantiate(spawnPortal, spawnPosition, Quaternion.LookRotation(-spawnDirection)).GetComponent<SpawnPlayerPortal>().StartPortal();
		if (stage.mapEdgeFillType == MapEdgeFillType.None) mapEdges.Set(worldCenter, worldSize, stage);
		yield return null;
		Vector3 worldArea = new Vector3(worldSize.x, 1f, worldSize.z);
		Vector3 worldAreaCenter = worldCenter + Vector3.up * (worldSize.y * 0.5f + 50f);
		float worldAreaMagnitude = worldArea.x * worldArea.z;
		randomObjectPlacer.size = worldArea;
		randomObjectPlacer.center = worldAreaCenter;
		randomObjectPlacer.Generate(stage.randomMapObjects, 1f);
		yield return null;
		randomObjectPlacer.GenerateInteractables(map);
		yield return null;
		interactablesSpawner.SetArea(worldArea, worldAreaCenter, worldAreaMagnitude);
		interactablesSpawner.SpawnShit();
		if (stage.grassMaterial != null && stage.grassPerChunk > 0)
		{
			grassRenderer.Set(stage.grassMaterial, stage.grassPerChunk);
			grassRenderer.gameObject.SetActive(true);
		}
		colliderBox.transform.position = worldCenter + Vector3.down * (worldSize.y * 0.5f + 20f);
		colliderBox.transform.localScale = (worldSize + Vector3.up * 200f) * 1.55f;
		MapInfo.mapBoundsLower = worldCenter - worldSize * 0.5f;
		MapInfo.mapBoundsUpper = worldCenter + worldSize * 0.5f;
		MapInfo.mapCenter = worldCenter;
		MapInfo.mapSize = worldSize;
		stage.ApplyFogAndSky(sunLight);
		stage.SpawnParticles();
		minimapMesh.Set(worldMesh, stage.fogColor);
		isGenerating = false;
		A_GenerationComplete?.Invoke();
	}
}
