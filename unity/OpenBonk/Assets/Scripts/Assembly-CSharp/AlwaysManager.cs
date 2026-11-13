using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Utility;
using Assets.Scripts.Tools;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Inventory__Items__Pickups;
using UnityEngine;

public class AlwaysManager : MonoBehaviour
{
	public SaveManager saveManager;

	public DataManager dataManager;

	public SteamManager steamManager;

	public GameObject rewiredManager;

	public GameObject eventSystem;

	public AlwaysUi alwaysUi;

	public static AlwaysManager Instance;

	public Material playerMaterialPreset;

	private int index;

	private Enemy testEnemy;

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		DontDestroyOnLoad(gameObject);
		saveManager.Init();
		dataManager.Load();
		steamManager.Load();
		rewiredManager.SetActive(true);
		eventSystem.SetActive(true);
		EnemyTargeting.Init();
		XpUtility.Init();
		Progression.Init();
		MyTime.Init();
		MoneyUtility.Init();
		FxUtility.Init();
		Potato.Init();
		DebuffFactory.Init();
		InteractablesStatus.Init();
		SaveManager.Instance.Load(false);
	}

	private void Start()
	{
		WindowManager.RefreshCursor();
	}

	private void Update()
	{
		if (Instance != this) return;
		Potato.Update();
		WindowManager.Update();
		MyTime.Update();
		MyAchievements.Update();
	}

	private void FixedUpdate()
	{
		MyTime.FixedUpdate();
		ChallengesTracker.Tick();
	}

	private void OnDestroy()
	{
		if (Instance != this) return;
		MyTime.Cleanup();
		Progression.Cleanup();
		MoneyUtility.Cleanup();
		FxUtility.Cleanup();
		Potato.Cleanup();
		EnemyTargeting.Cleanup();
		DebuffFactory.Cleanup();
		InteractablesStatus.Cleanup();
	}
}
