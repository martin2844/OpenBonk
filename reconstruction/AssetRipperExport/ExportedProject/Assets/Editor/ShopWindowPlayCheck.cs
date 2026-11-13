using System;
using System.Collections;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShopWindowPlayCheck
{
    const string Pending = "ShopWindowPlayCheck.pending";
    const string Path = "Assets/ShopWindowCheckTemporary.unity";
    static ShopWindowPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() {
        Scene source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<ShopWindow>(FindObjectsInactive.Include);
        if (original == null || original.shopContainerPrefab == null || original.contentParent == null || original.shopFooter == null || original.btnBuy == null || original.btnRefund == null) throw new Exception("Original shop bindings missing");
        Scene test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var clone = UnityEngine.Object.Instantiate(original.gameObject); clone.name = "original shop"; clone.SetActive(false); SceneManager.MoveGameObjectToScene(clone,test);
        EditorSceneManager.SetActiveScene(test); EditorSceneManager.CloseScene(source,true); EditorSceneManager.SaveScene(test,Path);
        SessionState.SetBool(Pending,true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("shop test runner").AddComponent<ShopWindowPlayRunner>().Begin(); } }
}
public class ShopWindowPlayRunner : MonoBehaviour
{
    static void Check(bool value,string message) { if (!value) throw new Exception("ShopWindowPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guarded()); }
    IEnumerator Guarded() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run() {
        var state = new GameObject("fixture state"); state.SetActive(false);
        var save = state.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); save.progression.silver = 1000;
        foreach (EShopItem item in Enum.GetValues(typeof(EShopItem))) save.progression.shopItems[item] = 0;
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var data = state.AddComponent<DataManager>(); DataManager.Instance = data;
        var itemData = AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Refresh.asset"); Check(itemData != null && itemData.isEnabled, "refresh asset");
        Check(itemData.unlockRequirement != null && itemData.unlockRequirement.statName == "questsCompleted" && itemData.unlockRequirement.targetValue == 20,"original refresh unlock requirement");
        MyStats.SetValueForce("questsCompleted", 5);
        data.shopItems.Add(itemData.eShopItem,itemData); save.progression.newShopItems.Add(itemData.GetInternalName());
        var skip = AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Skip.asset");
        Check(skip != null && skip.isEnabled, "skip asset");
        if (skip.unlockRequirement != null) save.progression.achievements.Add(skip.unlockRequirement.internalName);
        data.shopItems.Add(skip.eShopItem, skip);
        var window = UnityEngine.Object.FindAnyObjectByType<ShopWindow>(FindObjectsInactive.Include);
        Check(!save.progression.menuMeta.hasVisitedShop,"shop initially unvisited");
        window.gameObject.SetActive(true); yield return null;
        Check(save.progression.menuMeta.hasVisitedShop,"opening original shop window records visit");
        var cards = window.contentParent.GetComponentsInChildren<ShopContainer>(true);
        ShopContainer card = null, second = null;
        foreach (var c in cards) { if (c.data == itemData) card = c; if (c.data == skip) second = c; }
        Check(card != null && second != null && card != second, "original and cloned cards populated");
        var clonedButton = second.GetComponentInChildren<MyButtonShop>(true);
        Check(clonedButton != null && clonedButton.shopContainer == second, "cloned card button binding");
        var button = card.GetComponent<MyButtonShop>(); if (button == null) button = card.GetComponentInChildren<MyButtonShop>(true);
        Check(button != null && button.shopContainer == card,"original card button binding");
        var events = new GameObject("event system").AddComponent<EventSystem>();
        button.OnSelect(new BaseEventData(events)); Check(window.currentContainer == card && !save.progression.newShopItems.Contains(itemData.GetInternalName()),"selection event selects and clears new alert");
        var requirements = window.shopFooter.requirementsContainer; Check(requirements != null && requirements.gameObject.activeSelf && requirements.reqContainers.Length > 0,"locked requirement panel");
        var row = requirements.reqContainers[0]; Check(row.gameObject.activeSelf && row.progress.activeSelf && row.t_progress.text.Contains("5") && row.t_progress.text.Contains("20") && Mathf.Approximately(row.progressBar.transform.localScale.x,.25f) && !row.checkMark.activeSelf,"actual achievement requirement and progress");
        requirements.HideBar(); Check(!row.progress.activeSelf && row.t_progress.text == string.Empty, "hide requirement progress");
        row.Set((Assets.Scripts.Saves___Serialization.Progression.Achievements.UnlockableBase)itemData);
        Check(!row.checkMark.activeSelf && !row.progress.activeSelf && !string.IsNullOrEmpty(row.t_requirement.text),"unpurchased prerequisite display");
        save.progression.purchases.Add(itemData.GetInternalName()); row.Set((Assets.Scripts.Saves___Serialization.Progression.Achievements.UnlockableBase)itemData);
        Check(row.checkMark.activeSelf,"purchased prerequisite checkmark"); save.progression.purchases.Remove(itemData.GetInternalName());
        save.progression.achievements.Add(itemData.unlockRequirement.internalName); card.Set(itemData); button.OnSelect(new BaseEventData(events));
        Check(!requirements.gameObject.activeSelf && window.shopFooter.buy.activeSelf,"achievement unlock changes footer");
        yield return new WaitForSeconds(.2f);
        button.GetButton().onClick.Invoke(); Check(events.currentSelectedGameObject == window.btnBuy.gameObject,"shop card click focuses buy");
        int price = itemData.GetPrice(); window.btnBuy.GetButton().onClick.Invoke(); Check(itemData.GetLevel() == 1 && save.progression.silver == 1000-price,"bound buy button changes actual save");
        Check(window.t_refund.text.Contains(price.ToString()),"refund label"); window.btnRefund.GetButton().onClick.Invoke(); Check(itemData.GetLevel() == 0 && save.progression.silver == 1000,"bound refund button restores silver");
        Check(window.t_buy.text.Contains(price.ToString()),"buy label");
        Debug.Log("ShopWindowPlayCheck PASSED: original scene shop cards, selection, bound purchase, refund and labels"); EditorApplication.Exit(0);
    }
}
