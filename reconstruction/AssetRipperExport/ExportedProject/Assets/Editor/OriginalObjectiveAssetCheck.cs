using System;
using Assets.Scripts.UI.HUD;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public static class OriginalObjectiveAssetCheck
{
	private sealed class Provider : ITableProvider
	{
		public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
		{
			var table = ScriptableObject.CreateInstance<StringTable>();
			table.SharedData = ScriptableObject.CreateInstance<SharedTableData>(); table.SharedData.TableCollectionName = name; table.LocaleIdentifier = locale.Identifier;
			table.AddEntry("BOSS", "Defeat the boss");
			return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
		}
	}
	public static void Run()
	{
		var settings = ScriptableObject.CreateInstance<LocalizationSettings>(); var locale = Locale.CreateLocale("en"); GameObject clone = null;
		try
		{
			LocalizationSettings.Instance = settings; LocalizationSettings.SelectedLocale = locale; LocalizationSettings.StringDatabase.TableProvider = new Provider();
			EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
			ObjectiveUi ui = UnityEngine.Object.FindAnyObjectByType<ObjectiveUi>(FindObjectsInactive.Include);
			Check(ui != null && ui.objectivePrefab != null && ui.a_new != null && ui.a_complete != null, "original objective controller references");
			ObjectivePrefabUi original = ui.objectivePrefab.GetComponent<ObjectivePrefabUi>();
			Check(original != null && original.checkBox != null && original.checkMark != null && original.t_objective != null && original.content != null && original.textSizer != null && original.overlay != null, "original objective prefab references");
			Check(original.textSizer.Text != null && original.textSizer.GetComponent<RectTransform>() != null, "original text sizing references");
			clone = UnityEngine.Object.Instantiate(ui.objectivePrefab, ui.objectivePrefab.transform.parent);
			ObjectivePrefabUi entry = clone.GetComponent<ObjectivePrefabUi>();
			entry.Set(new LocalizedString("OriginalObjectiveCheck", "BOSS"), true);
			entry.textSizer.Refresh(); entry.textSizer.Recalculate();
			Check(entry.t_objective.text == "Defeat the boss" && entry.checkBox.activeSelf, "actual objective prefab accepts localized text");
			Check(entry.textSizer.GetComponent<RectTransform>().rect.width > 0f && entry.textSizer.GetComponent<RectTransform>().rect.height > 0f, "actual objective has positive measured size");
			entry.Complete(); Check(entry.checkMark.activeSelf, "actual objective completion checkmark");
			RsgController crypt = UnityEngine.Object.FindAnyObjectByType<RsgController>(FindObjectsInactive.Include);
			Check(crypt != null && crypt.roomBoss != null, "original boss room reference");
			GraveyardBossRoom room = crypt.roomBoss;
			Check(room.playerTeleportTransform != null && room.bossSpawnTransform != null && room.interactableGhostBossLeave != null && room.directionalLight != null && room.bossShields != null && room.metalGate != null, "original boss, light, shield and exit references");
			Check(room.lava != null && room.risingObjects != null && room.pillars != null && room.sfxRumble != null, "original lava-cycle references");
			Check(room.lamps != null && room.lamps.Length > 0, "original boss lamps");
			foreach (BossLamp lamp in room.lamps)
			{
				Check(lamp != null && lamp.zoneRenderer != null && lamp.zoneRenderer.GetComponent<Collider>() != null && lamp.lampRenderer != null && lamp.light != null, "original lamp geometry and light");
				Check(lamp.minimapIcon != null && lamp.altarIcon != null && lamp.circleProgress != null && lamp.circleParent != null && lamp.finishedFx != null && lamp.randomGoOutEffect != null, "original lamp UI and effects");
				Check(lamp.audioStart != null && lamp.audioLoop != null && lamp.audioComplete != null && lamp.audioAbort != null && lamp.lampExplosionPrefab != null, "original lamp audio and blast");
				Check(lamp.finishedFx.ps != null && lamp.randomGoOutEffect.ps != null, "original lamp particle references");
			}
			Check(room.bossShields.prefab != null && room.bossShields.transform.parent != null && room.metalGate.uiArrowTarget != null && room.metalGate.sfxOpen != null, "original shield and gate assets");
			Debug.Log("OriginalObjectiveAssetCheck passed: prefab references, localized text, size and completion");
		}
		finally
		{
			if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
			LocalizationSettings.Instance = null; UnityEngine.Object.DestroyImmediate(settings); UnityEngine.Object.DestroyImmediate(locale);
		}
	}
	private static void Check(bool value, string name) { if (!value) throw new Exception("OriginalObjectiveAssetCheck failed: " + name); }
}
