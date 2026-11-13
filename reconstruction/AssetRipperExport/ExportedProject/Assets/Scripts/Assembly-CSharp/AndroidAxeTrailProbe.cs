#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.Rendering;

// Opt-in check of the real Axe projectile's live TrailRenderer.
internal sealed class AndroidAxeTrailProbe : MonoBehaviour
{
    ProjectileAxe axe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_axe_trail_probe",false))return;
        var go=new GameObject("Axe trail probe");DontDestroyOnLoad(go);go.AddComponent<AndroidAxeTrailProbe>();
    }
    void OnSpawn(ProjectileBase projectile){if(projectile is ProjectileAxe candidate) axe=candidate;}
    IEnumerator Start() {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance==null || DataManager.Instance==null)) yield return null;
        var player=MyPlayer.Instance;
        if(player==null || player.inventory==null || EnemyManager.Instance==null || DataManager.Instance==null) {Debug.LogError("Axe trail probe INCONCLUSIVE: game unavailable");yield break;}
        EnemyManager.Instance.enabledWaves=false;
        foreach(var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,float.MaxValue);
        var data=DataManager.Instance.GetWeapon(EWeapon.Axe);
        if(data==null){Debug.LogError("Axe trail probe INCONCLUSIVE: weapon unavailable");yield break;}
        var enemyData=DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin);
        var enemy=enemyData==null ? null : EnemyManager.Instance.SpawnEnemy(enemyData,player.transform.position+player.playerRenderer.transform.forward*3f,-1,true,canBeElite:false);
        if(enemy==null){Debug.LogError("Axe trail probe INCONCLUSIVE: target unavailable");yield break;}
        enemy.enemyMovement.StopMovement();
        WeaponAttack.A_SpawnedProjectileSuccessfully+=OnSpawn;
        player.inventory.weaponInventory.AddWeapon(data,null);
        deadline=Time.realtimeSinceStartup+30f;TrailRenderer trail=null;
        while(Time.realtimeSinceStartup<deadline) {
            if(axe!=null && axe.gameObject.activeInHierarchy) {trail=axe.trailRenderer;if(trail!=null && trail.positionCount>=4)break;}
            yield return null;
        }
        WeaponAttack.A_SpawnedProjectileSuccessfully-=OnSpawn;
        if(axe==null || trail==null || !axe.gameObject.activeInHierarchy || trail.positionCount<4) {Debug.LogError("Axe trail probe INCONCLUSIVE: live trail unavailable");yield break;}
        var mat=trail.sharedMaterial; int positions=trail.positionCount; var bounds=trail.bounds;
        var camObj=new GameObject("Axe trail camera");var camera=camObj.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=Mathf.Max(2f,bounds.extents.magnitude*1.8f);
        camera.transform.position=bounds.center+new Vector3(0,10,-6);camera.transform.LookAt(bounds.center);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.blue;camera.cullingMask=1<<31;camera.allowHDR=false;camera.allowMSAA=false;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);var image=new Texture2D(256,256,TextureFormat.RGBA32,false);camera.targetTexture=target;
        int oldLayer=trail.gameObject.layer;bool oldEnabled=trail.enabled,oldFog=RenderSettings.fog;trail.gameObject.layer=31;RenderSettings.fog=false;
        try {
            Color32[] Capture(){camera.Render();var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();RenderTexture.active=old;return image.GetPixels32();}
            trail.enabled=false;var background=Capture();trail.enabled=true;var visible=Capture();int pixels=0;
            for(int i=0;i<visible.Length;i++){var a=background[i];var b=visible[i];if(System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b)>12)pixels++;}
            bool correct=mat!=null && mat.shader!=null && mat.shader.name=="Archanor VFX/Retro Arsenal/Beam" && mat.passCount>=2 && trail.shadowCastingMode==ShadowCastingMode.On && pixels>10;
            Debug.Log($"Axe trail probe {(correct?"PASS":"FAIL")}: positions={positions}, material={(mat!=null?mat.name:"null")}, shader={(mat!=null && mat.shader!=null?mat.shader.name:"null")}, passes={(mat!=null?mat.passCount:0)}, cast={trail.shadowCastingMode}, visiblePixels={pixels}");
            CheckActualShadow(trail,camera,bounds.center);
        } finally {trail.gameObject.layer=oldLayer;trail.enabled=oldEnabled;RenderSettings.fog=oldFog;camera.targetTexture=null;Destroy(camObj);Destroy(image);Destroy(target);}
    }
    static void CheckActualShadow(TrailRenderer trail,Camera camera,Vector3 center) {
        var shader=Shader.Find("MK/Toon/Built-in/Standard/Simple + Outline");
        if(shader==null || !shader.isSupported){Debug.LogError("Axe trail shadow probe INCONCLUSIVE: receiver shader unavailable");return;}
        var material=new Material(shader);material.SetTexture("_AlbedoMap",Texture2D.whiteTexture);material.SetColor("_AlbedoColor",Color.white);material.SetColor("_GoochBrightColor",Color.white);material.SetColor("_GoochDarkColor",Color.black);material.SetFloat("_LightThreshold",.05f);material.SetFloat("_DiffuseSmoothness",.1f);material.SetFloat("_Specular",0);material.SetFloat("_Rim",0);material.SetFloat("_Outline",1);material.SetFloat("_ReceiveShadows",1);
        var receiver=GameObject.CreatePrimitive(PrimitiveType.Quad);receiver.layer=31;receiver.transform.position=center+Vector3.forward;receiver.transform.localScale=Vector3.one*8;receiver.GetComponent<Renderer>().sharedMaterial=material;
        var lightObject=new GameObject("Axe trail shadow light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<31;light.renderMode=LightRenderMode.ForcePixel;light.transform.rotation=Quaternion.LookRotation(new Vector3(-.8f,0,1));light.intensity=1;light.shadowStrength=1;light.shadowBias=.01f;light.shadowNormalBias=0;
        var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;float oldSize=camera.orthographicSize;var oldCast=trail.shadowCastingMode;
        var oldMode=RenderSettings.ambientMode;var oldAmbient=RenderSettings.ambientLight;var oldShadows=QualitySettings.shadows;var oldDistance=QualitySettings.shadowDistance;var oldCascades=QualitySettings.shadowCascades;
        camera.transform.position=center+Vector3.back*6;camera.transform.LookAt(center);camera.orthographicSize=4;camera.renderingPath=RenderingPath.Forward;trail.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowDistance=30;QualitySettings.shadowCascades=1;
        var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try {
            Color[] Capture(){camera.Render();var old=RenderTexture.active;RenderTexture.active=camera.targetTexture;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();RenderTexture.active=old;return image.GetPixels();}
            light.shadows=LightShadows.None;var reference=Capture();light.shadows=LightShadows.Hard;var shadow=Capture();int darker=0;
            for(int i=0;i<reference.Length;i++)if(reference[i].r-shadow[i].r>.1f)darker++;
            Debug.Log($"Axe trail shadow probe {(darker>5?"PASS":"INCONCLUSIVE")}: shadowPixels={darker}, realTrail=true, positions={trail.positionCount}");
        } finally {
            camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;camera.orthographicSize=oldSize;trail.shadowCastingMode=oldCast;RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;QualitySettings.shadows=oldShadows;QualitySettings.shadowDistance=oldDistance;QualitySettings.shadowCascades=oldCascades;Destroy(image);Destroy(receiver);Destroy(material);Destroy(lightObject);
        }
    }
    void OnDestroy(){WeaponAttack.A_SpawnedProjectileSuccessfully-=OnSpawn;}
}
#endif
