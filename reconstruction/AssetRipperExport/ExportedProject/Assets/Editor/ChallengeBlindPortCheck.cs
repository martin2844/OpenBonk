using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers;
using UnityEditor;
using UnityEngine;

public static class ChallengeBlindPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var playerObject = new GameObject("player"); var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.transform.position = new Vector3(7f, 2f, 9f);
        var effectObject = new GameObject("effect manager"); var effects = effectObject.AddComponent<EffectManager>(); EffectManager.Instance = effects;
        effects.blindSphere = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChallengeBlindSphere.prefab");
        Check(effects.blindSphere != null, "exported fog sphere prefab missing");
        var oldColor = RenderSettings.fogColor; var oldDensity = RenderSettings.fogDensity;
        RenderSettings.fogColor = new Color(.8f, .4f, .2f, .6f); RenderSettings.fogDensity = .001f;
        var modifier = ScriptableObject.CreateInstance<ChallengeModifierBlind>(); modifier.Init(null); MapGenerationController.A_GenerationComplete?.Invoke();
        var fog = GameObject.Find("ChallengeBlindSphere(Clone)"); Check(fog != null && fog.transform.position == player.transform.position, "sphere created at player");
        Check(Mathf.Approximately(RenderSettings.fogDensity, .035f) && Mathf.Abs(RenderSettings.fogColor.r - .4f) < .001f && Mathf.Abs(RenderSettings.fogColor.a - .8f) < .001f, "original fog density and color");
        player.transform.position = new Vector3(-3f, 4f, 6f); typeof(FogOfWar).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fog.GetComponent<FogOfWar>(), null);
        Check(fog.transform.position == player.transform.position, "fog tracks player movement");
        modifier.Cleanup(); UnityEngine.Object.DestroyImmediate(fog); MapGenerationController.A_GenerationComplete?.Invoke();
        Check(GameObject.Find("ChallengeBlindSphere(Clone)") == null, "cleanup removes generation callback");
        RenderSettings.fogColor = oldColor; RenderSettings.fogDensity = oldDensity; MyPlayer.Instance = null; EffectManager.Instance = null;
        UnityEngine.Object.DestroyImmediate(effectObject); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(modifier);
        Debug.Log("ChallengeBlindPortCheck passed: generation event, original fog, follow behavior and cleanup");
    }
}
