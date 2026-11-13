using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Inventory__Items__Pickups.Xp_and_Levels;

[InitializeOnLoad]
public static class UnlocksMenuPlayCheck {
    const string Pending="UnlocksMenuPlayCheck.pending", Path="Assets/UnlocksMenuCheckTemporary.unity";
    static UnlocksMenuPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() {
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity",OpenSceneMode.Single);
        var original=UnityEngine.Object.FindAnyObjectByType<UnlocksUi>(FindObjectsInactive.Include);
        if(original==null || original.unlockContainerPrefab==null || original.tabButtons==null || original.contentParent==null) throw new Exception("Original unlock UI binding missing");
        var characterMenu=UnityEngine.Object.FindAnyObjectByType<CharacterMenu>(FindObjectsInactive.Include);
        if(characterMenu==null || characterMenu.mainMenu==null || characterMenu.shopUi!=original || characterMenu.b_purchase==null) throw new Exception("Original character purchase references missing");
        var purchaseButton=characterMenu.b_purchase.GetComponent<UnityEngine.UI.Button>();bool bound=false;
        for(int i=0;i<purchaseButton.onClick.GetPersistentEventCount();i++)if(purchaseButton.onClick.GetPersistentTarget(i)==characterMenu && purchaseButton.onClick.GetPersistentMethodName(i)=="BuyCharacter")bound=true;
        if(!bound)throw new Exception("Original character Buy button is not bound to BuyCharacter");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var clone=UnityEngine.Object.Instantiate(original.gameObject); clone.SetActive(false); SceneManager.MoveGameObjectToScene(clone,scene);
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source,true); EditorSceneManager.SaveScene(scene,Path);
        SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s) { if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("runner").AddComponent<UnlocksMenuPlayRunner>().Begin(); } }
}
public class UnlocksMenuPlayRunner:MonoBehaviour {
    sealed class EnglishProvider : ITableProvider {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable {
            var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T,null);
        }
    }
    sealed class LocaleProvider : ITableProvider {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable {
            var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_"+locale.Identifier.Code+".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T,null);
        }
    }
    static void Check(bool ok,string msg) { if(!ok) throw new Exception("UnlocksMenuPlayCheck: "+msg); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test=Run(); while(true) { bool more; try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;} }
    IEnumerator Run() {
        LocalizationSettings.StringDatabase.TableProvider=new EnglishProvider(); LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");
        var state=new GameObject("fixture state");state.SetActive(false); var save=state.AddComponent<SaveManager>();save.progression=new ProgressionSaveFile();save.stats=new StatsSaveFile();save.config=new ConfigSaveFile();save.progression.silver=1000;
        foreach(EShopItem item in Enum.GetValues(typeof(EShopItem))) save.progression.shopItems[item]=0;
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var manager=state.AddComponent<DataManager>();DataManager.Instance=manager;
        var fox=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        var gun=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset");
        var shotgun=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponShotgun.asset");
        var clank=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Clank.asset");
        Check(fox!=null && clank!=null && gun!=null && shotgun!=null && gun.GetUnlockRequirement()!=null && clank.weapon==gun,"original unlockable data");
        manager.unsortedCharacterData=new List<CharacterData>{fox,clank};manager.unsortedWeapons=new List<WeaponData>{gun,shotgun};manager.unsortedTomes=new List<TomeData>();manager.unsortedItems=new List<ItemData>();manager.unsortedHats=new List<HatData>();
        foreach(var assetName in new[]{"Refresh","Skip","Banish"}) { var shop=AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/"+assetName+".asset"); manager.shopItems.Add(shop.eShopItem,shop); }
        save.progression.achievements.Add(gun.GetUnlockRequirement().internalName); if(clank.GetUnlockRequirement()!=null) save.progression.achievements.Add(clank.GetUnlockRequirement().internalName); save.progression.newUnlockables.Add(gun.GetInternalName());
        var ui=UnityEngine.Object.FindAnyObjectByType<UnlocksUi>(FindObjectsInactive.Include); var footer=ui.GetComponentInChildren<UnlocksFooter>(true);
        Check(footer!=null && footer.buyButton!=null && footer.buyContainer!=null,"original unlock footer bindings");
        Check(!save.progression.menuMeta.hasVisitedUnlocks, "unlocks initially unvisited");
        ui.gameObject.SetActive(true); yield return null;
        Check(save.progression.menuMeta.hasVisitedUnlocks, "opening original unlocks window records visit");
        UnlockContainer beforeClank=null; foreach(var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true)) if(card.gameObject.activeSelf && card.unlockable==clank) beforeClank=card;
        Check(beforeClank!=null && !beforeClank.isUnlocked,"Clank is locked before his weapon is purchased");
        ui.tabButtons.ButtonPressed(1,true); yield return null;
        UnlockContainer gunCard=null, lockedCard=null; int active=0; foreach(var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true)) if(card.gameObject.activeSelf){active++;if(card.unlockable==gun)gunCard=card;if(card.unlockable==shotgun)lockedCard=card;}
        Check(active==2 && gunCard!=null && gunCard.isUnlocked && !gunCard.isPurchased,"weapons tab populated");
        var button=gunCard.GetComponent<MyButtonUnlock>(); Check(button!=null && button.unlockContainer==gunCard,"original weapon button binding");
        var events=new GameObject("events").AddComponent<EventSystem>();
        Check(lockedCard!=null && !lockedCard.isUnlocked,"locked weapon card");
        events.SetSelectedGameObject(lockedCard.gameObject); Check(footer.requirementsContainer.gameObject.activeSelf && !footer.buyContainer.activeSelf,"locked weapon shows requirements");
        var lockedAchievement=shotgun.GetUnlockRequirement();
        Check(lockedAchievement!=null,"locked weapon has an achievement requirement");
        var requirementRow=footer.requirementsContainer.reqContainers[0];
        Check(requirementRow.gameObject.activeSelf && requirementRow.t_requirement.text==lockedAchievement.GetUnlockRequirement() && !string.IsNullOrWhiteSpace(requirementRow.t_requirement.text),"English locked weapon requirement displayed");
        LocalizationSettings.StringDatabase.TableProvider=new LocaleProvider(); LocalizationSettings.SelectedLocale=Locale.CreateLocale("es");
        events.SetSelectedGameObject(null); events.SetSelectedGameObject(lockedCard.gameObject);
        Check(requirementRow.gameObject.activeSelf && requirementRow.t_requirement.text==lockedAchievement.GetUnlockRequirement() && !string.IsNullOrWhiteSpace(requirementRow.t_requirement.text),"Spanish locked weapon requirement displayed");
        Debug.Log("UnlocksMenuPlayCheck Spanish requirement: " + requirementRow.t_requirement.text);
        LocalizationSettings.StringDatabase.TableProvider=new EnglishProvider(); LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");
        events.SetSelectedGameObject(gunCard.gameObject);
        Check(!save.progression.newUnlockables.Contains(gun.GetInternalName()) && footer.buyContainer.activeInHierarchy,"selection updates footer and alert");
        var info=footer.extraInformation;
        Check(info!=null && !info.gameObject.activeSelf,"unbought weapon hides upgrade information");
        Check(ChallengesUi.IsNegativeModifier(new StatModifier{stat=EStat.MaxHealth,modifyType=EStatModifyType.Addition,modification=-.1f}) && !ChallengesUi.IsNegativeModifier(new StatModifier{stat=EStat.MaxHealth,modifyType=EStatModifyType.Multiplication,modification=1.2f}),"normal stat modifier signs");
        Check(ChallengesUi.IsNegativeModifier(new StatModifier{stat=EStat.ChestPriceMultiplier,modifyType=EStatModifyType.Multiplication,modification=1.2f}) && !ChallengesUi.IsNegativeModifier(new StatModifier{stat=EStat.EnemyHpMultiplier,modifyType=EStatModifyType.Multiplication,modification=.8f}),"opposite stat modifier signs");
        yield return new WaitForSeconds(.2f); button.GetButton().onClick.Invoke(); Check(events.currentSelectedGameObject==footer.buyButton.gameObject,"card click focuses buy");
        int price=gun.GetPrice();footer.buyButton.GetButton().onClick.Invoke();
        Check(save.progression.purchases.Contains(gun.GetInternalName()) && save.progression.silver==1000-price && gunCard.isPurchased && !footer.buyContainer.activeSelf,"bound purchase updates card and save");
        Check(info!=null && info.gameObject.activeSelf && info.t_info.text.Contains(LocalizationUtility.GetStatName(EStat.AttackSpeed)),"weapon upgrade information");
        foreach(var modifier in gun.upgradeData.upgradeModifiers) Check(info.t_info.text.Contains(LocalizationUtility.GetStatName(modifier.stat)),"weapon upgrade stat " + modifier.stat);
        ui.tabButtons.ButtonPressed(0,true);yield return null;
        active=0;bool foundFox=false,foundClank=false;foreach(var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true)) if(card.gameObject.activeSelf){active++;foundFox|=card.unlockable==fox;if(card.unlockable==clank) foundClank=card.isUnlocked;}
        Check(active==2 && foundFox && foundClank,"tab switch refreshes Clank after Revolver purchase");
        events.SetSelectedGameObject(null);
        foreach(var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true)) if(card.gameObject.activeSelf && card.unlockable==fox) events.SetSelectedGameObject(card.gameObject);
        Check(info.gameObject.activeSelf && info.t_infoNumbers.gameObject.activeSelf && info.t_info.text.Contains("MaxHealth") && info.t_info.text.Contains("MoveSpeedMultiplier") && info.t_info.text.Contains("JumpHeight") && info.t_info.text.Contains("PickupRange"),"character stat labels");
        Check(info.t_info.text.Contains("Evasion") && info.t_info.text.Contains("Luck") && info.t_infoNumbers.text.Contains("<color="),"character modifier details");
        Check(!info.t_info.text.Contains("FallDamageReduction"),"excluded fall damage stat");
        Check(info.t_infoNumbers.text.Contains(StatsUi.FormatStat(EStat.JumpHeight,8f)) && info.t_infoNumbers.text.Contains(StatsUi.FormatStat(EStat.PickupRange,9f)),"Fox raw jump and pickup stats");
        int subscriptions=PlayerStatusEffects.A_StatusModifiedStat?.GetInvocationList().Length ?? 0;
        int levelSubscriptions=PlayerXp.A_LevelUp?.GetInvocationList().Length ?? 0;
        info.SetCharacterInformation(fox);
        Check((PlayerStatusEffects.A_StatusModifiedStat?.GetInvocationList().Length ?? 0)==subscriptions,"reselection cleans up temporary stat subscriptions");
        int checkedCharacters=0;
        foreach(var guid in AssetDatabase.FindAssets("t:CharacterData",new[]{"Assets/MonoBehaviour"})) {
            var character=AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if(character==null || !character.isEnabled) continue;
            try {
                info.SetCharacterInformation(character);
                Check(info.t_info.text.Contains("MaxHealth") && info.t_info.text.Contains("JumpHeight") && !string.IsNullOrEmpty(info.t_infoNumbers.text),"character panel for " + character.name);
                Check((PlayerStatusEffects.A_StatusModifiedStat?.GetInvocationList().Length ?? 0)==subscriptions,"temporary subscription count for " + character.name);
                var dummy=(PlayerInventory)typeof(UnlocksExtraInformation).GetField("dummyInventory",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(info);
                foreach(var modifier in character.statModifiers) {
                    float flat=0f, addition=0f, multiplication=1f;
                    foreach(var entry in character.statModifiers) if(entry.stat==modifier.stat) {
                        if(entry.modifyType==EStatModifyType.Flat) flat+=entry.modification;
                        else if(entry.modifyType==EStatModifyType.Addition) addition+=entry.modification;
                        else if(entry.modifyType==EStatModifyType.Multiplication) multiplication*=entry.modification;
                    }
                    float expected=(PlayerStatsNew.GetBaseValue(modifier.stat)+flat)*(1f+addition)*multiplication;
                    Check(Mathf.Approximately(dummy.playerStats.GetRawStat(modifier.stat),expected),"raw stat " + modifier.stat + " for " + character.name);
                }
                checkedCharacters++;
            } catch(Exception ex) { throw new Exception("Character " + character.name + " failed",ex); }
        }
        Check(checkedCharacters>1,"multiple original character assets checked");
        info.SetCharacterInformation(fox);
        Check((PlayerXp.A_LevelUp?.GetInvocationList().Length ?? 0)==levelSubscriptions,"passive level callbacks cleaned up across roster");
        var calcium=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Calcium.asset");Check(calcium!=null && calcium.GetUnlockRequirement()!=null,"original purchasable character");
        manager.unsortedCharacterData.Add(calcium);save.progression.achievements.Add(calcium.GetUnlockRequirement().internalName);
        var characterMenu=state.AddComponent<CharacterMenu>();var main=state.AddComponent<MainMenu>();var chosen=state.AddComponent<MyButtonCharacter>();chosen.characterData=calcium;characterMenu.mainMenu=main;characterMenu.shopUi=ui;
        typeof(CharacterMenu).GetField("selectedButton",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(characterMenu,chosen);
        var characterTab=new GameObject("test character tab");main.tabUnlocks=ui.gameObject;typeof(MainMenu).GetField("currentTab",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(main,characterTab);ui.gameObject.SetActive(false);
        characterMenu.BuyCharacter();Check(ui.gameObject.activeSelf && !characterTab.activeSelf && ui.tabButtons.current==0,"character Buy opens the original unlocks window and character tab");
        UnlockContainer calciumCard=null;foreach(var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true))if(card.gameObject.activeSelf && card.unlockable==calcium)calciumCard=card;
        Check(calciumCard!=null && calciumCard.isUnlocked && !calciumCard.isPurchased && events.currentSelectedGameObject==calciumCard.gameObject && footer.buyContainer.activeInHierarchy,"selected character and purchase footer are focused");
        int calciumPrice=calcium.GetPrice(),silverBefore=save.progression.silver;footer.buyButton.GetButton().onClick.Invoke();Check(save.progression.purchases.Contains(calcium.GetInternalName()) && save.progression.silver==silverBefore-calciumPrice,"original bound Buy completes character purchase");
        var popupHost=new GameObject("test popup");var popup=popupHost.AddComponent<UiTextPopup>();popup.parent=new GameObject("popup content");popup.parent.transform.SetParent(popupHost.transform);popup.parent.SetActive(false);popup.t_text=new GameObject("popup text",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();popup.t_text.transform.SetParent(popup.parent.transform);popup.canvasGroup=popup.parent.AddComponent<CanvasGroup>();popup.sfx=popupHost.AddComponent<RandomSfx>();
        var alwaysUi=state.AddComponent<AlwaysUi>();AlwaysUi.Instance=alwaysUi;alwaysUi.UiTextPopup=popup;
        var activeMenu=new GameObject("active character menu").AddComponent<CharacterMenu>();var lockedCharacter=new GameObject("locked character",typeof(RectTransform),typeof(UnityEngine.UI.Button),typeof(MyButtonCharacter)).GetComponent<MyButtonCharacter>();lockedCharacter.transform.position=new Vector3(320,240,0);lockedCharacter.canUseCharacter=false;lockedCharacter.cantUseCharacterReason="Character locked";
        var confirmMethod=typeof(CharacterMenu).GetMethod("OnConfirmCharacter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);confirmMethod.Invoke(activeMenu,new object[]{lockedCharacter});
        Check(popup.parent.activeSelf && popup.t_text.text=="Character locked" && popup.t_text.color==Color.red && popup.parent.transform.position==lockedCharacter.transform.position,"unavailable character displays its reason at the button in red");
        var confirm=new GameObject("confirm",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button),typeof(MyButtonNormal)).GetComponent<MyButtonNormal>();confirm.background=confirm.GetComponent<UnityEngine.UI.Image>();activeMenu.b_confirm=confirm;lockedCharacter.canUseCharacter=true;confirmMethod.Invoke(activeMenu,new object[]{lockedCharacter});Check(events.currentSelectedGameObject==confirm.gameObject,"available character moves focus to confirm");
        UnityEngine.Object.Destroy(activeMenu.gameObject);UnityEngine.Object.Destroy(lockedCharacter.gameObject);UnityEngine.Object.Destroy(confirm.gameObject);UnityEngine.Object.Destroy(popupHost);AlwaysUi.Instance=null;
        Debug.Log("UnlocksMenuPlayCheck characters checked: " + checkedCharacters);
        Debug.Log("UnlocksMenuPlayCheck PASSED: original tabs, cloned cards, selection, bound purchases and character-purchase navigation");EditorApplication.Exit(0);
    }
}
