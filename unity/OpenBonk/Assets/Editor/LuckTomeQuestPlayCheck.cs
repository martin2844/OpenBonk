using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LuckTomeQuestPlayCheck {
    const string Pending = "LuckTomeQuestPlayCheck.pending";
    static LuckTomeQuestPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("luck quest runner").AddComponent<LuckTomeQuestRunner>().Begin(); } }
}
public class LuckTomeQuestRunner : MonoBehaviour {
    class FixedRandom : System.Random { public double value; public int calls; public override double NextDouble() { calls++; return value; } }
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("LuckTomeQuestPlayCheck: " + msg); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        try {
            var root = new GameObject("test state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_luckTome.asset"); var tome = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeLuck.asset");
            Check(achievement && tome && Mathf.Abs(achievement.targetValueFloat - .01f) < .000001f, "original achievement chance and tome");
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            typeof(DataManager).GetField("achievementsData", P).SetValue(data, new Dictionary<string,MyAchievement> { ["a_luckTome"] = achievement });
            typeof(DataManager).GetField("tomeData", P).SetValue(data, new Dictionary<ETome,TomeData> { [tome.eTome] = tome });
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory(); player.inventory.tomeInventory = new TomeInventory();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down; ground.transform.localScale = new Vector3(50,2,50); game.whatIsGround = 1 << ground.layer; Physics.SyncTransforms();
            var effect = root.AddComponent<EffectManager>(); EffectManager.Instance = effect; effect.luckTomeQuest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/LuckTomePickupQuest.prefab"); Check(effect.luckTomeQuest, "original pickup prefab");
            var enemyObject = new GameObject("enemy"); enemyObject.SetActive(false); enemyObject.transform.position = new Vector3(3, 5, 4); var enemy = enemyObject.AddComponent<Enemy>();
            var rng = new FixedRandom(); global::Utility.MyRandom.random = rng; AchievementTracker.Init(); GameManager.A_RunStarted?.Invoke();
            rng.value = .00011; Enemy.A_EnemyDied?.Invoke(enemy, null); Check(!effect.spawnedLuckTomeObject && rng.calls == 1, "roll above threshold fails");
            rng.value = .00009; Enemy.A_EnemyDied?.Invoke(enemy, null); Check(effect.spawnedLuckTomeObject && rng.calls == 2, "roll below threshold succeeds");
            Check(Vector3.Distance(effect.spawnedLuckTomeObject.transform.position, new Vector3(3,2,4)) < .01f, "pickup above ground");
            DestroyImmediate(effect.spawnedLuckTomeObject); Enemy.A_EnemyDied?.Invoke(enemy, null); Check(!effect.spawnedLuckTomeObject && rng.calls == 2, "once per run even after pickup disappears");
            GameManager.A_RunStarted?.Invoke(); Enemy.A_EnemyDied?.Invoke(enemy, null); Check(effect.spawnedLuckTomeObject && rng.calls == 3, "new run allows another roll");
            var pickup = effect.spawnedLuckTomeObject.GetComponent<InteractableUnlock>(); Check(pickup && pickup.Interact() && !pickup.Interact(), "pickup collected once");
            Check(MyAchievements.IsAchievementDone("a_luckTome") && player.inventory.tomeInventory.GetTomeLevel(tome.eTome) == 1, "tome and achievement awarded");
            GameManager.A_RunStarted?.Invoke(); Enemy.A_EnemyDied?.Invoke(enemy, null); Check(rng.calls == 3, "completed achievement prevents further rolls");
            AchievementTracker.Cleanup(); player.inventory.itemInventory.Cleanup();
            Debug.Log("LuckTomeQuestPlayCheck passed: original odds, per-run reset, pickup and achievement"); EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
