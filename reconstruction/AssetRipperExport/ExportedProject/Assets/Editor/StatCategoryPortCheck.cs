using System;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;

public static class StatCategoryPortCheck
{
    public static void Run()
    {
        // Values decoded from GameAssembly.so at 0xb6fb9c, with the
        // original 0x6fffffff9fff validity mask applied.
        int[] expected = {
            1,1,1,1,1,1,1,1,1,0,0,0,0,6,6,0,0,0,0,0,0,0,0,0,
            0,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,0,0,6,0,2
        };
        for (int i = 0; i < expected.Length; i++)
            if ((int)StatUtility.GetStatCategory((EStat)i) != expected[i])
                throw new Exception("Incorrect category for stat " + (EStat)i);
        foreach (int value in new[] { -1, 47, 48, 56, int.MaxValue })
            if (StatUtility.GetStatCategory((EStat)value) != EStatCategory.Null)
                throw new Exception("Incorrect fallback for stat " + value);
        Debug.Log("StatCategoryPortCheck passed: all original table entries and fallback");
        EditorApplication.Exit(0);
    }
}
