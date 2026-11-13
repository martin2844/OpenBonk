using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public static class ObjectiveHudPortCheck
{
	private sealed class Provider : ITableProvider
	{
		public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
		{
			var table = ScriptableObject.CreateInstance<StringTable>();
			table.SharedData = ScriptableObject.CreateInstance<SharedTableData>(); table.SharedData.TableCollectionName = name; table.LocaleIdentifier = locale.Identifier;
			table.AddEntry("BOSS", "Defeat boss");
			var entry = table.AddEntry("KEYS", "Keys {current}/{target}"); entry.IsSmart = true;
			return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
		}
	}
	public static void Run()
	{
		GameObject root = new GameObject("objective test"); root.SetActive(false);
		GameObject playerObject = new GameObject("objective player"); playerObject.SetActive(false);
		var settings = ScriptableObject.CreateInstance<LocalizationSettings>(); var locale = Locale.CreateLocale("en");
		MyPlayer oldPlayer = MyPlayer.Instance; ItemInventory inventory = null; ObjectivePrefabUi current = null;
		try
		{
			LocalizationSettings.Instance = settings; LocalizationSettings.SelectedLocale = locale; LocalizationSettings.StringDatabase.TableProvider = new Provider();
			MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
			MyPlayer.Instance.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
			inventory = new ItemInventory(); MyPlayer.Instance.inventory.itemInventory = inventory;
			var item = ItemFactory.CreateItem(EItem.CryptKey, inventory); item.amount = 2; inventory.items.Add(EItem.CryptKey, item);
			ObjectiveUi ui = root.AddComponent<ObjectiveUi>();
			ui.a_new = Child(root, "new audio").AddComponent<AudioSource>(); ui.a_complete = Child(root, "complete audio").AddComponent<AudioSource>();
			GameObject prefabObject = new GameObject("objective prefab", typeof(RectTransform)); prefabObject.transform.SetParent(root.transform); prefabObject.SetActive(false);
			ObjectivePrefabUi prefab = prefabObject.AddComponent<ObjectivePrefabUi>();
			prefab.checkBox = Child(prefabObject, "checkbox"); prefab.checkMark = Child(prefabObject, "checkmark"); prefab.checkMark.SetActive(false);
			prefab.t_objective = new GameObject("text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); prefab.t_objective.transform.SetParent(prefabObject.transform);
			prefab.content = new GameObject("content", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<HorizontalLayoutGroup>(); prefab.content.transform.SetParent(prefabObject.transform);
			prefab.textSizer = prefabObject.AddComponent<TextSizer>(); prefab.overlay = new GameObject("overlay", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); prefab.overlay.transform.SetParent(prefabObject.transform);
			ui.objectivePrefab = prefabObject;
			IEnumerator animation = (IEnumerator)typeof(ObjectiveUi).GetMethod("AnimateObjective", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, new object[] { new LocalizedString("Objectives", "KEYS"), true, EObjective.CryptKeys });
			Check(animation.MoveNext() && animation.Current is WaitForSeconds, "objective waits before appearing");
			Check(!animation.MoveNext(), "objective is created after delay");
			current = (ObjectivePrefabUi)typeof(ObjectiveUi).GetField("currentObjective", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
			Check(current != null && current.gameObject.activeSelf && current.checkBox.activeSelf && current.t_objective.text == "Keys 2/4", "localized key counter");
			typeof(ObjectivePrefabUi).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(current, null);
			item.amount = 3; ItemInventory.A_ItemAdded?.Invoke(EItem.CryptKey);
			Check(current.t_objective.text == "Keys 3/4", "key counter refreshes on pickup");
			ui.CompleteCurrentObjective();
			Check(current.checkMark.activeSelf && typeof(ObjectiveUi).GetField("currentObjective", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui) == null, "completed objective displays checkmark");
			Debug.Log("ObjectiveHudPortCheck passed");
		}
		finally
		{
			if (current != null) typeof(ObjectivePrefabUi).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(current, null);
			inventory?.Cleanup(); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(playerObject);
			MyPlayer.Instance = oldPlayer; LocalizationSettings.Instance = null; UnityEngine.Object.DestroyImmediate(settings); UnityEngine.Object.DestroyImmediate(locale);
		}
	}
	private static GameObject Child(GameObject root, string name) { var child = new GameObject(name); child.transform.SetParent(root.transform); return child; }
	private static void Check(bool value, string name) { if (!value) throw new Exception("ObjectiveHudPortCheck failed: " + name); }
}
