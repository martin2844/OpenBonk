#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections.Generic;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;

// Adds disposable map entries only when explicitly requested on an isolated device.
internal static class AndroidManyMapsProbe
{
    private static readonly List<OpenBonkProceduralMapBuild> Builds = new List<OpenBonkProceduralMapBuild>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "openbonk_many_maps_probe", false)) return;
        DataManager.A_DataLoaded += AddMaps;
    }
    private static void AddMaps()
    {
        DataManager.A_DataLoaded -= AddMaps;
        var data = DataManager.Instance; var forest = data.GetMap(EMap.Forest);
        if (forest == null || forest.icon == null) { Debug.LogError("Many maps probe: Forest unavailable"); return; }
        for (int i = 0; i < 7; i++)
        {
            var recipe = new OpenBonkProceduralMapRecipe { name = "Test Map " + (i + 1), description = "Disposable navigation test", tileGridSize = 9, volatility = 1, slopeStrength = 0, flatMapBias = 0, stageDuration = 600 };
            var build = OpenBonkProceduralMapBuild.Create(forest, recipe, forest.icon);
            data.RegisterExternalMap("openbonk.navigation.test", "map" + i, build.Map); Builds.Add(build);
        }
        Debug.Log("Many maps probe: registered 7 disposable entries");
    }
}
#endif
