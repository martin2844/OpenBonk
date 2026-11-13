#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;

// Disposable opt-in observation of an existing generated egg and real enemy movement.
internal sealed class AndroidFrogQuestProbe : MonoBehaviour
{
    readonly HashSet<Enemy> tracked = new HashSet<Enemy>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_frog_quest_probe", false)) return;
        var root = new GameObject("frog quest probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidFrogQuestProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        InteractableEgg egg = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted && EnemyManager.Instance != null)
                egg = FindObjectOfType<InteractableEgg>();
            if (egg != null) break;
            yield return null;
        }
        if (egg == null) { Debug.LogError("Frog quest probe INCONCLUSIVE: generated egg unavailable"); yield break; }
        var player = MyPlayer.Instance;
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var body = player.playerMovement.GetRb();
        body.position = egg.transform.position + Vector3.up * 2f;
        body.velocity = Vector3.zero;
        Debug.Log($"Frog quest probe: generated egg found at {egg.transform.position}; player moved, weapons disabled");
        RaycastHit closest = default;
        bool found = false;
        foreach (var hit in Physics.RaycastAll(egg.transform.position + Vector3.up * 100f, Vector3.down, 200f, GameManager.Instance.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore))
            if (!hit.collider.transform.IsChildOf(egg.transform) && (!found || hit.distance < closest.distance)) { closest = hit; found = true; }
        if (found) Debug.Log($"Frog quest probe: non-egg surface={closest.point} collider={closest.collider.name} eggOffsetY={egg.transform.position.y - closest.point.y:F2}");
        else Debug.Log("Frog quest probe: no non-egg surface under probe ray");
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool existingReward = intent.Call<bool>("getBooleanExtra", "megabonk_frog_existing_reward_probe", false);
        if (existingReward && !MyAchievements.IsUnlocked(egg.unlockAchievement)) MyAchievements.TryUnlock(egg.unlockAchievement.internalName);
        bool first = egg.Interact();
        bool second = egg.Interact();
        var frogs = (List<Enemy>)typeof(InteractableEgg).GetField("frogEnemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(egg);
        var targets = (Dictionary<Enemy, InteractableEgg.FrogContainer>)typeof(InteractableEgg).GetField("frogTargets", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(egg);
        if (!first || second || frogs == null || frogs.Count != 3 || targets == null || targets.Count != 3)
        { Debug.LogError($"Frog quest probe FAIL: first={first}, second={second}, frogs={frogs?.Count}, targets={targets?.Count}"); yield break; }
        Enemy[] snapshot = frogs.ToArray();
        var minimap = player.minimapCameraScript;
        var iconField = typeof(Assets.Scripts.Camera.MinimapCamera).GetField("enemyIconDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
        var icons = minimap != null && iconField != null ? iconField.GetValue(minimap) as Dictionary<Enemy, GameObject> : null;
        int initialIcons = 0;
        if (icons != null) foreach (var frog in snapshot) if (icons.ContainsKey(frog)) initialIcons++;
        Debug.Log($"Frog minimap probe: initialIcons={initialIcons}/3");
        yield return new WaitForSecondsRealtime(1f);
        yield return new WaitForEndOfFrame();
        if (initialIcons == 3 && minimap.minimapCamera != null && minimap.minimapCamera.targetTexture != null)
        {
            var mapCamera = minimap.minimapCamera;
            var targetTexture = mapCamera.targetTexture;
            var oldTarget = RenderTexture.active;
            var image = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA32, false);
            var renderers = new Renderer[3];
            var wasEnabled = new bool[3];
            try
            {
                for (int i = 0; i < 3; i++) { renderers[i] = icons[snapshot[i]].GetComponentInChildren<Renderer>(); if (renderers[i] != null) wasEnabled[i] = renderers[i].enabled; }
                mapCamera.Render(); RenderTexture.active = targetTexture; image.ReadPixels(new Rect(0,0,image.width,image.height),0,0); image.Apply();
                var withIcons = image.GetPixels32();
                System.IO.File.WriteAllBytes(Application.persistentDataPath + "/frog-map-with.png", image.EncodeToPNG());
                foreach (var renderer in renderers) if (renderer != null) renderer.enabled = false;
                mapCamera.Render(); RenderTexture.active = targetTexture; image.ReadPixels(new Rect(0,0,image.width,image.height),0,0); image.Apply();
                var withoutIcons = image.GetPixels32();
                System.IO.File.WriteAllBytes(Application.persistentDataPath + "/frog-map-without.png", image.EncodeToPNG());
                int changed = 0;
                for (int i = 0; i < withIcons.Length; i++) if (Mathf.Abs(withIcons[i].r-withoutIcons[i].r) > 8 || Mathf.Abs(withIcons[i].g-withoutIcons[i].g) > 8 || Mathf.Abs(withIcons[i].b-withoutIcons[i].b) > 8) changed++;
                Debug.Log($"Frog minimap raster probe {(changed > 0 ? "PASS" : "INCONCLUSIVE")}: changedPixels={changed} target={targetTexture.width}x{targetTexture.height}");
            }
            finally
            {
                for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = wasEnabled[i];
                mapCamera.Render(); RenderTexture.active = oldTarget; Destroy(image);
            }
        }
        else Debug.Log("Frog minimap raster probe INCONCLUSIVE: markers or camera target unavailable");
        foreach (var frog in snapshot) tracked.Add(frog);
        Enemy.A_EnemyReleasedFromPool += OnRelease;
        var initial = new Vector3[3];
        for (int i = 0; i < 3; i++) initial[i] = snapshot[i].transform.position;
        yield return new WaitForSecondsRealtime(10f);
        int moved = 0, alive = 0, following = 0;
        for (int i = 0; i < 3; i++)
        {
            var frog = snapshot[i];
            if (frog != null && frog.gameObject.activeInHierarchy && !frog.IsDead()) alive++;
            if (frog != null && frog.state == EEnemyState.FollowTarget) following++;
            Vector3 delta = frog != null ? frog.transform.position - initial[i] : Vector3.zero;
            float horizontal = new Vector2(delta.x, delta.z).magnitude;
            if (horizontal > 0.5f) moved++;
            Debug.Log($"Frog quest probe: frog={i} horizontal={horizontal:F2} vertical={delta.y:F2} state={(frog != null ? frog.state.ToString() : "missing")}");
        }
        int followed = 0;
        if (icons != null) foreach (var frog in snapshot)
            if (frog != null && frog.renderer != null && icons.TryGetValue(frog, out var icon) && icon != null && Vector3.Distance(icon.transform.position, frog.renderer.transform.position) < .5f) followed++;
        Debug.Log($"Frog minimap probe: followed={followed}/3");
        Debug.Log($"Frog quest probe {(moved == 3 && alive == 3 && following == 3 ? "PASS" : "INCONCLUSIVE")}: first={first} second={second} initialFrogs=3 remaining={frogs.Count} moved={moved} alive={alive} following={following}");
        if (intent.Call<bool>("getBooleanExtra", "megabonk_frog_reward_probe", false) || existingReward)
        {
            bool initiallyUnlocked = MyAchievements.IsUnlocked(egg.unlockAchievement);
            Debug.Log($"Frog reward probe: achievement={egg.unlockAchievement.internalName} initiallyUnlocked={initiallyUnlocked} existingBranch={existingReward} frogsRemaining={frogs.Count}");
            if (initiallyUnlocked != existingReward || frogs.Count != 3 || alive != 3)
                Debug.Log("Frog reward probe INCONCLUSIVE: achievement state or frogs unavailable");
            else
            {
                snapshot[0].Kill("frog reward diagnostic");
                snapshot[1].Kill("frog reward diagnostic");
                float end = Time.realtimeSinceStartup + 30f;
                while (Time.realtimeSinceStartup < end && frogs.Count > 1) yield return null;
                int remainingIcons = 0;
                if (icons != null) foreach (var frog in snapshot) if (icons.ContainsKey(frog)) remainingIcons++;
                Debug.Log($"Frog minimap probe: afterTwoReleases={remainingIcons}/3");
                bool earlyUnlocked = MyAchievements.IsUnlocked(egg.unlockAchievement);
                Debug.Log($"Frog reward probe: after two forced kills remaining={frogs.Count} unlocked={earlyUnlocked}");
                if (frogs.Count == 1 && earlyUnlocked == existingReward && frogs[0] == snapshot[2] && !snapshot[2].IsDead())
                {
                    snapshot[2].Kill("frog reward diagnostic");
                    var beforeRelease = new HashSet<OpenChest>(FindObjectsOfType<OpenChest>());
                    end = Time.realtimeSinceStartup + 30f;
                    while (Time.realtimeSinceStartup < end && frogs.Count != 0) yield return null;
                    int finalIcons = 0;
                    if (icons != null) foreach (var frog in snapshot) if (icons.ContainsKey(frog)) finalIcons++;
                    Debug.Log($"Frog minimap probe: afterThreeReleases={finalIcons}/3");
                    bool unlocked = MyAchievements.IsUnlocked(egg.unlockAchievement);
                    int newChests = 0;
                    OpenChest reward = null;
                    foreach (var chest in FindObjectsOfType<OpenChest>())
                        if (!beforeRelease.Contains(chest)) { reward = chest; newChests++; Debug.Log($"Frog reward probe: new chest={chest.name} position={chest.transform.position} active={chest.gameObject.activeInHierarchy}"); }
                    bool pass = unlocked && frogs.Count == 0 && (!existingReward || newChests == 1);
                    Debug.Log($"Frog reward probe {(pass ? "PASS" : "INCONCLUSIVE")}: after third forced kill remaining={frogs.Count} unlocked={unlocked} newChests={newChests}");
                    if (existingReward && pass && reward != null && intent.Call<bool>("getBooleanExtra", "megabonk_frog_collect_probe", false))
                    {
                        int opened = 0, awarded = 0;
                        EItem awardedItem = default;
                        System.Action onOpen = () => opened++;
                        System.Action<EItem> onItem = item => { awarded++; awardedItem = item; };
                        var settings = SaveManager.Instance.config.cfGameSettings;
                        int oldSkip = settings.skip_chest_animation;
                        settings.skip_chest_animation = 1;
                        OpenChest.A_Open += onOpen; ItemInventory.A_ItemAdded += onItem;
                        body.position = reward.transform.position; body.velocity = Vector3.zero; Physics.SyncTransforms();
                        end = Time.realtimeSinceStartup + 8f;
                        while (Time.realtimeSinceStartup < end && opened == 0) yield return null;
                        bool collected = opened == 1 && awarded == 1 && !reward.gameObject.activeSelf;
                        Debug.Log($"Frog reward collection probe {(collected ? "PASS" : "INCONCLUSIVE")}: teleported=true skippedAnimation=true opens={opened} items={awarded} item={awardedItem} chestActive={reward.gameObject.activeSelf}");
                        OpenChest.A_Open -= onOpen; ItemInventory.A_ItemAdded -= onItem;
                        settings.skip_chest_animation = oldSkip;
                    }
                }
                else Debug.Log("Frog reward probe INCONCLUSIVE: first two frogs did not release as expected");
            }
        }
        Enemy.A_EnemyReleasedFromPool -= OnRelease;
    }
    void OnRelease(Enemy enemy) { if (tracked.Contains(enemy)) Debug.Log($"Frog quest probe: tracked frog released instance={enemy.GetInstanceID()} dead={enemy.IsDead()} position={enemy.transform.position}"); }
    void OnDestroy() { Enemy.A_EnemyReleasedFromPool -= OnRelease; }
}
#endif
