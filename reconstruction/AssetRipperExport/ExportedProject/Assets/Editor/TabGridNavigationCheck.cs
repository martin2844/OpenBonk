using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TabGridNavigationCheck
{
    static void Check(bool value,string message) { if (!value) throw new Exception("TabGridNavigationCheck: " + message); }
    public static void Run() {
        var root = new GameObject("grid",typeof(RectTransform)); var navigation = root.AddComponent<TabGridNavigation>(); navigation.buttonsParent = root;
        var grid = root.AddComponent<GridLayoutGroup>(); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
        var tab = new GameObject("back",typeof(RectTransform),typeof(Button)).GetComponent<Button>();
        var buttons = new List<Button>();
        for (int i=0;i<7;i++) { var go = new GameObject("item"+i,typeof(RectTransform),typeof(Button)); go.transform.SetParent(root.transform,false); buttons.Add(go.GetComponent<Button>()); }
        var ignored = new GameObject("inactive",typeof(RectTransform),typeof(Button)); ignored.transform.SetParent(root.transform,false); ignored.SetActive(false);
        navigation.Set(tab);
        Check(buttons[0].navigation.mode == Navigation.Mode.Explicit && buttons[0].navigation.selectOnUp == tab && buttons[0].navigation.selectOnRight == buttons[1] && buttons[0].navigation.selectOnDown == buttons[3],"first cell");
        Check(buttons[2].navigation.selectOnRight == null && buttons[2].navigation.selectOnDown == buttons[5],"right edge");
        Check(buttons[4].navigation.selectOnUp == buttons[1] && buttons[4].navigation.selectOnDown == buttons[6] && buttons[4].navigation.selectOnLeft == buttons[3] && buttons[4].navigation.selectOnRight == buttons[5],"short last row");
        Check(buttons[6].navigation.selectOnUp == buttons[3] && buttons[6].navigation.selectOnDown == null && buttons[6].navigation.selectOnRight == null,"last cell");
        UnityEngine.Object.DestroyImmediate(grid);
        for (int i=0;i<buttons.Count;i++) buttons[i].transform.localPosition = new Vector3((i%2)*100, -(i/2)*100,0);
        navigation.Set(tab);
        Check(buttons[0].navigation.selectOnDown == buttons[2] && buttons[1].navigation.selectOnRight == null && buttons[5].navigation.selectOnDown == buttons[6],"row count inferred from positions");
        UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(tab.gameObject);
        Debug.Log("TabGridNavigationCheck PASSED: fixed grid, partial rows, inactive entries and inferred columns");
    }
}
