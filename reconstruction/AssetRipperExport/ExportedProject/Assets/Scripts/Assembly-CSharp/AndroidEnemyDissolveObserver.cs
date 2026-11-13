#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;

// Observes normal enemy deaths; enabled only by an explicit test intent.
internal sealed class AndroidEnemyDissolveObserver : MonoBehaviour
{
    bool observing, completed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_enemy_dissolve_observer",false)) return;
        var go=new GameObject("enemy dissolve observer"); DontDestroyOnLoad(go); go.AddComponent<AndroidEnemyDissolveObserver>();
    }
    void OnEnable() => Enemy.A_EnemyDied += OnDeath;
    void OnDisable() => Enemy.A_EnemyDied -= OnDeath;
    void OnDeath(Enemy enemy, DamageContainer damage)
    {
        if(!completed && !observing && enemy != null && enemy.dissolve != null && enemy.dissolve.enemyRenderer != null)
            StartCoroutine(Observe(enemy));
    }
    IEnumerator Observe(Enemy enemy)
    {
        observing=true;
        var effect=enemy.dissolve; var renderer=effect.enemyRenderer; var material=renderer.sharedMaterial;
        bool finished=false; System.Action onFinished=()=>finished=true; effect.A_DissolveFinished += onFinished;
        var block=new MaterialPropertyBlock(); float maxAmount=0; int samples=0; float deadline=Time.realtimeSinceStartup+8f;
        while(Time.realtimeSinceStartup<deadline && !finished && enemy != null && enemy.gameObject.activeInHierarchy)
        {
            yield return null;
            if(renderer == null) break;
            renderer.GetPropertyBlock(block); float amount=block.GetFloat("_DissolveAmount");
            if(amount>maxAmount) maxAmount=amount;
            samples++;
        }
        effect.A_DissolveFinished -= onFinished;
        bool ok=material != null && material==effect.dissolveMaterial && !material.enableInstancing && samples>0 && maxAmount>0 && finished;
        Debug.Log($"Enemy dissolve observer {(ok?"PASS":"FAIL")}: enemy={enemy.name}, sourceMaterial={(material!=null?material.name:"null")}, instancing={(material!=null?material.enableInstancing:false)}, samples={samples}, maxAmount={maxAmount:F3}, finished={finished}, active={enemy.gameObject.activeInHierarchy}");
        completed=true; observing=false;
    }
}
#endif
