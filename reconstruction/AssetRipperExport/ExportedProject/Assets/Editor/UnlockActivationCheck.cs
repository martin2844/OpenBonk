using System;
using System.Collections.Generic;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UnlockActivationCheck {
    static void Check(bool ok,string message) { if (!ok) throw new Exception("UnlockActivationCheck: "+message); }
    public static void Run() {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity",OpenSceneMode.Single);
        var ui=UnityEngine.Object.FindAnyObjectByType<UnlocksUi>(FindObjectsInactive.Include);
        Check(ui!=null && ui.unlockContainerPrefab!=null,"original unlock card binding");
        var state=new GameObject("test state"); state.SetActive(false); var save=state.AddComponent<SaveManager>(); save.progression=new ProgressionSaveFile(); save.stats=new StatsSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        foreach(EShopItem item in Enum.GetValues(typeof(EShopItem))) save.progression.shopItems[item]=0;
        var manager=state.AddComponent<DataManager>(); DataManager.Instance=manager;
        var weapon=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset");
        var character=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        var tome=AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeBlood.asset");
        var itemData=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Beacon.asset");
        Check(weapon!=null && weapon.GetUnlockRequirement()!=null && weapon.GetPrice()>0 && character!=null && tome!=null && itemData!=null,"original data assets");
        manager.unsortedCharacterData=new List<CharacterData>{character}; manager.unsortedWeapons=new List<WeaponData>{weapon}; manager.unsortedTomes=new List<TomeData>{tome}; manager.unsortedItems=new List<ItemData>{itemData};
        var all=manager.GetAllPurchasable(); Check(all.Count==4 && all[0]==character && all[1]==weapon && all[2]==tome && all[3]==itemData,"purchasable asset ordering");
        save.progression.achievements.Add(weapon.GetUnlockRequirement().internalName); save.progression.purchases.Add(weapon.GetInternalName());
        var weaponLookup=(Dictionary<EWeapon,WeaponData>)typeof(DataManager).GetField("weapons",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(manager);
        weaponLookup.Add(weapon.eWeapon,weapon);
        var player=state.AddComponent<MyPlayer>(); MyPlayer.Instance=player;
        player.inventory=(PlayerInventory)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.weaponInventory=new WeaponInventory();
        Check(!MyAchievements.CanToggleActivation(weapon),"shop upgrade required");
        save.progression.shopItems[EShopItem.Toggler]=1;
        Check(MyAchievements.CanToggleActivation(weapon) && !MyAchievements.CanToggleActivation(character),"weapon eligible, character excluded");
        var always=UnityEngine.Object.Instantiate(character); always.canAlwaysToggle=true; Check(MyAchievements.CanToggleActivation(always),"explicit always-toggle override");
        var clone=UnityEngine.Object.Instantiate(ui.unlockContainerPrefab); var card=clone.GetComponent<UnlockContainer>(); Check(card!=null && card.activationToggle!=null && card.activationToggleCheckmark!=null && card.unactivatedOverlay!=null,"original toggle card bindings");
        card.Set(weapon); Check(card.isUnlocked && card.isPurchased && card.activationToggle.activeSelf && card.activationToggleCheckmark.activeSelf && !card.unactivatedOverlay.activeSelf && MyAchievements.IsAvailable(weapon),"purchased weapon initially active");
        Check(UnlockUtility.GetAvailableWeapons().Contains(weapon),"active weapon offered during a run");
        card.ToggleActivation(); Check(save.progression.inactivated.Contains(weapon.GetInternalName()) && !card.activationToggleCheckmark.activeSelf && card.unactivatedOverlay.activeSelf && !MyAchievements.IsAvailable(weapon),"disable weapon");
        Check(!UnlockUtility.GetAvailableWeapons().Contains(weapon),"disabled weapon omitted from run offers");
        card.ToggleActivation(); Check(!save.progression.inactivated.Contains(weapon.GetInternalName()) && card.activationToggleCheckmark.activeSelf && !card.unactivatedOverlay.activeSelf && MyAchievements.IsAvailable(weapon),"re-enable weapon");
        Check(UnlockUtility.GetAvailableWeapons().Contains(weapon),"re-enabled weapon returns to run offers");
        save.progression.shopItems[EShopItem.Toggler]=0; card.ToggleActivation(); Check(MyAchievements.IsAvailable(weapon),"toggle cannot bypass shop gate");
        UnityEngine.Object.DestroyImmediate(clone); UnityEngine.Object.DestroyImmediate(always); UnityEngine.Object.DestroyImmediate(state);
        Debug.Log("UnlockActivationCheck PASSED: original card, toggler gate, availability and purchasable data ordering");
    }
}
