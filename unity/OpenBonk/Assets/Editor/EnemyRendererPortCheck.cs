using System;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using UnityEditor;
using UnityEngine;

public static class EnemyRendererPortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception("EnemyRendererPortCheck: " + message); }
    static void Near(Color a, Color b, string message) { Check(Mathf.Abs(a.r-b.r)<.002f && Mathf.Abs(a.g-b.g)<.002f && Mathf.Abs(a.b-b.b)<.002f && Mathf.Abs(a.a-b.a)<.002f, message + " got " + a); }
    static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out Color c); return c; }
    static Color Property(Renderer renderer, string key) { var b = new MaterialPropertyBlock(); renderer.GetPropertyBlock(b); return b.GetColor(key); }
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab");
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage2.asset");
        Check(prefab && data, "original enemy prefab and Ent boss data");
        var root = UnityEngine.Object.Instantiate(prefab); var enemy = root.GetComponent<Enemy>(); var visual = root.GetComponentInChildren<EnemyRenderer>(true);
        try {
            Check(enemy != null && visual != null && visual.enemy == enemy && visual.enemyRenderer != null, "prefab references");
            visual.Set(data); typeof(EnemyRenderer).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(visual, null);
            enemy.debuffs.Add(EDebuff.Echo, null); enemy.A_DebuffAdded(EDebuff.Echo);
            Near(Property(visual.enemyRenderer, "_SpecularColor"), Hex("#00D182"), "echo specular");
            Near(Property(visual.enemyRenderer, "_AlbedoColor"), Hex("#94FFCD"), "echo albedo");
            visual.SetInvulnerable(true); Near(Property(visual.enemyRenderer, "_RimColor"), new Color(0,1,1,1), "invulnerable rim");
            Near(Property(visual.enemyRenderer, "_AlbedoColor"), Hex("#94FFCD"), "rim preserves debuff");
            visual.SetInvulnerable(false); Near(Property(visual.enemyRenderer, "_RimColor"), new Color(0,1,1,0), "vulnerable rim");
            enemy.debuffs.Clear(); enemy.debuffs.Add(EDebuff.Freeze, null); enemy.A_DebuffAdded(EDebuff.Freeze);
            Near(Property(visual.enemyRenderer, "_SpecularColor"), Hex("#009A9A"), "freeze specular");
            Near(Property(visual.enemyRenderer, "_AlbedoColor"), Hex("#A8FEFF"), "freeze albedo");
            enemy.debuffs.Clear(); enemy.debuffs.Add(EDebuff.Stun, null); enemy.A_DebuffAdded(EDebuff.Stun);
            Near(Property(visual.enemyRenderer, "_SpecularColor"), Hex("#918200"), "stun specular");
            enemy.debuffs.Clear(); enemy.A_DebuffRemoved(EDebuff.Stun);
            Near(Property(visual.enemyRenderer, "_SpecularColor"), new Color(0,0,0,1), "default specular");
            Near(Property(visual.enemyRenderer, "_AlbedoColor"), Color.white, "default albedo");
            // The original GetDebuffColor checks Echo separately, but inspects only
            // the first enumerated debuff for Freeze or Stun.
            enemy.debuffs.Add(EDebuff.Poison, null); enemy.debuffs.Add(EDebuff.Freeze, null);
            enemy.A_DebuffAdded(EDebuff.Freeze);
            Near(Property(visual.enemyRenderer, "_SpecularColor"), new Color(0,0,0,1), "poison first leaves default color");
            enemy.debuffs.Clear(); enemy.debuffs.Add(EDebuff.Freeze, null); enemy.debuffs.Add(EDebuff.Poison, null);
            enemy.A_DebuffAdded(EDebuff.Poison);
            Near(Property(visual.enemyRenderer, "_SpecularColor"), Hex("#009A9A"), "freeze first overrides default");
            enemy.debuffs.Clear(); enemy.debuffs.Add(EDebuff.Poison, null); enemy.debuffs.Add(EDebuff.Echo, null);
            enemy.A_DebuffAdded(EDebuff.Echo);
            Near(Property(visual.enemyRenderer, "_SpecularColor"), Hex("#00D182"), "echo checked independently");
            typeof(EnemyRenderer).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(visual, null);
            Check(enemy.A_DebuffAdded == null && enemy.A_DebuffRemoved == null, "event cleanup");
            Debug.Log("EnemyRendererPortCheck passed: original prefab debuff colors, invulnerability rim and event cleanup");
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
