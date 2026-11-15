using System;
using System.Reflection;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UpgradeOfferNameLayoutCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static Rect GlyphBounds(TMP_Text text)
    {
        text.ForceMeshUpdate(true, true);
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < text.textInfo.characterCount; i++)
        {
            var character = text.textInfo.characterInfo[i];
            if (!character.isVisible) continue;
            Vector3 a = text.transform.TransformPoint(character.bottomLeft);
            Vector3 b = text.transform.TransformPoint(character.topRight);
            minX = Mathf.Min(minX, a.x, b.x); minY = Mathf.Min(minY, a.y, b.y);
            maxX = Mathf.Max(maxX, a.x, b.x); maxY = Mathf.Max(maxY, a.y, b.y);
        }
        Require(!float.IsInfinity(minX), "no visible glyphs: " + text.text);
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    public static void Run()
    {
#if !UNITY_ANDROID
        throw new Exception("Android build target is required");
#else
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<UpgradeButton>(FindObjectsInactive.Include);
        Require(original != null && original.t_name != null && original.t_description != null, "original offer card missing");
        var canvasObject = new GameObject("offer check canvas", typeof(RectTransform), typeof(Canvas));
        var clone = UnityEngine.Object.Instantiate(original.gameObject, canvasObject.transform, false);
        try
        {
            clone.SetActive(true);
            var offer = clone.GetComponent<UpgradeButton>();
            offer.t_name.gameObject.SetActive(true);
            offer.t_description.gameObject.SetActive(true);
            offer.t_description.text = "Velocidad del Proyectil: 0% → 15%";
            var fit = typeof(UpgradeButton).GetMethod("FitOfferName", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(fit != null, "Android name layout method missing");
            foreach (string name in new[] { "Tomo de Velocidad de Proyectil", "Espada", "Tomo de velocidad de proyectil extraordinariamente prolongado" })
            {
                offer.t_name.text = name;
                fit.Invoke(offer, null);
                Canvas.ForceUpdateCanvases();
                Rect title = GlyphBounds(offer.t_name);
                Rect description = GlyphBounds(offer.t_description);
                Require(offer.t_name.textInfo.lineCount == 1, "offer title wrapped: " + name);
                Require(!title.Overlaps(description), "offer title overlaps description: " + name);
            }
            Debug.Log("UpgradeOfferNameLayoutCheck PASS");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
#endif
    }
}
