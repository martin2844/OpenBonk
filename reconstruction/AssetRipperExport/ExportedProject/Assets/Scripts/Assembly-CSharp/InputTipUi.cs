using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Utility;
using Assets.Scripts.Utility.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class InputTipUi : MonoBehaviour
{
    public MyGlyphDisplay glyphContainer;
    public TextMeshProUGUI t_tip;
    public CanvasGroup group;
    public AudioSource audio;
    private Vector3 defaultPosition;
    private string currentAction;
    private float timeout = 5f;
    private float fadeTime = .6f;
    private float delay = .5f;
    private bool isShowingTip;
    private bool skipping;
    private Queue<InputTip> tipQueue = new Queue<InputTip>();
#if UNITY_ANDROID && !UNITY_EDITOR
    private bool diagnoseStartup;
    private float nextDiagnosticTime;
#endif

    private void Awake()
    {
        defaultPosition = transform.localPosition;
        WeaponInventory.A_WeaponAdded += OnWeaponAdded;
        GameManager.A_RunStarted += OnRunStarted;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            diagnoseStartup = intent.Call<bool>("getBooleanExtra", "megabonk_input_tip_startup_probe", false);
        if (diagnoseStartup) Debug.Log("Input tip startup: Awake subscribed");
#endif
        // The HUD can be activated by the portal after GameManager.Start.
        // Replay only events this newly awakened component could have missed.
        if (GameManager.Instance != null && GameManager.Instance.HasRaisedRunStarted)
        {
            OnRunStarted();
            var weapons = MyPlayer.Instance != null && MyPlayer.Instance.inventory != null
                ? MyPlayer.Instance.inventory.weaponInventory : null;
            if (weapons != null && weapons.weapons != null)
                foreach (var weapon in weapons.weapons.Values) OnWeaponAdded(weapon);
        }
    }

    private void OnDestroy()
    {
        WeaponInventory.A_WeaponAdded -= OnWeaponAdded;
        GameManager.A_RunStarted -= OnRunStarted;
    }

    private static bool TipsEnabled()
    {
        return SaveManager.Instance != null && SaveManager.Instance.config != null &&
            SaveManager.Instance.config.cfGameSettings != null && SaveManager.Instance.config.cfGameSettings.show_tips != 0;
    }

    public void SetTip(string tip, string action, float extraDelay = 0f)
    {
        if (TipsEnabled()) tipQueue.Enqueue(new InputTip(tip, action, extraDelay));
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (diagnoseStartup && Time.realtimeSinceStartup >= nextDiagnosticTime)
        {
            nextDiagnosticTime = Time.realtimeSinceStartup + 3f;
            Debug.Log($"Input tip startup: enabled={TipsEnabled()}, paused={MyTime.paused}, queue={tipQueue.Count}, showing={isShowingTip}, action={currentAction}, alpha={(group != null ? group.alpha : -1f)}");
        }
#endif
        if (MyTime.paused) return;
        if (!isShowingTip && tipQueue.Count > 0) StartCoroutine(ShowTip(tipQueue.Dequeue()));
        if (!skipping && isShowingTip &&
            (MyInputManager.GetButtonDown(currentAction) || Mathf.Abs(MyInputManager.GetAxis(currentAction)) > .1f))
            SkipTip();
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 3f);
    }

    private void SkipTip()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (diagnoseStartup) Debug.Log($"Input tip startup: dismissed action={currentAction}, joystick0={Input.GetKeyDown(KeyCode.JoystickButton0)}, elapsed={Time.realtimeSinceStartup:F2}");
#endif
        skipping = true;
        transform.localScale = Vector3.one * 1.25f;
    }

    private IEnumerator ShowTip(InputTip inputTip)
    {
        if (!TipsEnabled() || group == null || t_tip == null || glyphContainer == null) yield break;
        group.alpha = 0f;
        isShowingTip = true;
        skipping = false;
        float t = 0f;
        while (t < delay + inputTip.extraDelay) { t += MyTime.deltaTime; yield return null; }
        glyphContainer.SetAction(inputTip.action);
        t_tip.text = inputTip.tip;
        t_tip.color = Color.white;
        if (transform is RectTransform rect) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        transform.localScale = Vector3.one * .4f;
        if (audio != null) audio.Play();
        t = 0f;
        while (t < fadeTime)
        {
            t += MyTime.deltaTime;
            group.alpha = Mathf.Clamp01(t / fadeTime);
            float offset = Mathf.Lerp(-100f, 0f, Easing.OutCirc(t / fadeTime));
            transform.localPosition = defaultPosition + Vector3.right * offset;
            yield return null;
        }
        group.alpha = 1f;
        currentAction = inputTip.action;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (diagnoseStartup) Debug.Log($"Input tip startup: visible action={currentAction}, elapsed={Time.realtimeSinceStartup:F2}");
#endif
        t = 0f;
        while (t < timeout)
        {
            t += MyTime.deltaTime;
            if (skipping) { t_tip.color = Color.green; break; }
            yield return null;
        }
        t = 0f;
        while (t < delay) { t += MyTime.deltaTime; yield return null; }
        t = 0f;
        while (t < fadeTime) { t += MyTime.deltaTime; group.alpha = 1f - Mathf.Clamp01(t / fadeTime); yield return null; }
        currentAction = string.Empty;
        isShowingTip = false;
    }

    private void OnWeaponAdded(WeaponBase weaponBase)
    {
        if (weaponBase == null || weaponBase.weaponData == null || !weaponBase.weaponData.hasCrosshair || weaponBase.level != 1) return;
        var controls = SaveManager.Instance != null && SaveManager.Instance.config != null ? SaveManager.Instance.config.cfControlSettings : null;
        if (controls != null) SetTip(ConfigSettingsUtility.SettingNameToReadable("aim", controls), MyInputManager.Aim, 1f);
    }

    private void OnRunStarted()
    {
        var save = SaveManager.Instance;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (diagnoseStartup) Debug.Log($"Input tip startup: OnRunStarted save={save != null}, stats={(save != null && save.stats != null)}, enabled={TipsEnabled()}");
#endif
        if (save == null || save.stats == null || save.stats.stats == null || save.config == null || save.config.cfControlSettings == null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (diagnoseStartup) Debug.Log($"Input tip startup: runs={(save.stats.stats.TryGetValue(EMyStat.runs.ToString(), out MyStat observedRuns) ? observedRuns.value : -1f)}");
#endif
        if (save.stats.stats.TryGetValue(EMyStat.runs.ToString(), out MyStat runs) && runs.value >= 3f) return;
        SetTip(ConfigSettingsUtility.SettingNameToReadable("jump", save.config.cfControlSettings), MyInputManager.Jump, 10f);
        SetTip(ConfigSettingsUtility.SettingNameToReadable("slide", save.config.cfControlSettings), MyInputManager.Slide, 15f);
    }
}
