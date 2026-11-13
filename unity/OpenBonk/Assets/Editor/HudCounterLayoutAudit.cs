using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inspect the actual scene's counter glyph bounds after its layout runs.
public static class HudCounterLayoutAudit
{
    public static void Run()
    {
        foreach (string scene in new[] { "Assets/ScenesAndroid/GeneratedMap.unity", "Assets/ScenesAndroid/FinalBossMap.unity" })
        {
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            var timer = UnityEngine.Object.FindFirstObjectByType<GameTimer>(FindObjectsInactive.Include);
            var counters = UnityEngine.Object.FindFirstObjectByType<KillsAndGoldCounter>(FindObjectsInactive.Include);
            if (timer == null || counters == null) throw new Exception("HUD counter scripts unavailable in " + scene);
            var texts = new[] { timer.t_timerRun, counters.t_kills, counters.t_gold, counters.t_silver, counters.t_chestPrice };
            var values = new[] { "<size=110%><sprite name=clock></size> 123:45", "<size=120%><sprite name=skull></size> 999,999", "<size=110%><sprite name=gold></size> 99,999,999", "<size=110%><sprite name=silver></size> 999,999", "<size=115%><sprite name=chest></size><sprite name=gold>9999" };
            for (int i = 0; i < texts.Length; i++) if (texts[i] != null) texts[i].text = values[i];
            typeof(KillsAndGoldCounter).GetMethod("ConfigureCounterLayout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(counters, null);
            Canvas.ForceUpdateCanvases();
            Canvas.ForceUpdateCanvases();
            var bounds = new Rect[4];
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i]; if (t == null) continue;
                t.ForceMeshUpdate(true, true);
                var lo = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0); var hi = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0);
                for (int j = 0; j < t.textInfo.characterCount; j++)
                {
                    var ch = t.textInfo.characterInfo[j]; if (!ch.isVisible) continue;
                    foreach (var p in new[] { ch.bottomLeft, ch.topRight }) { var w = t.transform.TransformPoint(p); lo = Vector3.Min(lo, w); hi = Vector3.Max(hi, w); }
                }
                Debug.Log($"HUD counter scene={scene} name={t.name} active={t.gameObject.activeInHierarchy} bounds=({lo.x:F2},{lo.y:F2})-({hi.x:F2},{hi.y:F2}) chars={t.textInfo.characterCount} text={t.text}");
                if (i < bounds.Length) bounds[i] = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
                for (Transform p = t.transform; p != null && p != t.transform.root; p = p.parent)
                {
                    var rt = p as RectTransform;
                    var group = p.GetComponent<UnityEngine.UI.LayoutGroup>();
                    var horizontal = group as UnityEngine.UI.HorizontalLayoutGroup;
                    Debug.Log($"HUD hierarchy text={t.name} name={p.name} pos={(rt != null ? rt.anchoredPosition : Vector2.zero)} size={(rt != null ? rt.rect.size : Vector2.zero)} layout={(group != null ? group.GetType().Name : "none")} enabled={(group != null && group.enabled)} preferred={(rt != null ? UnityEngine.UI.LayoutUtility.GetPreferredWidth(rt) : -1f)} controlWidth={(horizontal != null && horizontal.childControlWidth)} forceExpand={(horizontal != null && horizontal.childForceExpandWidth)} spacing={(horizontal != null ? horizontal.spacing : 0f)}");
                }
            }
            for (int i = 0; i < bounds.Length; i++) for (int j = i + 1; j < bounds.Length; j++)
                if (bounds[i].Overlaps(bounds[j])) throw new Exception($"HUD counter glyph overlap scene={scene} {texts[i].name}/{texts[j].name}");
            Debug.Log("HUD counter glyph spacing PASS scene=" + scene);
        }
    }
}
