using System;
using System.IO;
using System.Reflection;
using UnityEngine;

// Executes the actual Android startup method with a simulated activity/store.
// Run with UNITY_ANDROID defined; the Android and Unity dependencies below are stubs.
internal static class LauncherLocalizationRetryCheck
{
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Run() => typeof(AndroidLauncherLocalizationImport).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    private static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "openbonk-localization-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            AndroidJavaObject.Cache = root;
            string path = Path.Combine(root, "megabonk-original-texts.pending.zip");
            File.WriteAllText(path, "selected data");
            AndroidJavaObject.ImportRequested = false;
            Run();
            Require(File.Exists(path) && AndroidOriginalLocalizationStore.Calls == 0, "unrequested import changed data");
            AndroidJavaObject.ImportRequested = true;
            AndroidOriginalLocalizationStore.Fail = true;
            Run();
            Require(File.ReadAllText(path) == "selected data" && AndroidJavaClass.LastResult == false, "failed import lost its queued ZIP");
            AndroidOriginalLocalizationStore.Fail = false;
            Run();
            Require(!File.Exists(path) && AndroidJavaClass.LastResult == true && AndroidOriginalLocalizationStore.Calls == 2, "successful retry did not consume ZIP");
            Console.WriteLine("Localization startup: no request, failure preservation, and successful retry passed.");
        }
        finally { Directory.Delete(root, true); }
    }
}

public static class AndroidOriginalLocalizationStore
{
    public static bool Fail;
    public static int Calls;
    public static string Import(Stream stream, string root)
    {
        Calls++;
        if (Fail) throw new IOException("simulated temporary failure");
        if (stream.ReadByte() < 0) throw new IOException("empty source");
        return Path.Combine(root, "generation");
    }
}

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) {} }
    public static class Application { public static string persistentDataPath = Path.GetTempPath(); }
    public static class Debug { public static void Log(string s) {} public static void LogWarning(string s) {} }
    public class AndroidJavaObject : IDisposable
    {
        public static string Cache;
        public static bool ImportRequested;
        public T Call<T>(string method, params object[] args)
        {
            object result;
            switch (method)
            {
                case "getIntent": case "getCacheDir": result = new AndroidJavaObject(); break;
                case "getBooleanExtra": result = ImportRequested; break;
                case "getAbsolutePath": result = Cache; break;
                case "getPackageName": result = "example.another.package"; break;
                default: throw new Exception("unexpected call " + method);
            }
            return (T)result;
        }
        public void Dispose() {}
    }
    public sealed class AndroidJavaClass : AndroidJavaObject
    {
        public static bool? LastResult;
        public AndroidJavaClass(string name) {}
        public T GetStatic<T>(string name) => (T)(object)new AndroidJavaObject();
        public void CallStatic(string name, params object[] args) { LastResult = (bool)args[0]; }
    }
}
