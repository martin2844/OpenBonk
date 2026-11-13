using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEngine;

public static class PowerGlovesAlignmentPortCheck
{
    public static void Run()
    {
        var previous = MyPlayer.Instance;
        var playerObject = new GameObject("alignment test player");
        GameObject effect = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PowerGlovesFx.prefab");
            if (prefab == null) throw new Exception("Original Power Gloves effect missing");
            effect = UnityEngine.Object.Instantiate(prefab);
            var align = effect.GetComponent<AlignWithPlayerRotation>();
            if (align == null) throw new Exception("Alignment component missing from effect");
            var tick = typeof(AlignWithPlayerRotation).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            MyPlayer.Instance = null;
            tick.Invoke(align, null);
            var player = playerObject.AddComponent<MyPlayer>();
            var rendererObject = new GameObject("player renderer");
            rendererObject.transform.SetParent(playerObject.transform);
            player.playerRenderer = rendererObject.AddComponent<PlayerRenderer>();
            MyPlayer.Instance = player;
            player.transform.rotation = Quaternion.Euler(20, 35, 10);
            rendererObject.transform.rotation = Quaternion.Euler(40, 70, 15);
            tick.Invoke(align, null);
            if (Vector3.Distance(align.transform.up, rendererObject.transform.up) > .0001f)
                throw new Exception("Effect did not align to renderer up");
            if (Vector3.Distance(align.transform.up, player.transform.up) < .1f)
                throw new Exception("Fixture does not distinguish renderer and player orientation");
            rendererObject.transform.rotation = Quaternion.Euler(-15, 120, 45);
            tick.Invoke(align, null);
            if (Vector3.Distance(align.transform.up, rendererObject.transform.up) > .0001f)
                throw new Exception("Effect did not follow orientation changes");
            Debug.Log("PowerGlovesAlignmentPortCheck passed: exported effect follows renderer up and tolerates absent player");
        }
        finally
        {
            MyPlayer.Instance = previous;
            if (effect != null) UnityEngine.Object.DestroyImmediate(effect);
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }
}
