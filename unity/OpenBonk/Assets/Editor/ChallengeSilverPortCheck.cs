using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;
using UnityEngine;

public static class ChallengeSilverPortCheck
{
	public static void Run()
	{
		RunConfig oldConfig = MapController.runConfig;
		int oldIndex = MapController.index;
		DataManager oldData = DataManager.Instance;
		Action<RunConfig> oldChanged = MapSelectionUi.A_RunConfigChanged;
		FieldInfo initializedField = typeof(ChallengesTracker).GetField("initializedInventory", BindingFlags.Static | BindingFlags.NonPublic);
		object oldInitialized = initializedField.GetValue(null);
		initializedField.SetValue(null, null);
		GameObject menuObject = new GameObject("challenge selection check");
		menuObject.SetActive(false);
		ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();
		try
		{
			RunConfig config = new RunConfig();
			Check(config.musicTrackIndex == -1 && config.GetSilverMultiplier() == 1f, "default run multiplier");
			config.mapTierIndex = 1;
			Check(Mathf.Approximately(config.GetSilverMultiplier(), 1.1f), "tier one multiplier");
			config.mapTierIndex = 2;
			Check(Mathf.Approximately(config.GetSilverMultiplier(), 1.2f), "tier two multiplier");
			challenge.silverMultiplier = 1.35f;
			MapSelectionUi menu = menuObject.AddComponent<MapSelectionUi>();
			menu.runConfig = config;
			int changes = 0;
			MapSelectionUi.A_RunConfigChanged = updated => { if (updated == config) changes++; };
			menu.SetChallenge(challenge);
			Check(config.challenge == challenge && changes == 1, "challenge selection event");
			Check(Mathf.Approximately(config.GetSilverMultiplier(), 1.55f) && config.GetFormattedSilverMultiplier() == "1.55", "challenge reward multiplier");
			Check(MyStringUtil.ShowOnlyDecimals(1f) == "1" && MyStringUtil.ShowOnlyDecimals(1.1f) == "1.1", "trimmed reward formatting");
			MapController.runConfig = config;
			MapController.index = 0;
			DataManager.Instance = null;
			PlayerInventory inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
			inventory.statInventory = new StatInventory();
			MethodInfo init = typeof(ChallengesTracker).GetMethod("OnInventoryInitialized", BindingFlags.Static | BindingFlags.NonPublic);
			init.Invoke(null, new object[] { inventory });
			var changesToSilver = inventory.statInventory.permanentChanges[EStat.SilverIncreaseMultiplier];
			Check(changesToSilver.Count == 1 && changesToSilver[0].modifyType == EStatModifyType.Multiplication && Mathf.Approximately(changesToSilver[0].modification, 1.55f), "first stage applies reward multiplier");
			init.Invoke(null, new object[] { inventory });
			Check(changesToSilver.Count == 1, "reinitializing the same inventory on stage zero does not duplicate multiplier");
			MapController.index = 1;
			init.Invoke(null, new object[] { inventory });
			Check(changesToSilver.Count == 1, "later stage does not duplicate multiplier");
			menu.SetChallenge(null);
			Check(config.challenge == null && changes == 2, "clear challenge selection");
			Debug.Log("ChallengeSilverPortCheck passed");
		}
		finally
		{
			MapController.runConfig = oldConfig; MapController.index = oldIndex;
			DataManager.Instance = oldData; MapSelectionUi.A_RunConfigChanged = oldChanged;
			initializedField.SetValue(null, oldInitialized);
			UnityEngine.Object.DestroyImmediate(menuObject); UnityEngine.Object.DestroyImmediate(challenge);
		}
	}
	private static void Check(bool value, string description) { if (!value) throw new Exception("ChallengeSilverPortCheck failed: " + description); }
}
