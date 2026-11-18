using System;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;

public static class ToggleEverythingPortCheck
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception("ToggleEverythingPortCheck: " + message); }
    public static void Run()
    {
        var state = new GameObject("toggle policy test"); state.SetActive(false);
        var save = state.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        var tome = ScriptableObject.CreateInstance<TomeData>();
        var item = ScriptableObject.CreateInstance<ItemData>();
        var character = ScriptableObject.CreateInstance<CharacterData>();
        var hat = ScriptableObject.CreateInstance<HatData>();
        try
        {
            weapon.isEnabled = tome.isEnabled = item.isEnabled = character.isEnabled = hat.isEnabled = true;
            save.progression.shopItems[EShopItem.Toggler] = 0;
            OpenBonkTogglePolicy.SetEnabled(false);
            Check(!MyAchievements.CanToggleActivation(weapon) && !MyAchievements.CanToggleActivation(tome) && !MyAchievements.CanToggleActivation(item), "ordinary no toggler");
            OpenBonkTogglePolicy.SetEnabled(true);
            Check(!MyAchievements.CanToggleActivation(weapon), "shop still required");
            save.progression.shopItems[EShopItem.Toggler] = 1;
            Check(MyAchievements.CanToggleActivation(weapon) && MyAchievements.CanToggleActivation(tome) && MyAchievements.CanToggleActivation(item), "three types eligible");
            Check(!MyAchievements.CanToggleActivation(character) && !MyAchievements.CanToggleActivation(hat), "character and hat unaffected");
            save.progression.inactivated.Add(weapon.GetInternalName());
            Check(!MyAchievements.IsActivated(weapon), "native saved inactive choice honored");
            OpenBonkUnlockPolicy.SetEnabled(true);
            Check(!MyAchievements.IsAvailable(weapon), "unlock-all does not override an inactive selection when toggle mod is active");
            save.progression.inactivated.Remove(weapon.GetInternalName());
            Check(MyAchievements.IsAvailable(weapon), "unlock-all keeps active item available");
            save.progression.inactivated.Add(weapon.GetInternalName());
            OpenBonkTogglePolicy.SetEnabled(false);
            Check(MyAchievements.IsAvailable(weapon), "unlock-all existing standalone behavior preserved");
            OpenBonkUnlockPolicy.SetEnabled(false); OpenBonkTogglePolicy.SetEnabled(true);
            Check(!weapon.canAlwaysToggle && !tome.canAlwaysToggle && !item.canAlwaysToggle, "data assets not mutated");
            OpenBonkTogglePolicy.SetEnabled(false);
            Check(!MyAchievements.CanToggleActivation(weapon) && !MyAchievements.CanToggleActivation(tome) && !MyAchievements.CanToggleActivation(item), "native eligibility restored");
            Check(MyAchievements.IsActivated(weapon), "native no-toggle semantics restored");
            Check(save.progression.inactivated.Contains(weapon.GetInternalName()), "saved choice retained");
            Check(!MyAchievements.CanToggleActivation(character) && !MyAchievements.CanToggleActivation(hat), "unrelated types still unaffected");
            character.canAlwaysToggle = true;
            Check(MyAchievements.CanToggleActivation(character), "pre-existing native override preserved");
            Debug.Log("ToggleEverythingPortCheck passed");
        }
        finally
        {
            OpenBonkTogglePolicy.SetEnabled(false); OpenBonkUnlockPolicy.SetEnabled(false);
            UnityEngine.Object.DestroyImmediate(weapon); UnityEngine.Object.DestroyImmediate(tome);
            UnityEngine.Object.DestroyImmediate(item); UnityEngine.Object.DestroyImmediate(character);
            UnityEngine.Object.DestroyImmediate(hat); UnityEngine.Object.DestroyImmediate(state);
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
        }
    }
}
