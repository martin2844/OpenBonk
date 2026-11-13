using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MusicDangerPlayCheck
{
    const string Pending = "MusicDangerPlayCheck.pending";
    static MusicDangerPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("music danger runner").AddComponent<MusicDangerPlayRunner>().Begin();
    }
}
public class MusicDangerPlayRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        var setup = new GameObject("synthetic data"); setup.SetActive(false);
        var save = setup.AddComponent<SaveManager>(); save.config = new ConfigSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        var player = setup.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        health.hp = 24; health.maxHp = 100; inventory.playerHealth = health; player.inventory = inventory;
        var effects = setup.AddComponent<PlayerEffects>(); player.playerEffects = effects;
        var musicGo = new GameObject("music"); var source = musicGo.AddComponent<AudioSource>();
        var music = musicGo.AddComponent<MusicController>(); music.musicSource = source;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var updateEffects = typeof(PlayerEffects).GetMethod("Update", flags);
        try
        {
            MyTime.deltaTime = .1f;
            updateEffects.Invoke(effects, null);
            health.hp = 4;
            updateEffects.Invoke(effects, null);
            for (int i = 0; i < 40; i++) updateEffects.Invoke(effects, null);
            if (effects.GetDangerRatioMusic() <= .35f) throw new Exception("Damage at low health did not produce music danger");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(.5f);
        try
        {
            if (source.pitch >= .99f || source.pitch < .7f) throw new Exception("Danger did not lower pitch: " + source.pitch);
			float previousDanger = effects.GetDangerRatio();
			health.hp = 100;
			updateEffects.Invoke(effects, null);
			if (effects.GetDangerRatio() >= previousDanger) throw new Exception("Restored health did not reduce danger");
            save.config.cfVisualsSettings.low_hp_effects = 0;
            updateEffects.Invoke(effects, null);
            typeof(MusicController).GetMethod("UpdatePitch", flags).Invoke(music, null);
            if (effects.GetDangerRatio() != 0f || source.pitch != 1f) throw new Exception("Disabling low HP effects did not reset danger/pitch");
            Debug.Log("MusicDangerPlayCheck passed"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
