using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryHudRowSpacingPortCheck
{
    static void CheckRows(InventoryHud source, Transform parent, int weaponCount, int tomeCount)
    {
        var obj = UnityEngine.Object.Instantiate(source.gameObject, parent);
        obj.SetActive(true);
        var hud = obj.GetComponent<InventoryHud>();
        var weapons = (RectTransform)hud.weaponParent;
        var tomes = (RectTransform)hud.tomeParent;
        weapons.gameObject.SetActive(true); tomes.gameObject.SetActive(true);
        foreach (Transform child in weapons) child.gameObject.SetActive(false);
        foreach (Transform child in tomes) child.gameObject.SetActive(false);
        for (int i = 0; i < weaponCount; i++) UnityEngine.Object.Instantiate(source.itemContainerPrefab, weapons).SetActive(true);
        for (int i = 0; i < tomeCount; i++) UnityEngine.Object.Instantiate(source.itemContainerPrefab, tomes).SetActive(true);
        Canvas.ForceUpdateCanvases();
        typeof(InventoryHud).GetMethod("SeparateRows", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
        Canvas.ForceUpdateCanvases();
        var commonParent = weapons.parent;
        var corners = new Vector3[4];
        float weaponBottom = float.PositiveInfinity, tomeTop = float.NegativeInfinity;
        foreach (RectTransform child in weapons) if (child.gameObject.activeSelf) { child.GetWorldCorners(corners); weaponBottom = Mathf.Min(weaponBottom, commonParent.InverseTransformPoint(corners[0]).y); }
        foreach (RectTransform child in tomes) if (child.gameObject.activeSelf) { child.GetWorldCorners(corners); tomeTop = Mathf.Max(tomeTop, commonParent.InverseTransformPoint(corners[1]).y); }
        if (weaponBottom - tomeTop < 9.9f) throw new Exception("rows overlap: " + weaponCount + " weapons, gap=" + (weaponBottom - tomeTop));
        if (weaponCount > 7)
        {
            float highest = float.NegativeInfinity;
            foreach (RectTransform child in weapons) if (child.gameObject.activeSelf) { child.GetWorldCorners(corners); highest = Mathf.Max(highest, commonParent.InverseTransformPoint(corners[1]).y); }
            if (highest - weaponBottom < 120f)
            {
                var positions = ""; foreach (RectTransform child in weapons) if (child.gameObject.activeSelf) positions += child.anchoredPosition + ";";
                throw new Exception("multiple weapon rows not laid out: span=" + (highest - weaponBottom) + ", active=" + weapons.gameObject.activeInHierarchy + ", positions=" + positions);
            }
        }
        UnityEngine.Object.DestroyImmediate(obj);
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        var source = UnityEngine.Object.FindAnyObjectByType<InventoryHud>(FindObjectsInactive.Include);
        if (source == null || source.weaponParent == null || source.tomeParent == null || source.itemContainerPrefab == null) throw new Exception("scene inventory HUD missing");
        var root = new GameObject("test canvas", typeof(RectTransform), typeof(Canvas));
        var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(1000f, 1000f);
        CheckRows(source, rect, 1, 1); CheckRows(source, rect, 7, 4); CheckRows(source, rect, 12, 8);
        Debug.Log("INVENTORY_HUD_ROW_SPACING_PORT_CHECK_OK");
        UnityEngine.Object.DestroyImmediate(root);
    }
}
