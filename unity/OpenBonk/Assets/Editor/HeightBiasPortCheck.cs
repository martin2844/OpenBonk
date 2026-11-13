using System;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEngine;

public static class HeightBiasPortCheck
{
    static void Close(float actual, float expected, string label)
    {
        if (float.IsNaN(actual) || Mathf.Abs(actual - expected) > 0.0001f)
            throw new Exception(label + ": expected " + expected + ", got " + actual);
    }
    public static void Run()
    {
        var gaussian = new GaussianHeightBias();
        var linear = new LinearHeightBias();
        Close(gaussian.CalculateBias(2, 2, 5, 8, 2), 8, "gaussian center");
        Close(gaussian.CalculateBias(0, 0, 5, 8, 2), 8 * Mathf.Exp(-2), "gaussian corner");
        Close(gaussian.CalculateBias(0, 2, 5, 8, 2), 8 * Mathf.Exp(-1), "gaussian edge");
        Close(gaussian.CalculateBias(0, 0, 5, 8, 0), 8, "zero strictness");
        Close(linear.CalculateBias(2, 2, 5, 8, 9), 8, "linear center");
        Close(linear.CalculateBias(0, 0, 5, 8, 9), 0, "linear corner");
        Close(linear.CalculateBias(0, 2, 5, 8, 9), 8 * (1 - 1 / Mathf.Sqrt(2)), "linear edge");
        Close(linear.CalculateBias(0, 2, 5, 8, 0), linear.CalculateBias(0, 2, 5, 8, 9), "linear ignores strictness");
        if (!(linear.CalculateBias(-2, -2, 5, 8, 9) < 0)) throw new Exception("outside map remains unclamped");
        if (!float.IsNaN(gaussian.CalculateBias(0, 0, 1, 8, 2)) || !float.IsNaN(linear.CalculateBias(0, 0, 1, 8, 2)))
            throw new Exception("size one division behavior");
        Debug.Log("HeightBiasPortCheck passed: center, edges, corners, strictness, unclamped and degenerate cases");
    }
}
