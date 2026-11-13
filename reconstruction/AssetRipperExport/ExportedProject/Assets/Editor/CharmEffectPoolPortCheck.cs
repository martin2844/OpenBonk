using System;
using System.Reflection;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class CharmEffectPoolPortCheck
{
    public static void Run()
    {
        var oldInstance = PoolManager.Instance;
        float oldTime = MyTime.time;
        var root = new GameObject("charm effect pool fixture");
        root.SetActive(false);
        var manager = root.AddComponent<PoolManager>();
        manager.charmFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CharmFx.prefab");
        if (manager.charmFx == null) throw new Exception("Original charm prefab missing");
        GameObject effect = null;
        try
        {
            typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            if (manager.charmPool == null) throw new Exception("Charm pool was not initialized");
            MyTime.time = 100f;
            effect = manager.charmPool.Get();
            var returner = effect.GetComponent<ReturnToPool>();
            if (returner == null || returner.pool != manager.charmPool || !effect.activeSelf)
                throw new Exception("Effect is not configured for the charm pool");
            var update = typeof(ReturnToPool).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            MyTime.time = 100.49f;
            update.Invoke(returner, null);
            if (!effect.activeSelf || manager.charmPool.CountActive != 1) throw new Exception("Effect returned too early");
            MyTime.time = 100.5f;
            update.Invoke(returner, null);
            if (effect.activeSelf || manager.charmPool.CountInactive != 1) throw new Exception("Effect did not return at timeout");
            if (manager.charmPool.Get() != effect || !effect.activeSelf) throw new Exception("Effect was not reused");
            Debug.Log("CharmEffectPoolPortCheck passed: original prefab, timeout, release and reuse");
        }
        finally
        {
            if (effect != null) UnityEngine.Object.DestroyImmediate(effect);
            UnityEngine.Object.DestroyImmediate(root);
            PoolManager.Instance = oldInstance;
            MyTime.time = oldTime;
        }
    }
}
