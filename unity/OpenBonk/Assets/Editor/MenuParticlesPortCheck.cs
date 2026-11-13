using System;
using System.Reflection;
using Coffee.UIExtensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MenuParticlesPortCheck
{
    public static void RunScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Additive);
        try
        {
            MenuParticles manager = null;
            foreach (var root in scene.GetRootGameObjects())
                if ((manager = root.GetComponentInChildren<MenuParticles>(true)) != null) break;
            if (manager == null || manager.coinAttractor == null || manager.coinEffect == null || manager.cointEffectParent == null)
                throw new Exception("Menu particle scene references missing");
            if (manager.coinEffect.GetComponent<UIParticle>() == null || manager.coinEffect.GetComponent<DestroyObject>() == null ||
                manager.coinEffect.transform.childCount == 0 || manager.coinEffect.transform.GetChild(0).GetComponent<ParticleSystem>() == null)
                throw new Exception("Original coin effect components missing");
            Debug.Log("MenuParticlesPortCheck scene passed: actual Android scene and coin prefab references");
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    public static void Run()
    {
        var old = MenuParticles.Instance;
        var root = new GameObject("menu particle test");
        var prefab = new GameObject("coin effect");
        try
        {
            var child = new GameObject("particles");
            child.transform.SetParent(prefab.transform);
            child.AddComponent<ParticleSystem>();
            prefab.AddComponent<DestroyObject>();
            var manager = root.AddComponent<MenuParticles>();
            manager.coinEffect = prefab;
            manager.cointEffectParent = root;
            manager.coinAttractor = root.AddComponent<UIParticleAttractor>();
            typeof(MenuParticles).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            if (MenuParticles.Instance != manager) throw new Exception("Singleton not initialized");
            Vector3 at = new Vector3(2, 3, 4);
            manager.CoinEffect(at);
            if (root.transform.childCount != 1) throw new Exception("Effect was not instantiated");
            Transform effect = root.transform.GetChild(0);
            if (effect.position != at) throw new Exception("Effect position differs");
            var destroy = effect.GetComponent<DestroyObject>();
            if (destroy == null || destroy.OnDestroy == null) throw new Exception("Attractor cleanup was not registered");
            destroy.OnDestroy.Invoke();
            Debug.Log("MenuParticlesPortCheck passed: singleton, effect instantiation, position and cleanup callback");
        }
        finally
        {
            MenuParticles.Instance = old;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }
}
