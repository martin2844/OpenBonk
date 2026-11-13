#if UNITY_EDITOR
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

internal sealed class AndroidSplitLauncherBuild : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (!NativeAndroidBuild.SplitLauncherProbeEnabled) return;
        if (System.Environment.GetEnvironmentVariable("OPENBONK_ANDROID_TARGET_SDK") == "35")
        {
            string aapt2 = System.Environment.GetEnvironmentVariable("OPENBONK_AAPT2_OVERRIDE");
            if (string.IsNullOrEmpty(aapt2) || !File.Exists(aapt2) || aapt2.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new InvalidDataException("Android 35 aapt2 override is missing or invalid");
            string properties = Path.GetFullPath(Path.Combine(path, "../gradle.properties"));
            string text = File.ReadAllText(properties);
            const string key = "android.aapt2FromMavenOverride=";
            if (text.Contains(key)) throw new InvalidDataException("Gradle aapt2 override already exists");
            string escaped = aapt2.Replace("\\", "\\\\").Replace(" ", "\\ ").Replace(":", "\\:");
            File.AppendAllText(properties, "\n" + key + escaped + "\n");
            Debug.Log("OpenBonk: configured Android 35 resource tool");
        }
        string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
        var doc = new XmlDocument(); doc.Load(manifestPath);
        const string android = "http://schemas.android.com/apk/res/android";
        var application = doc.SelectSingleNode("/manifest/application") as XmlElement;
        if (application == null) throw new InvalidDataException("Unity application manifest missing");
        // Branding belongs to the final launcher manifest. Incremental Unity
        // builds can otherwise retain old values in the library manifest.
        application.RemoveAttribute("label", android);
        application.RemoveAttribute("icon", android);
        string launcherManifest = Path.GetFullPath(Path.Combine(path, "../launcher/src/main/AndroidManifest.xml"));
        var launcherDoc = new XmlDocument(); launcherDoc.Load(launcherManifest);
        var launcherApplication = launcherDoc.SelectSingleNode("/manifest/application") as XmlElement;
        if (launcherApplication == null) throw new InvalidDataException("Android launcher application missing");
        Set(launcherApplication, "icon", "@drawable/openbonk_icon", android);
        if (System.Environment.GetEnvironmentVariable("OPENBONK_OFFLINE_BETA") == "1")
        {
            const string tools = "http://schemas.android.com/tools";
            var permission = launcherDoc.CreateElement("uses-permission");
            Set(permission, "name", "android.permission.INTERNET", android);
            permission.SetAttribute("node", tools, "remove");
            launcherDoc.DocumentElement.InsertBefore(permission, launcherApplication);
            Debug.Log("OpenBonk: remove Android INTERNET permission for offline beta");
        }
        launcherDoc.Save(launcherManifest);
        XmlElement unityActivity = null;
        foreach (XmlNode node in application.SelectNodes("activity"))
            if (node is XmlElement element && element.GetAttribute("name", android) == "com.unity3d.player.UnityPlayerGameActivity") unityActivity = element;
        if (unityActivity == null) throw new InvalidDataException("Unity GameActivity missing");
        // Only the launcher in this app should start Unity. Exporting this
        // activity lets other apps bypass data checks and supply intent extras.
        Set(unityActivity, "exported", "false", android);
        var filters = unityActivity.SelectNodes("intent-filter");
        for (int i = filters.Count - 1; i >= 0; i--)
            if (filters[i].InnerXml.Contains("android.intent.action.MAIN")) unityActivity.RemoveChild(filters[i]);
        var activity = doc.CreateElement("activity");
        Set(activity, "name", "com.megabonk.launcher.SplitDataLauncher", android);
        Set(activity, "exported", "true", android);
        Set(activity, "screenOrientation", "userLandscape", android);
        Set(activity, "theme", "@android:style/Theme.Material.NoActionBar", android);
        var filter = doc.CreateElement("intent-filter");
        var action = doc.CreateElement("action"); Set(action, "name", "android.intent.action.MAIN", android);
        var category = doc.CreateElement("category"); Set(category, "name", "android.intent.category.LAUNCHER", android);
        filter.AppendChild(action); filter.AppendChild(category); activity.AppendChild(filter); application.AppendChild(activity);
        doc.Save(manifestPath);
        Debug.Log("Split-data probe: installed separate Android launcher activity");
    }
    private static void Set(XmlElement element, string name, string value, string ns) => element.SetAttribute(name, ns, value);
}
#endif
