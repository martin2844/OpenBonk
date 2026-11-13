using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public static class AchievementUnlockPortCheck
{
    private sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = ScriptableObject.CreateInstance<StringTable>();
            table.SharedData = ScriptableObject.CreateInstance<SharedTableData>(); table.SharedData.TableCollectionName = name; table.LocaleIdentifier = locale.Identifier;
            var entry = table.AddEntry("DESCRIPTION", "Defeat {value} frogs"); entry.IsSmart = true;
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    public static void Run()
    {
        var settings = ScriptableObject.CreateInstance<LocalizationSettings>(); var locale = Locale.CreateLocale("en");
        LocalizationSettings.Instance = settings; LocalizationSettings.SelectedLocale = locale; LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var root = new GameObject("achievement check"); root.SetActive(false);
        var eggPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/QuestEggBlue.prefab");
        var achievement = UnityEngine.Object.Instantiate(eggPrefab.GetComponentInChildren<InteractableEgg>(true).unlockAchievement);
        achievement.localizedDescription = new LocalizedString("QuestTest", "DESCRIPTION"); achievement.targetValue = 3; achievement.statName = ""; achievement.serializedLocalizationKeys = new List<LocalizationKey>();
        var item = ScriptableObject.CreateInstance<ItemData>(); item.eItem = EItem.CryptKey;
        SaveManager oldSave = SaveManager.Instance; DataManager oldData = DataManager.Instance;
        var pathField = typeof(SaveManager).GetField("defaultSavesPath", BindingFlags.Static | BindingFlags.NonPublic);
        string oldPath = (string)pathField.GetValue(null); bool oldLoaded = SaveManager.loaded;
        const string steamIdKey = "saves_last_steamid";
        bool hadSteamId = PlayerPrefs.HasKey(steamIdKey); string previousSteamId = PlayerPrefs.GetString(steamIdKey, "");
        var steamIdField = typeof(SaveManager).GetField("steamIdSave", BindingFlags.Static | BindingFlags.NonPublic); ulong oldSteamId = (ulong)steamIdField.GetValue(null);
        string tempPath = Path.Combine(Path.GetTempPath(), "megabonk-achievement-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            var menuScene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Additive);
            MyButtonQuest originalButton = null;
            QuestsWindowNew originalWindow = null;
            foreach (var sceneRoot in menuScene.GetRootGameObjects())
            {
                foreach (var candidate in sceneRoot.GetComponentsInChildren<MyButtonQuest>(true))
                    if (candidate.claimOverlay != null && candidate.rewardContainer != null && candidate.i_completeCheck != null && candidate.t_claimAmount != null) { originalButton = candidate; break; }
                foreach (var candidate in sceneRoot.GetComponentsInChildren<QuestsWindowNew>(true))
                    if (candidate.tabVertNavigation != null && candidate.tabNavigation != null && candidate.tabPrefab != null && candidate.questPrefab != null && candidate.toggleHideCompleted != null) { originalWindow = candidate; break; }
            }
            Require(originalButton != null, "original quest button exists");
            Require(originalWindow != null, "original quest window exists");
            originalButton.gameObject.SetActive(false);
            var questObject = UnityEngine.Object.Instantiate(originalButton.gameObject); questObject.transform.SetParent(root.transform); questObject.SetActive(false);
            var questButton = questObject.GetComponent<MyButtonQuest>();
            originalWindow.gameObject.SetActive(false);
            var menuWindowObject = UnityEngine.Object.Instantiate(originalWindow.gameObject); menuWindowObject.transform.SetParent(root.transform); menuWindowObject.SetActive(false);
            var menuWindow = menuWindowObject.GetComponent<QuestsWindowNew>();
            Require(questButton.claimOverlay != null && questButton.rewardContainer != null && questButton.i_completeCheck != null && questButton.t_claimAmount != null, "quest button references");
            EditorSceneManager.CloseScene(menuScene, true);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            save.stats = new StatsSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            dict.Add(achievement.internalName, achievement); achievement.SetUnlockable(item);
            achievement.isHidden = true;
            Require(!achievement.IsVisible() && achievement.IsHiddenInMenus(), "hidden quest is concealed before unlock");
            achievement.isHidden = false;
            Require(achievement.IsVisible(), "enabled general quest is visible");
            achievement.isEnabled = false; Require(!achievement.IsVisible(), "disabled quest is concealed"); achievement.isEnabled = true;
            var map = ScriptableObject.CreateInstance<MapData>(); map.isEnabled = true;
            var challenge = ScriptableObject.CreateInstance<ChallengeData>(); challenge.isEnabled = true; challenge.achievementType = EAchievementType.Challenges; challenge.map = map.eMap; challenge.tier = 2;
            data.maps = new List<MapData> { map };
            Require(data.GetMap(map.eMap) == map && !challenge.IsVisible(), "challenge is concealed before tier completion");
            save.progression.menuMeta.SetTierCompletion(map.eMap, 2);
            Require(challenge.IsVisible(), "challenge is visible after tier completion");
            map.isEnabled = false; Require(!challenge.IsVisible(), "challenge is concealed when its map is unavailable");
            UnityEngine.Object.DestroyImmediate(challenge); UnityEngine.Object.DestroyImmediate(map);
            var toggleObject = new GameObject("toggle check"); var toggle = toggleObject.AddComponent<MyButtonToggle>(); toggle.toggleIcon = new GameObject("toggle icon"); toggle.toggleIcon.transform.SetParent(toggleObject.transform);
            int toggled = 0; toggle.A_Toggled += value => { if (value) toggled++; };
            toggle.Set(false); Require(!toggle.IsOn(), "toggle state set without notification");
            typeof(MyButtonToggle).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(toggle, null);
            Require(toggle.IsOn() && toggled == 1, "toggle click changes state and emits notification");
            toggleObject.SetActive(false); Require(!toggle.IsOn(), "toggle respects inactive parent"); toggleObject.SetActive(true);
            var window = root.AddComponent<QuestsWindowNew>();
            var typeIndex = typeof(QuestsWindowNew).GetMethod("GetAchievementTypeIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            int[] expectedOrder = { 4, 0, 1, 5, 3, 2, 6, 7 };
            for (int i = 0; i < expectedOrder.Length; i++) Require((int)typeIndex.Invoke(window, new object[] { i }) == expectedOrder[i], "original tab order " + i);
            Require((int)typeIndex.Invoke(window, new object[] { 8 }) == -1, "invalid tab order");
            var navObject = new GameObject("navigation check");
            var navigation = navObject.AddComponent<ButtonNavigationSelectionOnly>(); navigation.cooldown = 0f;
            var tabs = new List<MyButtonTabs>();
            for (int i = 0; i < 2; i++)
            {
                var tabObject = new GameObject("tab " + i, typeof(RectTransform), typeof(Image), typeof(Button)); tabObject.transform.SetParent(navObject.transform);
                var tab = tabObject.AddComponent<MyButtonTabs>(); tab.background = tabObject.GetComponent<Image>(); tab.defaultColor = Color.red; tab.selectedColor = Color.green;
                tab.associatedContent = new GameObject("content " + i); tab.associatedContent.transform.SetParent(navObject.transform); tab.associatedContent.SetActive(false);
                tabs.Add(tab);
            }
            int selectedTab = -1; navigation.A_ButtonSelected += value => selectedTab = value;
            navigation.ButtonPressed(0, true);
            Require(navigation.GetSelectedButton() == tabs[0].GetButton() && tabs[0].associatedContent.activeSelf && selectedTab == 0, "initial tab selection");
            tabs[1].GetButton().onClick.Invoke();
            Require(navigation.GetSelectedButton() == tabs[1].GetButton() && !tabs[0].associatedContent.activeSelf && tabs[1].associatedContent.activeSelf && selectedTab == 1, "tab click changes selection and content");
            var listObject = new GameObject("vertical buttons"); listObject.transform.SetParent(navObject.transform);
            var first = new GameObject("first", typeof(RectTransform), typeof(Image), typeof(Button)); first.transform.SetParent(listObject.transform);
            var skipped = new GameObject("inactive", typeof(RectTransform), typeof(Image), typeof(Button)); skipped.transform.SetParent(listObject.transform); skipped.SetActive(false);
            var last = new GameObject("last", typeof(RectTransform), typeof(Image), typeof(Button)); last.transform.SetParent(listObject.transform);
            var vertical = navObject.AddComponent<TabVerticalNavigation>(); vertical.buttonsParent = listObject; vertical.Set(tabs[1].GetButton());
            Require(first.GetComponent<Button>().navigation.selectOnUp == tabs[1].GetButton() && first.GetComponent<Button>().navigation.selectOnDown == last.GetComponent<Button>() && last.GetComponent<Button>().navigation.selectOnUp == first.GetComponent<Button>(), "vertical navigation skips inactive buttons");
            UnityEngine.Object.DestroyImmediate(navObject);
            var otherQuest = UnityEngine.Object.Instantiate(achievement); otherQuest.internalName = "quest_test_other"; otherQuest.achievementType = EAchievementType.General; otherQuest.SetUnlockable(item);
            var hiddenQuest = UnityEngine.Object.Instantiate(achievement); hiddenQuest.internalName = "quest_test_hidden"; hiddenQuest.achievementType = EAchievementType.General; hiddenQuest.isHidden = true; hiddenQuest.SetUnlockable(item);
            achievement.achievementType = EAchievementType.General;
            data.unsortedAchievements = new List<MyAchievement> { achievement, otherQuest, hiddenQuest };
            save.progression.achievements.Add(otherQuest.internalName);
            MyAchievements.GetAchievementTypeProgress(EAchievementType.General, out int completedCount, out int totalCount, out int unclaimedCount);
            Require(completedCount == 1 && totalCount == 2 && unclaimedCount == 1, "quest tab counts visible completed and unclaimed entries");
            menuWindow.localizedGeneral = new LocalizedString("QuestTest", "DESCRIPTION");
            typeof(QuestsWindowNew).GetMethod("CreateTabs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menuWindow, null);
            var createdTabs = (List<EAchievementType>)typeof(QuestsWindowNew).GetField("tabsEnums", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menuWindow);
            Require(createdTabs.Count == 1 && createdTabs[0] == EAchievementType.General, "original quest window creates a visible general tab");
            var originalTab = menuWindow.tabPrefab.GetComponent<MyButtonTabsQuest>();
            Require(originalTab.t_progress.text == "1 / 2" && originalTab.t_unlcaimed.text == "1" && originalTab.unclaimedUi.activeSelf, "original quest tab shows counts");
            window.questPrefab = questObject; window.toggleHideCompleted = toggle;
            var refresh = typeof(QuestsWindowNew).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
            refresh.Invoke(window, null);
            var buttons = (List<MyButtonQuest>)typeof(QuestsWindowNew).GetField("questButtons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
            Require(buttons.Count == 2 && buttons[0].achievement == otherQuest && buttons[1].achievement == achievement && buttons[0].gameObject.activeSelf && buttons[1].gameObject.activeSelf, "refresh populates visible quests and puts unclaimed quest first");
            data.unsortedAchievements.Remove(otherQuest); refresh.Invoke(window, null);
            Require(buttons.Count == 2 && buttons[0].achievement == achievement && buttons[0].gameObject.activeSelf && !buttons[1].gameObject.activeSelf, "refresh reuses and hides surplus buttons");
            save.progression.achievements.Remove(otherQuest.internalName);
            UnityEngine.Object.DestroyImmediate(toggleObject);
            data.unsortedAchievements.Remove(hiddenQuest);
            UnityEngine.Object.DestroyImmediate(otherQuest); UnityEngine.Object.DestroyImmediate(hiddenQuest);
            int unlocks = 0, attempts = 0; string order = "";
            Action<string> tried = value => { attempts++; order += "t"; };
            Action<MyAchievement> unlocked = value => { unlocks++; order += "u"; };
            MyAchievements.A_TryUnlock += tried; MyAchievements.A_Unlocked += unlocked;
            MyAchievements.Init();
            questButton.Set(achievement); questButton.TryClaimButton();
            Require(questButton.t_description.text == "Defeat 3 frogs" && questButton.t_progress.text == "0 / 1" && questButton.i_progressBar.transform.localScale.x == 0f && questButton.i_icon.color == Color.black, "incomplete quest presentation");
            achievement.statName = "quest_test_progress";
            Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.SetValueForce(achievement.statName, 1f);
            questButton.Set(achievement);
            Require(questButton.t_progress.text == "1/3" && Mathf.Approximately(questButton.i_progressBar.transform.localScale.x, 1f / 3f), "tracked quest progress");
            achievement.statName = "";
            Require(!achievement.IsClaimed() && save.progression.silver == 0, "incomplete quest cannot be claimed");
            Require(MyAchievements.TryUnlock(achievement.internalName), "first unlock succeeds");
            Require(save.progression.achievements.Contains(achievement.internalName) && save.progression.newUnlockables.Contains(item.GetInternalName()) && save.progression.hasNewQuestDone, "progression records quest and unlockable");
            Require(order == "tu" && unlocks == 1 && attempts == 1, "events dispatched in original order");
            Require(!MyAchievements.TryUnlock(achievement.internalName) && !MyAchievements.TryUnlock("unknown-achievement"), "duplicates and unknown entries rejected");
            Require((bool)typeof(MyAchievements).GetField("hasUnsavedChanges", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), "progression marked dirty");
            pathField.SetValue(null, tempPath); SaveManager.loaded = true;
            string progressionPath = (string)typeof(SaveManager).GetMethod("GetProgressionPath", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(save, null);
            Directory.CreateDirectory(Path.GetDirectoryName(progressionPath));
            typeof(MyAchievements).GetField("nextSaveTimeReady", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, 0f);
            MyTime.A_Pause?.Invoke(true);
            Require(File.Exists(progressionPath), "pause writes isolated progression file");
            var reloaded = JsonConvert.DeserializeObject<ProgressionSaveFile>(SimpleEncryptor.Decrypt(File.ReadAllText(progressionPath)));
            Require(reloaded.achievements.Contains(achievement.internalName) && reloaded.newUnlockables.Contains(item.GetInternalName()) && reloaded.hasNewQuestDone, "saved progression round-trips");
            Require(!(bool)typeof(MyAchievements).GetField("hasUnsavedChanges", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), "save event clears dirty state");
            save.progression = null; SaveManager.loaded = false;
            Require(save.Load(oldSteamId, false, out string loadError, out string loadPath), "game load succeeds: " + loadError + " at " + loadPath);
            Require(save.progression.achievements.Contains(achievement.internalName) && save.progression.newUnlockables.Contains(item.GetInternalName()) && save.progression.hasNewQuestDone, "game load restores achievement");
            Require(achievement.IsCompleted() && !achievement.IsClaimed() && save.progression.HasUnclaimedQuests(), "unclaimed completed quest visible");
            foreach (var pair in new[] { (EAchievementDifficulty.Easy, 1), (EAchievementDifficulty.Medium, 2), (EAchievementDifficulty.Hard, 4), (EAchievementDifficulty.Cooked, 8) }) { achievement.difficulty = pair.Item1; Require(achievement.GetSilverReward() == pair.Item2, "silver reward for " + pair.Item1); }
            int claimEvents = 0; Action<MyAchievement> claimed = value => claimEvents++; ProgressionSaveFile.A_AchievementClaimed += claimed;
            questButton.Set(achievement); Require(questButton.claimOverlay.activeSelf, "original button shows claim overlay");
            Require(questButton.t_progress.text == "1 / 1" && questButton.i_progressBar.transform.localScale.x == 1f && questButton.i_icon.color == Color.white && questButton.i_colorOutline.color == MyColorUtility.DifficultyToColor(EAchievementDifficulty.Cooked), "completed quest presentation");
            questButton.TryClaimButton(); questButton.TryClaimButton();
            Require(achievement.IsClaimed() && !save.progression.HasUnclaimedQuests() && save.progression.silver == 8 && claimEvents == 1, "claim records reward and notifies");
            Require(!questButton.claimOverlay.activeSelf, "claim overlay closes");
            ProgressionSaveFile.A_AchievementClaimed -= claimed;
            save.SaveProgression(); save.progression = null; SaveManager.loaded = false;
            Require(save.Load(oldSteamId, false, out loadError, out loadPath), "game reload after claim succeeds: " + loadError);
            Require(achievement.IsClaimed() && save.progression.silver == 8 && !save.progression.HasUnclaimedQuests(), "claim survives reload");
            MyAchievements.Cleanup(); MyAchievements.A_TryUnlock -= tried; MyAchievements.A_Unlocked -= unlocked;
            Debug.Log("AchievementUnlockPortCheck passed");
        }
        finally { if (hadSteamId) PlayerPrefs.SetString(steamIdKey, previousSteamId); else PlayerPrefs.DeleteKey(steamIdKey); steamIdField.SetValue(null, oldSteamId); pathField.SetValue(null, oldPath); SaveManager.loaded = oldLoaded; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); DataManager.Instance = oldData; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(achievement); UnityEngine.Object.DestroyImmediate(item); LocalizationSettings.Instance = null; UnityEngine.Object.DestroyImmediate(settings); UnityEngine.Object.DestroyImmediate(locale); if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true); }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception("AchievementUnlockPortCheck failed: " + message); }
}
