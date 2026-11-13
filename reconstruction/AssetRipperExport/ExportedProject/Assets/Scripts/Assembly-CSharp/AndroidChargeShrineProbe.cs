#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.InGame.Rewards.Effects;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in test of a generated shrine's trigger, normal timer, completion and reward window.
internal sealed class AndroidChargeShrineProbe : MonoBehaviour
{
    bool addWrench;
    bool addBeacon;
    bool declineReuse;
    bool interruptCharge;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_charge_shrine_probe", false)) return;
        var go = new GameObject("Charge shrine probe"); DontDestroyOnLoad(go); var probe = go.AddComponent<AndroidChargeShrineProbe>();
        probe.addWrench = intent.Call<bool>("getBooleanExtra", "megabonk_charge_shrine_wrench_probe", false);
        probe.addBeacon = intent.Call<bool>("getBooleanExtra", "megabonk_charge_shrine_beacon_probe", false);
        probe.declineReuse = intent.Call<bool>("getBooleanExtra", "megabonk_decline_reuse_probe", false);
        probe.interruptCharge = intent.Call<bool>("getBooleanExtra", "megabonk_charge_shrine_interrupt_probe", false);
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f; ChargeShrine shrine = null;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || (shrine = FindObjectOfType<ChargeShrine>()) == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || manager == null || shrine == null) { Debug.LogError("Charge shrine probe: scene or generated shrine unavailable"); yield break; }
        if (addWrench) player.inventory.itemInventory.AddItem(EItem.Wrench, 2);
        if (addBeacon) player.inventory.itemInventory.AddItem(EItem.Beacon);
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var body = player.playerMovement.GetRb(); var oldConstraints = body.constraints;
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        int count = 0; bool uninterrupted = false; void OnCharged(bool value) { count++; uninterrupted = value; }
        ChargeShrine.A_Charged += OnCharged;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var charging = typeof(ChargeShrine).GetField("charging", flags);
        var progress = typeof(ChargeShrine).GetField("chargeProgress", flags);
        var completed = typeof(ChargeShrine).GetField("completed", flags);
        var reward = typeof(ChargeShrine).GetField("rewardGiven", flags);
        StartCoroutine(ObserveEmission(shrine, progress));
        body.position = shrine.transform.position + Vector3.up * 1f; body.velocity = Vector3.zero; Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); body.constraints = RigidbodyConstraints.FreezeAll;
        Debug.Log($"Charge shrine probe: generated={shrine.name}, charging={charging.GetValue(shrine)}, pos={body.position}, shrinePos={shrine.transform.position}");
        if (interruptCharge)
        {
            deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && (float)progress.GetValue(shrine) < .5f) yield return null;
            float beforeExit = (float)progress.GetValue(shrine);
            body.position = shrine.transform.position + Vector3.right * 20f + Vector3.up; Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline && (float)progress.GetValue(shrine) > .05f) yield return null;
            float afterExit = (float)progress.GetValue(shrine); bool stopped = !(bool)charging.GetValue(shrine);
            body.position = shrine.transform.position + Vector3.up; Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            deadline = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < deadline && !(bool)completed.GetValue(shrine)) yield return null;
            bool ok = beforeExit >= .5f && stopped && afterExit < beforeExit && (bool)completed.GetValue(shrine) && count == 1 && !uninterrupted;
            Debug.Log($"Charge shrine interruption probe {(ok ? "PASS" : "FAIL")}: before={beforeExit:F3}, after={afterExit:F3}, stopped={stopped}, completed={completed.GetValue(shrine)}, events={count}, uninterrupted={uninterrupted}");
        }
        yield return new WaitForSecondsRealtime(12f);
        bool done = (bool)completed.GetValue(shrine), gave = (bool)reward.GetValue(shrine);
        Debug.Log($"Charge shrine probe {(done && gave && count == 1 ? "PASS" : "FAIL")}: progress={progress.GetValue(shrine)}, completed={done}, rewardGiven={gave}, chargedEvents={count}, uninterrupted={uninterrupted}, paused={Assets.Scripts.Utility.MyTime.paused}");
        var before = new Dictionary<EStat, float>(player.inventory.playerStats.stats);
        if (declineReuse)
        {
            var window = FindObjectOfType<EncounterUi>();
            var buttons = window != null ? window.GetComponentsInChildren<EncounterButton>(false) : null;
            var button = buttons == null ? null : System.Array.Find(buttons, b => b.index == 3);
            if (button == null) Debug.LogError("Decline reuse probe: decline button unavailable");
            else
            {
                var denied = new EncounterOffer { effects = new[] { new EffectStat { effectType = EEncounterEffect.EGold, value = -1000000f } } };
                var eligible = typeof(EncounterButton).GetField("canAccept", flags);
                button.SetOffer(denied, false);
                bool rejected = !(bool)eligible.GetValue(button);
                button.SetDeclineOffer(3);
                bool canDecline = (bool)eligible.GetValue(button);
                Debug.Log($"Decline reuse probe: rejected={rejected}, canDecline={canDecline}, index={button.index}");
            }
        }
        EncounterOffer[] offers = null;
        if (addWrench)
        {
            var window = FindObjectOfType<EncounterUi>();
            offers = window == null ? null : (EncounterOffer[])typeof(EncounterUi).GetField("offers", flags).GetValue(window);
            var wrench = player.inventory.itemInventory.GetItem(EItem.Wrench) as ItemWrench;
            var getBase = typeof(EncounterUtility).GetMethod("GetRandomStatValue", BindingFlags.NonPublic | BindingFlags.Static);
            if (offers == null || wrench == null || getBase == null) Debug.LogError("Charge shrine wrench probe: offers unavailable");
            else foreach (var offer in offers)
            {
                var modifier = offer.effects[0].statModifier;
                object[] args = { modifier.stat, EStatModifyType.Flat };
                float raw = (float)getBase.Invoke(null, args);
                float expected = StatUtility.GetRarityValue(raw, offer.rarity, 3) * wrench.GetChargeRewardMultiplier();
                Debug.Log($"Charge shrine wrench offer: stat={modifier.stat}, rarity={offer.rarity}, actual={modifier.modification:F5}, expected={expected:F5}, matches={Mathf.Abs(modifier.modification - expected) < .0001f}");
            }
        }
        deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline && MyTime.paused) yield return null;
        if (MyTime.paused) Debug.LogError("Charge shrine probe: reward selection timed out");
        else
        {
            yield return new WaitForSecondsRealtime(1f);
            var changes = new List<string>();
            foreach (var pair in player.inventory.playerStats.stats)
                if (before.TryGetValue(pair.Key, out float old) && Mathf.Abs(pair.Value - old) > .0001f) changes.Add($"{pair.Key}:{old:F3}->{pair.Value:F3}");
            Debug.Log($"Charge shrine probe selection: paused={MyTime.paused}, changed=[{string.Join(", ", changes)}]");
            if (declineReuse) Debug.Log($"Decline reuse probe: closed={!MyTime.paused}, statChanges={changes.Count}");
            if (offers != null) foreach (var offer in offers)
            {
                var modifier = offer.effects[0].statModifier;
                var permanent = player.inventory.statInventory.permanentChanges;
                if (permanent.TryGetValue(modifier.stat, out var list) && list.Contains(modifier))
                    Debug.Log($"Charge shrine wrench selected: stat={modifier.stat}, modifier={modifier.modification:F5}, count={list.FindAll(x => ReferenceEquals(x, modifier)).Count}, current={player.inventory.playerStats.GetStat(modifier.stat):F5}");
            }
        }
        if (addBeacon && !MyTime.paused)
        {
            var zone = shrine.healingZone;
            var health = player.inventory.playerHealth;
            int oldHp = health.hp;
            health.hp = Mathf.Max(1, health.maxHp / 2);
            int injuredHp = health.hp;
            bool active = zone != null && zone.activeInHierarchy;
            float distance = zone == null ? -1f : Vector3.Distance(player.transform.position, zone.transform.position);
            yield return new WaitForSecondsRealtime(3f);
            int healedHp = health.hp;
            player.inventory.itemInventory.RemoveItem(EItem.Beacon);
            bool removed = zone != null && !zone.activeSelf;
            Debug.Log($"Charge shrine beacon probe {(active && healedHp > injuredHp && removed ? "PASS" : "FAIL")}: active={active}, distance={distance:F2}, hp={injuredHp}->{healedHp}, removed={removed}");
            health.hp = oldHp;
        }
        if (addWrench) { player.inventory.itemInventory.RemoveItem(EItem.Wrench); player.inventory.itemInventory.RemoveItem(EItem.Wrench); }
        ChargeShrine.A_Charged -= OnCharged; body.constraints = oldConstraints; invincible.SetValue(player, oldInvincible); manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
    }
    IEnumerator ObserveEmission(ChargeShrine shrine, FieldInfo progressField)
    {
        var block = new MaterialPropertyBlock(); shrine.meshRenderer.GetPropertyBlock(block);
        Color initial = block.GetColor("_EmissionColor");
        float deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && (float)progressField.GetValue(shrine) < .35f) yield return null;
        float progress = (float)progressField.GetValue(shrine); shrine.meshRenderer.GetPropertyBlock(block);
        Color charged = block.GetColor("_EmissionColor");
        var colorField = typeof(ChargeShrine).GetField("zoneColor", BindingFlags.Instance | BindingFlags.NonPublic);
        Color expected = (Color)colorField.GetValue(shrine) * (3f * progress);
        var source = shrine.meshRenderer.sharedMaterial;
        bool state = progress >= .35f && initial.maxColorComponent < .001f && Mathf.Abs(charged.r - expected.r) < .01f && Mathf.Abs(charged.g - expected.g) < .01f && Mathf.Abs(charged.b - expected.b) < .01f && source.IsKeywordEnabled("_MK_EMISSION") && source.IsKeywordEnabled("_MK_EMISSION_MAP");
        var mat = new Material(source); mat.SetFloat("_Outline", 1); mat.SetFloat("_Rim", 0); mat.SetFloat("_RenderFace", 0); mat.SetColor("_AlbedoColor", Color.black); mat.SetFloat("_IndirectFade", 1);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.layer = 30; quad.transform.position = new Vector3(0,10000,0); quad.transform.localScale = Vector3.one * 2; var renderer = quad.GetComponent<Renderer>(); renderer.sharedMaterial = mat;
        var cameraObject = new GameObject("shrine emission camera"); cameraObject.transform.position = new Vector3(0,10000,-4); var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1; camera.aspect = 1; camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image = new Texture2D(64,64,TextureFormat.RGBA32,false); camera.targetTexture = target; var previous = RenderTexture.active;
        Color[] Read(Color color) { block.SetColor("_EmissionColor", color); renderer.SetPropertyBlock(block); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixels(); }
        Color[] off = Read(Color.black), on = Read(charged); int changed = 0;
        for (int i = 0; i < on.Length; i++) if (on[i].r > off[i].r + .02f || on[i].g > off[i].g + .02f || on[i].b > off[i].b + .02f) changed++;
        Debug.Log($"Charge shrine emission probe {(state && changed > 0 ? "PASS" : "FAIL")}: state={state} progress={progress:F3} initial={initial} charged={charged} expected={expected} material={source.name} changedPixels={changed}");
        RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(image); Destroy(cameraObject); Destroy(quad); Destroy(mat);
    }
}
#endif
