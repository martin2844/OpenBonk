#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Opt-in native-runtime smoke test; no mod loading or game API yet.
internal static class AndroidLuaRuntimeProbe
{
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_eval(byte[] source, out double value, byte[] error, UIntPtr errorSize);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Run()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_lua_probe", false)) return;
        try
        {
            Check("arithmetic", "return math.max(2, 4)*3", 0, 12, null);
            Check("file access", "return os.execute('id')", 1, 0, "nil value");
            Check("instruction limit", "while true do end", 1, 0, "instruction limit");
            Check("memory limit", "local s='x' while true do s=s..s end", 1, 0, "memory");
            Check("recovery", "return 7", 0, 7, null);
        }
        catch (Exception e) { Debug.LogError("Lua runtime probe FAIL: " + e); }
    }

    private static void Check(string name, string code, int expected, double result, string errorText)
    {
        var bytes = Encoding.UTF8.GetBytes(code + "\0");
        var error = new byte[256];
        int status = megabonk_lua_eval(bytes, out double value, error, (UIntPtr)error.Length);
        int end = Array.IndexOf(error, (byte)0);
        string message = Encoding.UTF8.GetString(error, 0, end < 0 ? error.Length : end);
        bool ok = status == expected && (status == 0 ? Math.Abs(value - result) < .0001 : message.Contains(errorText));
        Debug.Log($"Lua runtime probe {(ok ? "PASS" : "FAIL")}: {name} result={value} error={message}");
    }
}
#endif
