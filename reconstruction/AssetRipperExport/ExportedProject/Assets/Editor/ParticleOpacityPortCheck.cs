using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Particles___Effects.ParticleOpacity;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class ParticleOpacityPortCheck
{
    static void Check(bool v, string m) { if (!v) throw new Exception("ParticleOpacityPortCheck: " + m); }
    static void Near(float a,float b,string m) => Check(Mathf.Abs(a-b)<.004f,m+": "+a+" expected "+b);
    public static void Run()
    {
        var root = new GameObject("particle opacity fixture"); root.SetActive(false);
        var save = root.AddComponent<SaveManager>(); save.config = new ConfigSaveFile();
        var singleton = typeof(SaveManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetSetMethod(true);
        var oldSave = SaveManager.Instance; var oldPause = MyTime.paused; var oldEvent = CurrentSettings.A_SettingUpdated;
        var oldGame = GameManager.Instance; var oldPlayer = MyPlayer.Instance;
        WeaponData weaponData = null;
        var p = root.AddComponent<ParticleSystem>(); var main=p.main; main.startColor=new Color(.4f,.5f,.6f,.8f);
        var trail = root.AddComponent<TrailRenderer>(); trail.startColor=new Color(1,1,1,.6f); trail.endColor=new Color(1,1,1,.4f);
        var line = root.AddComponent<LineRenderer>(); line.startColor=new Color(1,1,1,.3f); line.endColor=new Color(1,1,1,.2f);
        var opacity = root.AddComponent<ParticleOpacity>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var refresh=typeof(ParticleOpacity).GetMethod("Refresh", flags, null, new[]{typeof(bool)}, null);
        var update=typeof(ParticleOpacity).GetMethod("Update", flags);
        var awake=typeof(ParticleOpacity).GetMethod("Awake", flags);
        var destroy=typeof(ParticleOpacity).GetMethod("OnDestroy", flags);
        try
        {
            singleton.Invoke(null,new object[]{save}); MyTime.paused=false;
            opacity.TryValidate(); opacity.meshRenderer = Array.Empty<MeshRenderer>();
            Check(opacity.particleSystems.Length==1 && opacity.trails.Length==1 && opacity.lines.Length==1,"component validation");
            opacity.subscribeToOpacityChange=true; awake.Invoke(opacity,null);
            save.config.cfVisualsSettings.particle_opacity=1f; refresh.Invoke(opacity,new object[]{true});
            save.config.cfVisualsSettings.particle_opacity=.5f; refresh.Invoke(opacity,new object[]{true});
            Near(p.main.startColor.color.a,.4f,"particle alpha"); Near(trail.startColor.a,.3f,"trail start"); Near(trail.endColor.a,.2f,"trail end");
            Near(line.startColor.a,.15f,"line start"); Near(line.endColor.a,.1f,"line end");
            save.config.cfVisualsSettings.particle_opacity=0; refresh.Invoke(opacity,new object[]{true});
            Check(!p.emission.enabled && !trail.enabled && !line.enabled,"zero hides effects");
            save.config.cfVisualsSettings.particle_opacity=.75f; refresh.Invoke(opacity,new object[]{true});
            Check(p.emission.enabled && trail.enabled && line.enabled,"nonzero restores effects"); Near(p.main.startColor.color.a,.6f,"restores default alpha");
            opacity.opacityCurve=ParticleOpacity.EOpacityCurve.OutCirc;
            save.config.cfVisualsSettings.particle_opacity=.5f; refresh.Invoke(opacity,new object[]{true}); Near(p.main.startColor.color.a,.7f,"original cubic curve");
            opacity.opacityCurve=ParticleOpacity.EOpacityCurve.Linear; MyTime.paused=true;
            save.config.cfVisualsSettings.particle_opacity=.25f; CurrentSettings.A_SettingUpdated?.Invoke("particle_opacity",null,null); update.Invoke(opacity,null);
            Near(p.main.startColor.color.a,.7f,"paused defers setting"); MyTime.paused=false; update.Invoke(opacity,null);
            Near(p.main.startColor.color.a,.2f,"setting event forces update");
            destroy.Invoke(opacity,null); Check(CurrentSettings.A_SettingUpdated==oldEvent,"setting unsubscribe");
            var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; GameManager.Instance=root.AddComponent<GameManager>();
            player.inventory=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory.playerStats=(PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            player.inventory.playerStats.stats=new Dictionary<EStat,float>{{EStat.SizeMultiplier,1f}};
            weaponData=ScriptableObject.CreateInstance<WeaponData>(); weaponData.maxSizeMultiplier=1f;
            var weapon=(WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase)); weapon.weaponData=weaponData;
            var weaponStats=new Dictionary<EStat,float>{{EStat.SizeMultiplier,1f}};
            typeof(WeaponBase).GetField("weaponStats",flags).SetValue(weapon,weaponStats);
            var aura=root.AddComponent<IceAura>(); aura.weaponBase=weapon;
            opacity.useProjectileAutoOpacity=true; opacity.useScaleWithoutProjectileData=true; opacity.constantAttack=aura;
            var auto=typeof(ParticleOpacity).GetMethod("GetAutoOpacity",flags);
            float Auto()=> (float)auto.Invoke(opacity,null);
            save.config.cfVisualsSettings.particle_auto_opacity=1;
            Near(Auto(),1f,"auto below threshold"); weaponStats[EStat.SizeMultiplier]=6f; Near(Auto(),.55f,"auto midpoint");
            weaponStats[EStat.SizeMultiplier]=8f; Near(Auto(),.1f,"auto maximum"); weaponStats[EStat.SizeMultiplier]=20f; Near(Auto(),.1f,"auto clamps");
            save.config.cfVisualsSettings.particle_auto_opacity=0; Near(Auto(),1f,"auto setting disabled");
            save.config.cfVisualsSettings.particle_auto_opacity=1; opacity.useScaleWithoutProjectileData=false; Near(Auto(),1f,"missing projectile stays opaque");
            foreach (string prefabName in new[] { "AegisAttack", "IceRing" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + prefabName + ".prefab");
                Check(prefab != null, prefabName + " prefab");
                var components = prefab.GetComponentsInChildren<ParticleOpacity>(true);
                Check(components.Length > 0, prefabName + " opacity component");
                foreach (var component in components)
                {
                    Check(component.particleSystems != null && component.defaultOpacitiesParticles != null && component.particleSystems.Length == component.defaultOpacitiesParticles.Length, prefabName + " particle alpha wiring");
                    for (int i = 0; i < component.particleSystems.Length; i++) Check(component.particleSystems[i] != null, prefabName + " particle reference " + i);
                }
                var clone=UnityEngine.Object.Instantiate(prefab,root.transform);
                try
                {
                    save.config.cfVisualsSettings.particle_opacity=.35f;
                    foreach(var component in clone.GetComponentsInChildren<ParticleOpacity>(true))
                    {
                        refresh.Invoke(component,new object[]{true});
                        for(int i=0;i<component.particleSystems.Length;i++)
                            Near(component.particleSystems[i].main.startColor.color.a,component.defaultOpacitiesParticles[i]*.35f,prefabName+" actual particle "+i);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(clone); }
            }
            Debug.Log("ParticleOpacityPortCheck passed: default alphas, curves, hide, restore, settings, auto size and real prefabs");
        }
        finally { CurrentSettings.A_SettingUpdated=oldEvent; singleton.Invoke(null,new object[]{oldSave}); MyTime.paused=oldPause; GameManager.Instance=oldGame; MyPlayer.Instance=oldPlayer; UnityEngine.Object.DestroyImmediate(root); if(weaponData!=null) UnityEngine.Object.DestroyImmediate(weaponData); }
    }
}
